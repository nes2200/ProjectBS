using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Blossom_Active_Topdown", menuName = "Scriptable Objects/Skill/Monster/Blossom/Blossom_Active_Topdown")]
public class Blossom_Active_Topdown : ActiveSkillContainer
{
    [SerializeField] GameObject seedPrefab;

    [SerializeField, Min(0.1f)] float searchRange = 10f;
    [SerializeField, Min(0.1f)] float riseHeight = 6f;
    [SerializeField, Min(0.1f)] float fallHeight = 6f;
    [SerializeField, Min(0.1f)] float riseSpeed = 12f;
    [SerializeField, Min(0.1f)] float fallSpeed = 18f;
    [SerializeField, Min(0f)] float disappearDelay = 0.2f;

    [SerializeField, Min(0.1f)] float radius = 1.5f;
    [SerializeField, Min(0)] int damage = 30;

    [SerializeField, Range(0f, 1f)] float launchNormalizedTime = 0.55f;
    [SerializeField, Min(0.1f)] float maxDuration = 8f;

    const string TriggerName = "Topdown";
    const string StateTag = "Topdown";

    enum FlightPhase { Waiting, Rising, Hidden, Falling, Complete }

    sealed class SeedFlight
    {
        public GameObject visual;
        public Vector3 riseEnd;
    }

    public override bool CanUse(CharacterBase user, CharacterBase target)
    {
        if (!base.CanUse(user, target) || !user.gameObject.activeInHierarchy ||
            user.Team == TeamID.None || !seedPrefab)
            return false;
        if (!user.GetModule<AnimationModule>()) return false;
        AttackModule attack = user.GetModule<AttackModule>();
        if (attack && attack.IsAttacking) return false;

        if (!target || !target.IsAlive ||
            !target.gameObject.activeInHierarchy)
            return false;

        Vector3 offset = target.transform.position - user.transform.position;
        offset.y = 0f;

        StageManager stage = GetStage(user);
        return stage && stage.CurrentState == StageState.Battle &&
            HasSpawnPoint(user.GetComponentInChildren<SeedAttackExecutor>()) &&
            offset.sqrMagnitude <= searchRange * searchRange;
    }

    public override void UseSkill(CharacterBase user, CharacterBase target, Action requestCooldown)
    {
        if (!CanUse(user, target))
        {
            requestCooldown?.Invoke();
            return;
        }

        StageManager stage = GetStage(user);
        AnimationModule animation = user.GetModule<AnimationModule>();
        SkillModule skills = user.GetModule<SkillModule>();
        SeedAttackExecutor executor = user.GetComponentInChildren<SeedAttackExecutor>();

        CharacterBase victim = target;
        if (!victim)
        {
            requestCooldown?.Invoke();
            return;
        }

        // 여러 Blossom이 공유하는 에셋이므로 실행 상태는 지역 변수로 둔다.
        var flights = new List<SeedFlight>(executor.SpawnPoints.Count);
        var hitTargets = new List<CharacterBase>();
        FlightPhase phase = FlightPhase.Waiting;
        bool entered = false;
        bool animationFinished = false;
        bool finished = false;
        float elapsed = 0f;
        float hiddenElapsed = 0f;
        Vector3 impactPosition = default;

        void Finish()
        {
            if (finished) return;
            finished = true;
            GameManager.OnUpdateCharacter -= Tick;
            StageManager.OnStageStateChange -= OnStageChanged;
            user.OnFaint -= Finish;
            user.OnCharacterRemoved -= OnUserRemoved;
            victim.OnFaint -= OnTargetLost;
            victim.OnCharacterRemoved -= OnTargetRemoved;
            if (skills) skills.OnActiveSkillChanged -= OnSkillChanged;
            if (animation) animation.ResetTrigger(TriggerName);
            ReleaseSeeds(flights);
            requestCooldown?.Invoke();
        }

        void OnUserRemoved(CharacterBase removed) => Finish();
        void OnSkillChanged(SkillContainer oldSkill, SkillContainer newSkill) => Finish();
        void OnTargetRemoved(CharacterBase removed) => OnTargetLost();

        void OnTargetLost()
        {
            // 낙하 시작 후에는 목표가 제거되어도 확정한 위치에 착탄한다.
            if (phase != FlightPhase.Falling && phase != FlightPhase.Complete)
                Finish();
        }

        void OnStageChanged(StageState oldState, StageState newState)
        {
            if (!stage || stage.CurrentState != StageState.Battle) Finish();
        }

        void Launch()
        {
            // 실제 발사 순간의 월드 위치를 읽어 애니메이션의 손 위치에 맞춘다.
            foreach (Transform point in executor.SpawnPoints)
            {
                if (!point) continue;
                Vector3 position = point.position;
                GameObject visual = ObjectManager.CreateObject(seedPrefab, position,
                    Quaternion.LookRotation(Vector3.up, Vector3.forward));
                if (!visual)
                {
                    Finish();
                    return;
                }
                flights.Add(new SeedFlight
                {
                    visual = visual,
                    riseEnd = position + Vector3.up * riseHeight
                });
            }

            if (flights.Count == 0)
            {
                Finish();
                return;
            }
            phase = FlightPhase.Rising;
        }

        bool MoveSeeds(float deltaTime, bool falling)
        {
            bool arrived = true;
            float speed = Mathf.Max(0.1f, falling ? fallSpeed : riseSpeed);
            foreach (SeedFlight flight in flights)
            {
                if (!flight.visual || !flight.visual.activeInHierarchy)
                {
                    Finish();
                    return false;
                }

                Vector3 destination = falling ? impactPosition : flight.riseEnd;
                Transform visual = flight.visual.transform;
                visual.position = Vector3.MoveTowards(visual.position, destination, speed * deltaTime);
                if ((visual.position - destination).sqrMagnitude > 0.000001f)
                    arrived = false;
            }
            return arrived;
        }

        void BeginFall()
        {
            impactPosition = victim.transform.position;
            phase = FlightPhase.Falling;
            Vector3 position = impactPosition + Vector3.up * fallHeight;
            foreach (SeedFlight flight in flights)
            {
                flight.visual = ObjectManager.CreateObject(seedPrefab, position,
                    Quaternion.LookRotation(Vector3.down, Vector3.forward));
                if (!flight.visual)
                {
                    Finish();
                    return;
                }
            }
        }

        void ApplyImpact()
        {
            TargetingModule targeting = user.GetModule<TargetingModule>();
            Transform hostileParent = targeting ? targeting.HostileGroupParent : null;
            if (!hostileParent) return;

            // 피해로 적이 제거되기 전에 대상을 수집한다.
            hitTargets.Clear();
            foreach (Transform child in hostileParent)
            {
                if (!child.TryGetComponent(out CharacterBase enemy) || !IsEnemy(user, enemy))
                    continue;
                Vector3 offset = enemy.transform.position - impactPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radius * radius)
                    hitTargets.Add(enemy);
            }

            var damageInfo = new DamageStruct
            {
                from = user.gameObject,
                instigator = user.Controller,
                damageAmount = damage,
                defensePenetration = 0
            };

            // 피해는 발당 적용한다. 범위가 겹친 대상은 최대 세 번 맞는다.
            for (int i = 0; i < flights.Count && !finished; i++)
            {
                foreach (CharacterBase enemy in hitTargets)
                {
                    if (finished) return;
                    if (!IsEnemy(user, enemy)) continue;
                    HitPointModule hp = enemy.GetModule<HitPointModule>();
                    if (hp) hp.TakeDamage(damageInfo);
                }
            }
        }

