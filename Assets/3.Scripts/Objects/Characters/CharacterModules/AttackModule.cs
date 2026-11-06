using System;
using UnityEngine;

public delegate void AttackEvent(CharacterBase attacker, CharacterBase target);

public struct AttackInfo
{
    public CharacterBase target;
    public ControllerBase instigator;
    public int damageAmount;
}

[Serializable]
public struct AttackMotion
{
    public string trigger;

    [Range(0f, 1f)]
    public float normalizedTime;
}

//이 모듈은 '공격'을 담당하는 모듈.
//hit과 같은 '맞는 역할'은 HitPointModule이 담당한다.
public class AttackModule : CharacterModule
{
    //공격 쿨다운
    bool isAttackCooldown = false;
    public bool IsAttackCooldown => isAttackCooldown;
    float attackCooldownCurrent;

    //공격 진행
    bool hasExecuteAttack = false;
    AttackInfo currentAttackInfo;
    bool isAttacking = false;
    public bool IsAttacking => isAttacking;

    //애니메이션 모듈
    AnimationModule animModule;

    //근거리 원거리 나누는 용도
    [SerializeField] AttackExecutor attackExecutor;

    //다양한 공격 애니메이션 용도
    [SerializeField]
    AttackMotion[] attackMotions =
    {
        new AttackMotion
        {
            trigger = "Attack",
            normalizedTime = 0f
        }
    };
    AttackMotion currentAttackMotion;

    public event AttackEvent OnBeforeAttack;
    public event AttackEvent OnAfterAttack;

    public override Type RegistrationType => typeof(AttackModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        animModule = Owner.GetModule<AnimationModule>();
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);
        GameManager.OnUpdateCharacter -= UpdateAttack;
        GameManager.OnUpdateCharacter -= AttackCooldownUpdate;

        isAttacking = false;
        isAttackCooldown = false;
        hasExecuteAttack = false;
        attackCooldownCurrent = 0f;

        currentAttackInfo = default;
        currentAttackMotion = default;
        animModule = null;
    }

    public void AttackTarget(in AttackInfo attackInfo)
    {
        if (isAttackCooldown || isAttacking || !attackExecutor || !animModule)
            return;

        if (attackMotions == null || attackMotions.Length == 0)
        {
            Debug.LogError("공격 모션이 설정되지 않았습니다.", this);
            return;
        }

        int index = UnityEngine.Random.Range(0, attackMotions.Length);
        currentAttackMotion = attackMotions[index];

        if (string.IsNullOrWhiteSpace(currentAttackMotion.trigger))
        {
            Debug.LogError("공격 트리거 이름이 비어 있습니다.", this);
            return;
        }

        currentAttackInfo = attackInfo;
        hasExecuteAttack = false;
        isAttacking = true;

        attackExecutor.Prepare();

        animModule.SetBool("IsAttacking", true);
        animModule.TriggerAnimation(currentAttackMotion.trigger);

        GameManager.OnUpdateCharacter -= UpdateAttack;
        GameManager.OnUpdateCharacter += UpdateAttack;
    }

    void UpdateAttack(float deltaTime)
    {
        if(!animModule)
        {
            GameManager.OnUpdateCharacter -= UpdateAttack;
            isAttacking = false;
            return;
        }
        if (!isAttacking || hasExecuteAttack) return;

        if (!animModule.TryGetNormalizedTime(out float normalizedProgress, attackExecutor.ExecutionStateTag)) return;

        if (normalizedProgress < currentAttackMotion.normalizedTime) return;
        
        hasExecuteAttack = true;
        GameManager.OnUpdateCharacter -= UpdateAttack;

        OnBeforeAttack?.Invoke(Owner, currentAttackInfo.target);
        attackExecutor.Execute(currentAttackInfo);
        OnAfterAttack?.Invoke(Owner, currentAttackInfo.target);
    }

    public void OnAttackAnimationEnd()
    {
        if (!isAttacking) return;

        isAttacking = false;
        animModule.SetBool("IsAttacking", false);

        AttackCooldownStart();
    }

    public void AttackCooldownStart()
    {
        GameManager.OnUpdateCharacter -= AttackCooldownUpdate;
        GameManager.OnUpdateCharacter += AttackCooldownUpdate;
        isAttackCooldown = true;
    }
    public void AttackCooldownEnd()
    {
        GameManager.OnUpdateCharacter -= AttackCooldownUpdate;
        attackCooldownCurrent = 0f;
        isAttackCooldown = false;
    }
    void AttackCooldownUpdate(float deltaTime)
    {
        attackCooldownCurrent += deltaTime;
        if(attackCooldownCurrent >= Owner.finalStatus.attackSpeed)
        {
            AttackCooldownEnd();
        }
    }
}