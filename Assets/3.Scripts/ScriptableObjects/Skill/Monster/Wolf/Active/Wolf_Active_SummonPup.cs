using System;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Wolf_Active_SummonPup", menuName = "Scriptable Objects/Skill/Monster/Wolf/Wolf_Active_SummonPup")]
public class Wolf_Active_SummonPup : ActiveSkillContainer
{
    [SerializeField] UnitDefinition pupDefinition;
    [SerializeField, Min(0f)] float minSpawnDistance = 1.5f;
    [SerializeField, Min(0f)] float maxSpawnDistance = 3f;
    [SerializeField, Min(8)] int positionSamples = 24;
    [SerializeField, Range(0f, 1f)] float summonNormalizedTime = 0.55f;
    [SerializeField, Min(0.1f)] float maxDuration = 5f;

    const string TriggerName = "SummonPup";
    const string StateTag = "SummonPup";
    const float Clearance = 0.1f;

    public override bool CanUse(CharacterBase user, CharacterBase target)
    {
        if (!base.CanUse(user, target) || !user.gameObject.activeInHierarchy ||
            !pupDefinition || !pupDefinition.Status || user.Team == TeamID.None)
            return false;
        if (!user.GetModule<AnimationModule>()) return false;
        AttackModule attack = user.GetModule<AttackModule>();
        if (attack && attack.IsAttacking) return false;

        StageManager stage = GetStage(user);
        TargetingModule targeting = user.GetModule<TargetingModule>();
        return stage && stage.CurrentState == StageState.Battle &&
            stage.Placement && stage.CharacterRegistry && targeting && targeting.HostileGroupParent;
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

        // 여러 Wolf가 하나의 에셋을 공유하므로 실행 상태는 지역 변수로 둔다.
        bool entered = false;
        bool summoned = false;
        bool finished = false;
        float elapsed = 0f;

        void Finish()
        {
            if (finished) return;
            finished = true;
            GameManager.OnUpdateCharacter -= Tick;
            StageManager.OnStageStateChange -= OnStageChanged;
            user.OnFaint -= Finish;
            user.OnCharacterRemoved -= OnRemoved;
            if (skills) skills.OnActiveSkillChanged -= OnSkillChanged;
            if (animation) animation.ResetTrigger(TriggerName);
            requestCooldown?.Invoke();
        }

        void OnRemoved(CharacterBase removed) => Finish();
        void OnSkillChanged(SkillContainer oldSkill, SkillContainer newSkill) => Finish();
        void OnStageChanged(StageState oldState, StageState newState)
        {
            if (!stage || stage.CurrentState != StageState.Battle) Finish();
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

            if (!animation.TryGetNormalizedTime(out float progress, StateTag))
            {
                if (entered) Finish();
                return;
            }

            entered = true;
            if (!summoned && progress >= summonNormalizedTime)
            {
                // 위치를 찾지 못해도 이번 시전에서는 한 번만 시도한다.
                summoned = true;
                TrySummon(user, stage);
            }
            if (progress >= 1f) Finish();
        }

        user.OnFaint += Finish;
        user.OnCharacterRemoved += OnRemoved;
        if (skills) skills.OnActiveSkillChanged += OnSkillChanged;
        StageManager.OnStageStateChange += OnStageChanged;
        GameManager.OnUpdateCharacter += Tick;
        user.GetModule<MovementModule>()?.StopMovement();
        user.MovementNotify(Vector3.zero);
        animation.TriggerAnimation(TriggerName);
    }

    static StageManager GetStage(CharacterBase user)
    {
        TeamEliminateNotifier team = user.GetComponentInParent<TeamEliminateNotifier>();
        return team ? team.Stage : null;
    }

    bool TryFindSpawnPosition(CharacterBase user, StageManager stage,
        NavMeshAgent prefabAgent, out Vector3 position)
    {
        position = default;
        Vector3 origin = user.transform.position;
        float pupRadius = Mathf.Max(pupDefinition.Status.colliderRadius, prefabAgent.radius);
        float minDistance = Mathf.Max(minSpawnDistance,
            user.Status.colliderRadius + pupRadius + Clearance);
        float maxDistance = Mathf.Max(0f, maxSpawnDistance);
        if (minDistance > maxDistance) return false;

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = prefabAgent.agentTypeID,
            areaMask = prefabAgent.areaMask
        };
        if (!NavMesh.SamplePosition(origin, out NavMeshHit start, 1f, filter))
            return false;

        var path = new NavMeshPath();
        float bestScore = float.NegativeInfinity;
        float angleOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        int samples = Mathf.Max(8, positionSamples);
        bool found = false;

        for (int i = 0; i < samples; i++)
        {
            float angle = angleOffset + Mathf.PI * 2f * i / samples;
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.5f, filter)) continue;

            Vector3 offset = hit.position - origin;
            if (Mathf.Abs(offset.y) > 0.75f) continue;
            offset.y = 0f;
            if (offset.sqrMagnitude < minDistance * minDistance ||
                offset.sqrMagnitude > maxDistance * maxDistance) continue;
            if (!NavMesh.CalculatePath(start.position, hit.position, filter, path) ||
                path.status != NavMeshPathStatus.PathComplete) continue;

            bool blocked = false;
            float nearestEnemy = float.PositiveInfinity;
            foreach (CharacterBase other in stage.CharacterRegistry.Characters)
            {
                if (!other || !other.IsAlive || !other.gameObject.activeInHierarchy) continue;
                Vector3 separation = other.transform.position - hit.position;
                separation.y = 0f;
                float requiredDistance = pupRadius + other.Status.colliderRadius + Clearance;
                if (separation.sqrMagnitude < requiredDistance * requiredDistance)
                {
                    blocked = true;
                    break;
                }
                if (other.Team != user.Team && other.Team != TeamID.None)
                    nearestEnemy = Mathf.Min(nearestEnemy, separation.sqrMagnitude);
            }

            if (blocked || (found && nearestEnemy <= bestScore)) continue;
            bestScore = nearestEnemy;
            position = hit.position;
            found = true;
        }
        return found;
    }

    void TrySummon(CharacterBase user, StageManager stage)
    {
        if (!pupDefinition || !pupDefinition.Status || !stage.Placement) return;

        // 위치 탐색과 실제 생성에 동일한 프리팹을 사용한다.
        GameObject prefab = pupDefinition.GetRandomPrefab();
        if (!prefab || !prefab.TryGetComponent(out NavMeshAgent prefabAgent))
        {
            Debug.LogError("[SummonPup] WolfPup 프리팹의 NavMeshAgent를 확인하세요.", this);
            return;
        }
        if (!TryFindSpawnPosition(user, stage, prefabAgent, out Vector3 position)) return;

        if (stage.Placement.TrySpawnUnit(prefab, pupDefinition.Status, user.Team,
            position, user.transform.rotation, out CharacterBase pup))
        {
            pup.GetComponent<SpawnFade>()?.Play();
        }
    }
}