        void Tick(float deltaTime)
        {
            if (finished) return;
            if (!user || !user.IsAlive || !user.gameObject.activeInHierarchy ||
                !animation || !stage || stage.CurrentState != StageState.Battle)
            {
                Finish();
                return;
            }

            elapsed += deltaTime;
            if (elapsed >= maxDuration)
            {
                Finish();
                return;
            }

            if (phase != FlightPhase.Falling && phase != FlightPhase.Complete &&
                !IsEnemy(user, victim))
            {
                Finish();
                return;
            }

            if (!animationFinished)
            {
                if (animation.TryGetNormalizedTime(out float progress, StateTag))
                {
                    entered = true;
                    if (phase == FlightPhase.Waiting && progress >= launchNormalizedTime)
                    {
                        Launch();
                        if (finished) return;
                    }
                    if (progress >= 1f) animationFinished = true;
                }
                else if (entered)
                {
                    animationFinished = true;
                    if (phase == FlightPhase.Waiting)
                    {
                        Finish();
                        return;
                    }
                }
            }

            switch (phase)
            {
                case FlightPhase.Rising:
                    if (MoveSeeds(deltaTime, false))
                    {
                        ReleaseSeeds(flights);
                        hiddenElapsed = 0f;
                        phase = FlightPhase.Hidden;
                    }
                    break;

                case FlightPhase.Hidden:
                    hiddenElapsed += deltaTime;
                    if (hiddenElapsed >= disappearDelay) BeginFall();
                    break;

                case FlightPhase.Falling:
                    if (MoveSeeds(deltaTime, true))
                    {
                        phase = FlightPhase.Complete;
                        ReleaseSeeds(flights);
                        ApplyImpact();
                    }
                    break;
            }

            // 모션과 착탄이 모두 끝난 뒤 AI의 이동/일반 공격 잠금을 해제한다.
            if (!finished && animationFinished && phase == FlightPhase.Complete)
                Finish();
        }

        user.OnFaint += Finish;
        user.OnCharacterRemoved += OnUserRemoved;
        victim.OnFaint += OnTargetLost;
        victim.OnCharacterRemoved += OnTargetRemoved;
        if (skills) skills.OnActiveSkillChanged += OnSkillChanged;
        StageManager.OnStageStateChange += OnStageChanged;
        GameManager.OnUpdateCharacter += Tick;

        user.GetModule<MovementModule>()?.StopMovement();
        user.MovementNotify(Vector3.zero);
        user.LookAtNotify(victim.transform.position - user.transform.position);
        animation.TriggerAnimation(TriggerName);
    }

    static bool IsEnemy(CharacterBase user, CharacterBase enemy)
    {
        return enemy && enemy != user && enemy.IsAlive && enemy.gameObject.activeInHierarchy &&
            enemy.Team != TeamID.None && enemy.Team != user.Team &&
            enemy.GetModule<HitPointModule>();
    }

    static bool HasSpawnPoint(SeedAttackExecutor executor)
    {
        if (!executor || executor.SpawnPoints == null) return false;
        foreach (Transform point in executor.SpawnPoints)
            if (point) return true;
        return false;
    }

    static StageManager GetStage(CharacterBase user)
    {
        TeamEliminateNotifier team = user.GetComponentInParent<TeamEliminateNotifier>();
        return team ? team.Stage : null;
    }

    static void ReleaseSeeds(List<SeedFlight> flights)
    {
        foreach (SeedFlight flight in flights)
        {
            GameObject visual = flight.visual;
            flight.visual = null;
            if (visual) ObjectManager.DestroyObject(visual);
        }
    }
}
