using UnityEngine;

public class AnimationModule : CharacterModule
{
    [Header("Animation Settings")]
    [SerializeField] Animator anim;
    [SerializeField] bool isRotationByMovement;
    [SerializeField] CapsuleCollider mainCollider;
    [SerializeField] Rigidbody mainRigid;
    [SerializeField] bool useDeathAnimation;

    [Header("Weapons")]
    [SerializeField] private Rigidbody weaponRigid;
    [SerializeField] private Collider weaponCollider;

    Rigidbody[] ragdollRigidbodies;
    public CapsuleCollider MainCollider => mainCollider;

    bool isMoving;

    public sealed override System.Type RegistrationType => typeof(AnimationModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        newOwner.OnLookAt -= AnimationByLookRotation;
        newOwner.OnLookAt += AnimationByLookRotation;
        newOwner.OnMovement -= AnimationByMovement;
        newOwner.OnMovement += AnimationByMovement;
        newOwner.OnFaint -= AnimationByFaint;
        newOwner.OnFaint += AnimationByFaint;
        newOwner.OnDamage -= AnimationByDamaged;
        newOwner.OnDamage += AnimationByDamaged;

        StageManager.OnStageStateChange -= StopAnimationByEndBattle;
        StageManager.OnStageStateChange += StopAnimationByEndBattle;

        //모든 rigid를 가져와 isKineatic을 true로 바꾼다
        ResetWeapon();
        GetAllRigidbody();
        SetRigidbodyAndCollier();
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);
        oldOwner.OnLookAt -= AnimationByLookRotation;
        oldOwner.OnMovement -= AnimationByMovement;
        oldOwner.OnFaint -= AnimationByFaint;
        StageManager.OnStageStateChange -= StopAnimationByEndBattle;
        oldOwner.OnDamage -= AnimationByDamaged;
    }

    public void AnimationByLookRotation(Vector3 lookRotation)
    {
        if (!anim) return;
        //                            world로 들어온 벡터를 local로 돌린다
        Vector3 localRotation = transform.InverseTransformVector(lookRotation).normalized;
        anim.SetFloat("MoveX", localRotation.x);
        anim.SetFloat("MoveZ", localRotation.z);
    }
    public void AnimationByMovement(Vector3 moveDelta)
    {
        if (!anim) return;
        isMoving = moveDelta.sqrMagnitude > 0.01f;
        if (isRotationByMovement && isMoving)
        {
            AnimationByLookRotation(moveDelta);
        }
        anim.SetFloat("MoveSpeed", moveDelta.magnitude / Time.fixedDeltaTime);
    }

    public void AnimationByFaint()
    {
        if (!anim) return;

        mainCollider.enabled = false;
        mainRigid.isKinematic = true;

        //죽는 애니메이션을 쓸 친구들
        if (useDeathAnimation)
        {
            anim.enabled = true;
            anim.SetBool("IsAttacking", false);
            anim.SetTrigger("Die");
            return;
        }

        //죽는 애니메이션 없이 래그돌 쓸 친구들
        anim.enabled = false;
        foreach (Rigidbody rigid in ragdollRigidbodies)
        {
            if (rigid == mainRigid || rigid == weaponRigid) continue;
            rigid.isKinematic = false;
        }
        DropWeapon();
    }
    private void DropWeapon()
    {
        if (!weaponRigid) return;

        weaponRigid.transform.SetParent(null, true);
        weaponRigid.useGravity = true;
        weaponRigid.isKinematic = false;
        weaponCollider.isTrigger = false;
    }
    private void ResetWeapon()
    {
        if (!weaponRigid) return;

        weaponRigid.useGravity = false;
        weaponRigid.isKinematic = true;
        weaponCollider.isTrigger = true;
    }

    public void AnimationByDamaged(in DamageStruct info)
    {
        AttackModule atkModule = Owner.GetModule<AttackModule>();
        if (atkModule && atkModule.IsAttacking) return;

        if (isMoving)
        {
            anim.SetTrigger("MovingDamaged");
        }
        else
        {
            anim.SetTrigger("FullBodyDamaged");
        }
    }

    //모든 하위 rigidbody 가져오기
    public void GetAllRigidbody()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
    }
    //rigidbody와 collider 세팅하기
    public void SetRigidbodyAndCollier()
    {
        if (ragdollRigidbodies == null || !mainCollider) return;

        //각 파츠의 rigidbody의 isKinematic을 true로 바꾸는 작업.
        foreach (Rigidbody rigid in ragdollRigidbodies) { rigid.isKinematic = true; }
        //메인 콜라이더가 꺼져있다면 켜주기
        mainCollider.enabled = true;
        mainRigid.isKinematic = true;
    }

    //hp가 0이 되면 실행해야 할 기능
    public void StopAnimationByEndBattle(StageState oldState, StageState newState)
    {
        if (!anim) return;

        if(newState == StageState.Result)
        {
            anim.speed = 0f;
        }
    }

    public bool TryGetNormalizedTime(out float normalizedTime, string requiredTag, int layerIndex = 0)
    {
        if(!anim || !anim.isActiveAndEnabled)
        {
            normalizedTime = 0f;
            return false;
        }

        AnimatorStateInfo stateInfo;

        if (anim.IsInTransition(layerIndex))
        {
            stateInfo = anim.GetNextAnimatorStateInfo(layerIndex);
        }
        else
        {
            stateInfo = anim.GetCurrentAnimatorStateInfo(layerIndex);
        }

        if (!stateInfo.IsTag(requiredTag))
        {
            normalizedTime = 0f;
            return false;
        }
        normalizedTime = stateInfo.normalizedTime;
        return true;

    }

    public void TriggerAnimation(string wantAnim)
    {
        anim.SetTrigger(wantAnim);
    }
    public void SetBool(string parameter, bool value)
    {
        anim.SetBool(parameter, value);
    }

}