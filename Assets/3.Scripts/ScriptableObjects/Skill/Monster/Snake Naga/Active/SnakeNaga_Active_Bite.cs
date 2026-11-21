using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SnakeNaga_Active_Bite", menuName = "Scriptable Objects/Skill/Monster/SnakeNaga/SnakeNaga_Active_Bite")]
public class SnakeNaga_Active_Bite : ActiveSkillContainer
{
    const string BiteTrigger = "Bite";
    const string BiteStateTag = "Bite Attack";
    const float StateEntryTimeout = 1f;

    [SerializeField, Min(0f)] float damageMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] float hitNormalizedTime = 0.4f;
    [SerializeField, FormerlySerializedAs("defenseDebuff")] SnakeNaga_Poisoned poisonEffect;

    public override bool CanUse(CharacterBase user, CharacterBase target)
    {
        if (!base.CanUse(user, target) || !user.gameObject.activeInHierarchy ||
            !target || target.Team == TeamID.None || !target.GetModule<HitPointModule>())
            return false;

        var range = user.GetModule<AttackRangeModule>();
        var attack = user.GetModule<AttackModule>();
        var team = user.GetComponentInParent<TeamEliminateNotifier>();
        return range && range.Evaluate(target) == AttackRangeState.Ready &&
            attack && !attack.IsAttacking && user.GetModule<AnimationModule>() &&
            team && team.Stage && team.Stage.CurrentState == StageState.Battle;
    }

    public override void UseSkill(CharacterBase user, CharacterBase target, Action requestCooldown)
    {
        if (!CanUse(user, target))
        {
            requestCooldown?.Invoke();
            return;
        }

        var animation = user.GetModule<AnimationModule>();
        var range = user.GetModule<AttackRangeModule>();
        var skills = user.GetModule<SkillModule>();
        var stage = user.GetComponentInParent<TeamEliminateNotifier>().Stage;
        bool enteredBiteState = false;
        bool hitResolved = false;
        bool finished = false;
        float entryWait = 0f;

        void Finish()
        {
            if (finished) return;
            finished = true;
            GameManager.OnUpdateCharacter -= Tick;
            StageManager.OnStageStateChange -= OnStageChanged;
            user.OnFaint -= Finish;
            user.OnCharacterRemoved -= OnRemoved;
            target.OnCharacterRemoved -= OnRemoved;
            if (skills) skills.OnActiveSkillChanged -= OnSkillChanged;
            if (animation) animation.ResetTrigger(BiteTrigger);
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
            if (!user || !user.IsAlive || !user.gameObject.activeInHierarchy ||
                !animation || !range || !stage || stage.CurrentState != StageState.Battle ||
                (!hitResolved && range.Evaluate(target) == AttackRangeState.InvalidTarget))
            {
                Finish();
                return;
            }

            if (!animation.TryGetNormalizedTime(out float progress, BiteStateTag))
            {
                entryWait += deltaTime;
                if (enteredBiteState || entryWait >= StateEntryTimeout) Finish();
                return;
            }

            enteredBiteState = true;
            if (!hitResolved && progress >= hitNormalizedTime)
            {
                hitResolved = true;
                if (range.Evaluate(target) == AttackRangeState.Ready)
                {
                    var targetHitPoints = target.GetModule<HitPointModule>();
                    if (targetHitPoints)
                    {
                        targetHitPoints.TakeDamage(new DamageStruct
                        {
                            from = user.gameObject,
                            instigator = user.Controller,
                            damageAmount = Mathf.RoundToInt(user.finalStatus.damage * damageMultiplier),
                            defensePenetration = 0
                        });

                        if (!finished && target && target.IsAlive && poisonEffect)
                            target.GetModule<EffectModule>()?.ApplyEffect(user, poisonEffect);
                    }
                }
            }

            if (progress >= 1f) Finish();
        }

        user.OnFaint += Finish;
        user.OnCharacterRemoved += OnRemoved;
        target.OnCharacterRemoved += OnRemoved;
        if (skills) skills.OnActiveSkillChanged += OnSkillChanged;
        StageManager.OnStageStateChange += OnStageChanged;
        GameManager.OnUpdateCharacter += Tick;

        user.GetModule<MovementModule>()?.StopMovement();
        user.MovementNotify(Vector3.zero);
        user.LookAtNotify(target.transform.position - user.transform.position);
        animation.TriggerAnimation(BiteTrigger);
    }
}
