using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Werewolf_Active_Pounce", menuName = "Scriptable Objects/Skill/Monster/Werewolf/Werewolf_Active_Pounce")]
public class Werewolf_Active_Pounce : ActiveSkillContainer
{
    [SerializeField, Min(0.01f)] float radius = 3f;
    [SerializeField, Min(0)] int damage = 30;

    [SerializeField, Range(0f, 1f)] float hitNormalizedTime = 0.55f;

    // Animator 설정 오류 등으로 사용 상태가 계속 유지되는 것을 방지
    [SerializeField, Min(0.1f)] float maxDuration = 5f;

    const string TriggerName = "Pounce";
    const string StateTag = "Pounce";

    public override bool CanUse(CharacterBase user, CharacterBase target)
    {
        if (!base.CanUse(user, target)) return false;
        if (!user.GetModule<AnimationModule>()) return false;

        AttackModule attack = user.GetModule<AttackModule>();
        if (attack && attack.IsAttacking) return false;

        return FindTargets(user, null);
    }

    public override void UseSkill(CharacterBase user, CharacterBase target, Action requestCooldown)
    {
        AnimationModule animation = user.GetModule<AnimationModule>();
        SkillModule skills = user.GetModule<SkillModule>();

        // ScriptableObject는 여러 유닛이 공유하므로
        // 사용 중 상태는 필드 대신 호출마다 만들어지는 지역 변수로 보관
        bool entered = false;
        bool hitApplied = false;
        bool finished = false;
        float elapsed = 0f;

        var targets = new List<CharacterBase>();

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

        void OnRemoved(CharacterBase removed)
        {
            Finish();
        }

        void OnSkillChanged(SkillContainer oldSkill, SkillContainer newSkill)
        {
            Finish();
        }

        void OnStageChanged(StageState oldState, StageState newState)
        {
            if (newState != StageState.Battle)
                Finish();
        }

        void Tick(float deltaTime)
        {
            if (finished) return;

            if (!user || !user.IsAlive || !user.gameObject.activeInHierarchy || !animation)
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

            if (!animation.TryGetNormalizedTime(
                    out float progress, StateTag))
            {
                // 진입 전에는 기다리고, 진입 후 상태를 벗어나면 종료
                if (entered)
                    Finish();

                return;
            }

            entered = true;

            if (!hitApplied && progress >= hitNormalizedTime)
            {
                hitApplied = true;

                // 사용 시작 시점이 아니라 타격 시점의 위치로 다시 판정
                targets.Clear();
                FindTargets(user, targets);

                var damageInfo = new DamageStruct
                {
                    from = user.gameObject,
                    instigator = user.Controller,
                    damageAmount = damage,
                    defensePenetration = 0
                };

                foreach (CharacterBase enemy in targets)
                {
                    if (finished || !user.IsAlive) break;

                    if (!enemy || !enemy.IsAlive ||
                        !enemy.gameObject.activeInHierarchy)
                        continue;

                    HitPointModule hp = enemy.GetModule<HitPointModule>();

                    if (hp)
                        hp.TakeDamage(damageInfo);
                }
            }

            if (progress >= 1f)
                Finish();
        }

        user.OnFaint += Finish;
        user.OnCharacterRemoved += OnRemoved;

        if (skills) skills.OnActiveSkillChanged += OnSkillChanged;

        StageManager.OnStageStateChange += OnStageChanged;
        GameManager.OnUpdateCharacter += Tick;

        user.GetModule<MovementModule>()?.StopMovement();
        animation.TriggerAnimation(TriggerName);
    }

    bool FindTargets(CharacterBase user, List<CharacterBase> results)
    {
        TargetingModule targeting = user.GetModule<TargetingModule>();
        Transform hostileParent = targeting ? targeting.HostileGroupParent : null;

        if (!hostileParent) return false;

        Vector3 center = user.transform.position;
        float radiusSqr = radius * radius;
        bool found = false;

        foreach (Transform child in hostileParent)
        {
            if (!child.TryGetComponent(out CharacterBase enemy))
                continue;

            if (enemy == user || !enemy.IsAlive ||
                !enemy.gameObject.activeInHierarchy ||
                enemy.Team == user.Team ||
                enemy.Team == TeamID.None)
                continue;

            if (!enemy.GetModule<HitPointModule>())
                continue;

            Vector3 offset = enemy.transform.position - center;
            offset.y = 0f;

            if (offset.sqrMagnitude > radiusSqr)
                continue;

            // 사용 가능 여부만 검사할 때는 첫 번째 적을 찾으면 종료
            if (results == null)
                return true;

            results.Add(enemy);
            found = true;
        }

        return found;
    }
}