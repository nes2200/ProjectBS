using System;
using UnityEngine;

public struct AttackInfo
{
    public CharacterBase target;
    public ControllerBase instigator;
    public int damageAmount;
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
        animModule = null;
    }

    public void AttackTarget(in AttackInfo attackInfo)
    {
        if (isAttackCooldown || isAttacking || !attackExecutor) return;

        isAttacking = true;
        attackExecutor.Prepare();

        animModule.SetBool("IsAttacking", true);
        animModule.TriggerAnimation("Attack");

        currentAttackInfo = attackInfo;
        hasExecuteAttack = false;

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

        if (normalizedProgress < attackExecutor.ExecuteNormalizedTime) return;
        
        hasExecuteAttack = true;
        GameManager.OnUpdateCharacter -= UpdateAttack;

        attackExecutor.Execute(currentAttackInfo);
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
        if(attackCooldownCurrent >= Owner.Status.attackSpeed)
        {
            AttackCooldownEnd();
        }
    }
}