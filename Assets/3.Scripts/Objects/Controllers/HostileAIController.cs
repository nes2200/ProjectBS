using Unity.Cinemachine;
using UnityEngine;

public class HostileAIController : AIController 
{
    TargetingModule targetModule;
    AttackModule atkModule;
    AttackRangeModule atkRangeModule;
    SkillModule skillModule;

    CharacterBase targetCharacter;
    HitPointModule targetHPModule;
    AttackSlotModule targetSlotModule;

    TeamEliminateNotifier teamEliminateNotifier;

    float selfRadius;
    float targetRadius;
    
    protected override void OnPossess(CharacterBase newCharacter)
    {
        GameManager.OnUpdateController -= Think;
        GameManager.OnUpdateController += Think;

        newCharacter.OnFaint -= OnFaint;
        newCharacter.OnFaint += OnFaint;

        targetModule = Character.GetModule<TargetingModule>();
        atkModule = Character.GetModule<AttackModule>();
        atkRangeModule = Character.GetModule<AttackRangeModule>();
        skillModule = Character.GetModule<SkillModule>();
        teamEliminateNotifier = GetComponentInParent<TeamEliminateNotifier>();

        SetFocusTarget(null);
        targetModule.ForceScanReady();

        selfRadius = newCharacter.Status.colliderRadius;
    }
    protected override void OnUnpossess(CharacterBase oldCharacter)
    {
        GameManager.OnUpdateController -= Think;
        oldCharacter.OnFaint -= OnFaint;

        if (targetSlotModule)
        {
            targetSlotModule.Release(oldCharacter);
            targetSlotModule = null;
        }
    }

    protected override void OnFocusTargetChanged(GameObject oldTarget, GameObject newTarget)
    {
        base.OnFocusTargetChanged(oldTarget, newTarget);

        //기존 타겟 슬롯 반환
        if(targetSlotModule && Character)
        {
            targetSlotModule.Release(Character);
        }

        targetSlotModule = null;
        targetCharacter = null;
        targetHPModule = null;
        targetRadius = 0f;

        if (!newTarget) return;

        targetCharacter = newTarget.GetComponent<CharacterBase>();

        if (!targetCharacter) return;
      
        targetHPModule = targetCharacter.GetModule<HitPointModule>();
        targetRadius = targetCharacter.Status.colliderRadius;
        targetSlotModule = targetCharacter.GetModule<AttackSlotModule>();

        targetSlotModule?.TryReserve(Character);
    }

    protected override void Think(float deltaTime)
    {
        if (!Character || !Character.IsAlive) return;

        // 공격 중에는 목표를 변경하지 않음
        if (atkModule.IsAttacking)
        {
            CommandStop();

            if (IsTargetAlive())
            {
                CommandRotateToDirection(targetCharacter.transform.position - Character.transform.position);
            }

            return;
        }

        skillModule?.TryUseActiveSkill(Character);

        // 기존 목표가 죽거나 무효가 된 순간에는 즉시 재검색
        if (FocusTarget && !IsTargetAlive())
        {
            SetFocusTarget(null);
            targetModule.ForceScanReady();
        }

        // 유효한 목표가 있어도 주기적으로 더 가까운 적 검색
        if (targetModule.TryScanTarget(deltaTime, out GameObject newTarget))
        {
            SetFocusTarget(newTarget);
        }

        if (!IsTargetAlive())
        {
            CommandStop();
            return;
        }

        // 사거리 안이면 정지·회전·공격 처리
        if (TryHandleInAttackRange()) return;

        // 사거리 밖이면 기존 슬롯으로 접근
        if (targetSlotModule && targetSlotModule.TryGetOrReserveSlotPosition(Character, out Vector3 slotPosition))
        {
            CommandMoveToDestination(slotPosition, 0.02f);

            CommandRotateToDirection(targetCharacter.transform.position - Character.transform.position);
        }
        else
        {
            SetFocusTarget(null);
            targetModule.ForceScanReady();
            CommandStop();
        }
    }

    protected bool IsTargetAlive()
    {
        return FocusTarget
            && targetCharacter
            && targetCharacter.IsAlive
            && targetCharacter.gameObject.activeInHierarchy
            && targetCharacter.Team != Character.Team
            && targetHPModule?.IsHPEmpty != true;
    }

    //타겟과 가까워 졌을때 호출
    public void TryAttack()
    {
        //공격 쿨타임이 아니면 공격하기
        if (!atkModule.IsAttackCooldown)
        {
            atkModule.AttackTarget(new AttackInfo
            {
                target = targetCharacter,
                instigator = this,
                damageAmount = Character.finalStatus.damage
            });
        }
    }

    //공격 범위 내에 적이 있는지 보기
    public bool TryHandleInAttackRange()
    {
        AttackRangeState state = atkRangeModule.Evaluate(targetCharacter);

        if (state == AttackRangeState.InvalidTarget || state == AttackRangeState.OutOfRange)
        {
            return false;
        }

        CommandStop();

        CommandRotateToDirection(targetCharacter.transform.position - Character.transform.position
        );

        if (state == AttackRangeState.Ready)
        {
            TryAttack();
        }

        // 회전 중에도 true를 반환해서 이동 처리를 막음
        return true;
    }

    //죽었을 때, 내 모든 활동을 정지해야한다
    public void OnFaint()
    {
        CommandStop();
        teamEliminateNotifier.TeamEliminateCheck();
        if (targetSlotModule)
        {
            targetSlotModule.Release(Character);
            targetSlotModule = null;
        }
    }
}
