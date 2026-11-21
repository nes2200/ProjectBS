using UnityEngine;

[CreateAssetMenu(fileName = "Bloom_Active_Combo", menuName = "Scriptable Objects/Skill/Monster/Bloom/Bloom_Active_Combo")]
public class Bloom_Active_Combo : ActiveSkillContainer
{
    const string Combo = "Combo";

    [SerializeField, Range(0f, 1f)] float firstShotTime = 0.22f;
    [SerializeField, Range(0f, 1f)] float secondShotTime = 0.47f;
    [SerializeField, Range(0f, 1f)] float thirdShotTime = 0.73f;

    void OnValidate()
    {
        secondShotTime = Mathf.Max(firstShotTime, secondShotTime);
        thirdShotTime = Mathf.Max(secondShotTime, thirdShotTime);
    }

    public override bool CanUse(CharacterBase user, CharacterBase target)
    {
        if (!base.CanUse(user, target) || !target || !user.gameObject.activeInHierarchy)
            return false;

        var range = user.GetModule<AttackRangeModule>();
        var attack = user.GetModule<AttackModule>();
        var team = user.GetComponentInParent<TeamEliminateNotifier>();
        return range && range.Evaluate(target) == AttackRangeState.Ready &&
            team && team.Stage && team.Stage.CurrentState == StageState.Battle &&
            attack && !attack.IsAttacking && user.GetModule<AnimationModule>() &&
            user.GetComponentInChildren<SeedAttackExecutor>();
    }

    public override void UseSkill(CharacterBase user, CharacterBase target, System.Action requestCooldown)
    {
        if (!CanUse(user, target))
        {
            requestCooldown?.Invoke();
            return;
        }

        var animation = user.GetModule<AnimationModule>();
        var skills = user.GetModule<SkillModule>();
        var executor = user.GetComponentInChildren<SeedAttackExecutor>();
        var stage = user.GetComponentInParent<TeamEliminateNotifier>().Stage;
        float[] shotTimes = { firstShotTime, secondShotTime, thirdShotTime };
        int nextShot = 0;
        bool entered = false;
        bool finished = false;
        float entryWait = 0f;

        void Finish()
        {
            if (finished) return;
            finished = true;
            GameManager.OnUpdateCharacter -= Tick;
            StageManager.OnStageStateChange -= OnStageChanged;
            user.OnFaint -= Finish;
            target.OnFaint -= Finish;
            user.OnCharacterRemoved -= OnRemoved;
            target.OnCharacterRemoved -= OnRemoved;
            if (skills) skills.OnActiveSkillChanged -= OnSkillChanged;
            if (animation) animation.ResetTrigger(Combo);
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
            if (!user || !target || !user.IsAlive || !target.IsAlive ||
                !user.gameObject.activeInHierarchy || !target.gameObject.activeInHierarchy ||
                target.Team == user.Team || !executor || !animation ||
                !stage || stage.CurrentState != StageState.Battle)
            {
                Finish();
                return;
            }

            if (!animation.TryGetNormalizedTime(out float progress, Combo))
            {
                entryWait += deltaTime;
                if (entered || entryWait >= 1f) Finish();
                return;
            }

            entered = true;
            while (!finished && nextShot < shotTimes.Length && progress >= shotTimes[nextShot])
            {
                nextShot++;
                executor.Execute(new AttackInfo
                {
                    target = target,
                    instigator = user.Controller,
                    damageAmount = user.finalStatus.damage
                });
            }
            if (progress >= 1f) Finish();
        }

        user.OnFaint += Finish;
        target.OnFaint += Finish;
        user.OnCharacterRemoved += OnRemoved;
        target.OnCharacterRemoved += OnRemoved;
        if (skills) skills.OnActiveSkillChanged += OnSkillChanged;
        StageManager.OnStageStateChange += OnStageChanged;
        GameManager.OnUpdateCharacter += Tick;

        user.GetModule<MovementModule>()?.StopMovement();
        user.MovementNotify(Vector3.zero);
        user.LookAtNotify(target.transform.position - user.transform.position);
        animation.TriggerAnimation(Combo);
    }
}
