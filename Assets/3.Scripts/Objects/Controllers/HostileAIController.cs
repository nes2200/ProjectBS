using Unity.Cinemachine;
using UnityEngine;

public class HostileAIController : AIController 
{
    TargetingModule targetModule;
    AttackModule atkModule;
    AttackRangeModule atkRangeModule;

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
        teamEliminateNotifier = GetComponentInParent<TeamEliminateNotifier>();

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
        //나 죽었으면 생각을 중지
        if (!Character || !Character.IsAlive) return;

        //공격 중에는 타겟 변경과 이동 금지
        if (atkModule.IsAttacking)
        {
            CommandStop();
            if (FocusTarget)
            {
                CommandRotateToDirection(FocusTarget.transform.position - transform.position);
            }
            return;
        }

        //바로 공격 가능한 적 공격
        if (TryHandleInAttackRange()) return;

        //공격 범위에 아무도 없을 때 스캔
        //적이 죽었다면 일단 탐색
        if (!IsTargetAlive())
        {
            SetFocusTarget(null);
            targetModule.ForceScanReady();

            if (targetModule.TryGetNewTarget(deltaTime, out GameObject newTarget))
            {
                SetFocusTarget(newTarget);
            }
        }

        if(!FocusTarget)
        {
            CommandStop();
            return;
        }

        if (targetSlotModule && targetSlotModule.TryGetOrReserveSlotPosition(Character, out Vector3 slotPosition))
        {
            CommandMoveToDestination(slotPosition, 0.02f);
            
            //이상한 방향 바라보는거 강제 제거
            Vector3 targetDirection = FocusTarget.transform.position - transform.position;
            CommandRotateToDirection(targetDirection);
        }
        else
        {
            //슬롯 꽉차서 예약 못했으면 다른놈 찾기
            Debug.Log($"{name}: 공격 슬롯 없음 - {FocusTarget.name}");
            SetFocusTarget(null);
            targetModule.ForceScanReady();
            CommandStop();
        }

        return;
    }

    protected bool IsTargetAlive()
    {
        if (!FocusTarget || targetHPModule?.IsHPEmpty == true)
        {
            return false;
        }
        return true;
    }

    //타겟과 가까워 졌을때 호출
    public void TryAttack()
    {
        //공격 쿨타임이 아니면 공격하기
        if (!atkModule.IsAttackCooldown)
        {
            atkModule.AttackTarget(new AttackInfo
            {
                target = FocusTarget,
                instigator = this,
                damageAmount = Character.Status.damage
            });
        }
    }

    //공격 범위 내에 적이 있는지 보기
    public bool TryHandleInAttackRange()
    {
        //현재 타겟이 범위 안이라면 그대로 유지
        if(targetCharacter && atkRangeModule.Contains(targetCharacter))
        {
            StopAndTryAttack();
            return true;
        }

        if (atkRangeModule.TryGetClosestTarget(out CharacterBase inRangeTarget))
        {
            SetFocusTarget(inRangeTarget.gameObject);
            StopAndTryAttack();
            return true;
        }

        return false;
    }

    private void StopAndTryAttack()
    {
        CommandStop();

        Vector3 direction = FocusTarget.transform.position - transform.position;
        CommandRotateToDirection(direction);
        TryAttack();
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
