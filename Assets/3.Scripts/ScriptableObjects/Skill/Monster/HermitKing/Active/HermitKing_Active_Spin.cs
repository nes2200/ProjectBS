using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HermitKing_Active_Spin", menuName = "Scriptable Objects/Skill/Monster/HermitKing/HermitKing_Active_Spin")]
public class HermitKing_Active_Spin : ActiveSkillContainer
{
    const string SpinTrigger = "SpinAttack";
    const string CastingTag = "HermitKingSpin";
    const string EmergeTag = "HermitKingEmerge";
    const float CastTimeout = 5f;

    [SerializeField, Min(0.01f)] float radius = 3.5f;
    [SerializeField, Min(0f)] float damageMultiplier = 1.5f;
    [SerializeField] HermitKing_Stun stunEffect;

    public override bool CanUse(CharacterBase user, CharacterBase target)
    {
        if (!base.CanUse(user, target) || !user.gameObject.activeInHierarchy ||
            !stunEffect || !user.GetModule<AnimationModule>() ||
            user.GetModule<EffectModule>()?.IsStunned == true)
            return false;

        AttackModule attack = user.GetModule<AttackModule>();
        StageManager stage = user.GetComponentInParent<TeamEliminateNotifier>()?.Stage;
        return !(attack && attack.IsAttacking) && stage && stage.CurrentState == StageState.Battle &&
            FindEnemies(user, null);
    }

    public override void UseSkill(CharacterBase user, CharacterBase target, Action requestCooldown)
    {
        if (!CanUse(user, target))
        {
            requestCooldown?.Invoke();
            return;
        }

        AnimationModule animation = user.GetModule<AnimationModule>();
        SkillModule skills = user.GetModule<SkillModule>();
        StageManager stage = user.GetComponentInParent<TeamEliminateNotifier>().Stage;
        bool enteredCast = false;
        bool hitApplied = false;
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
            if (animation) animation.ResetTrigger(SpinTrigger);
            requestCooldown?.Invoke();
        }

        void OnRemoved(CharacterBase removed) => Finish();
        void OnSkillChanged(SkillContainer previous, SkillContainer current) => Finish();

        void OnStageChanged(StageState previous, StageState current)
        {
            if (!stage || stage.CurrentState != StageState.Battle) Finish();
        }

        void Tick(float deltaTime)
        {
            if (finished) return;
            elapsed += deltaTime;

            if (!user || !user.IsAlive || !user.gameObject.activeInHierarchy ||
                !animation || !stage || stage.CurrentState != StageState.Battle ||
                user.GetModule<EffectModule>()?.IsStunned == true || elapsed >= CastTimeout)
            {
                Finish();
                return;
            }

            // The Animator enters Emerge at the end of the third spin.
            if (animation.TryGetNormalizedTime(out _, EmergeTag))
            {
                if (enteredCast && !hitApplied)
                {
                    hitApplied = true;
                    DealAreaDamage(user);
                }
                return;
            }

            if (animation.TryGetNormalizedTime(out _, CastingTag))
                enteredCast = true;
            else if (enteredCast)
                Finish();
        }

        user.OnFaint += Finish;
        user.OnCharacterRemoved += OnRemoved;
        if (skills) skills.OnActiveSkillChanged += OnSkillChanged;
        StageManager.OnStageStateChange += OnStageChanged;
        GameManager.OnUpdateCharacter += Tick;

        MovementModule movement = user.GetModule<MovementModule>();
        movement?.StopMovement();
        movement?.Rotate(Vector3.zero);
        user.MovementNotify(Vector3.zero);
        animation.TriggerAnimation(SpinTrigger);
    }

    void DealAreaDamage(CharacterBase user)
    {
        var enemies = new List<CharacterBase>();
        FindEnemies(user, enemies);

        var damage = new DamageStruct
        {
            from = user.gameObject,
            instigator = user.Controller,
            damageAmount = Mathf.RoundToInt(user.finalStatus.damage * damageMultiplier),
            defensePenetration = 0
        };

        foreach (CharacterBase enemy in enemies)
        {
            if (!enemy || !enemy.IsAlive || !enemy.gameObject.activeInHierarchy) continue;
            HitPointModule hitPoints = enemy.GetModule<HitPointModule>();
            if (!hitPoints) continue;

            hitPoints.TakeDamage(damage);
            if (enemy && enemy.IsAlive && enemy.gameObject.activeInHierarchy && stunEffect)
                enemy.GetModule<EffectModule>()?.ApplyEffect(user, stunEffect);
        }
    }

    bool FindEnemies(CharacterBase user, List<CharacterBase> enemies)
    {
        TargetingModule targeting = user.GetModule<TargetingModule>();
        Transform hostileParent = targeting ? targeting.HostileGroupParent : null;
        if (!hostileParent) return false;

        Vector3 center = user.transform.position;
        float radiusSquared = radius * radius;
        foreach (Transform child in hostileParent)
        {
            if (!child.TryGetComponent(out CharacterBase enemy) ||
                enemy == user || !enemy.IsAlive || !enemy.gameObject.activeInHierarchy ||
                enemy.Team == TeamID.None || enemy.Team == user.Team ||
                !enemy.GetModule<HitPointModule>())
                continue;

            Vector3 offset = enemy.transform.position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude > radiusSquared) continue;

            if (enemies == null) return true;
            enemies.Add(enemy);
        }

        return enemies != null && enemies.Count > 0;
    }
}
