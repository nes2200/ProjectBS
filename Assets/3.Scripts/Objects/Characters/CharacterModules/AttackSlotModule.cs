using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AttackSlotModule : CharacterModule
{
    public override Type RegistrationType => typeof(AttackSlotModule);

    [Header("Setting")]
    [SerializeField, Min(1)] int slotCount = 4;
    [SerializeField, Min(0f)] float margin = 0.05f;

    readonly Dictionary<CharacterBase, int> reservations = new();

    //첫 번째 공격자가 접근한 방향
    float baseAngle;

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        reservations.Clear();
        baseAngle = 0f;
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        reservations.Clear();
        baseAngle = 0f;
        base.OnUnregistration(oldOwner);
    }

    public bool TryGetOrReserveSlotPosition(CharacterBase attacker, out Vector3 slotPosition)
    {
        slotPosition = default;

        if (!TryReserve(attacker)) return false;

        int slotIndex = reservations[attacker];
        slotPosition = CalculateSlotPosition(slotIndex, attacker);

        return true;
    }

    //현재 타깃에 내 슬롯이 있는지
    public bool HasReservation(CharacterBase attacker)
    {
        return attacker && reservations.ContainsKey(attacker);
    }
    //기존 예약이 있거나 빈 슬롯이 있는지
    public bool CanReserve(CharacterBase attacker)
    {
        if (!attacker) return false;

        return reservations.ContainsKey(attacker) || reservations.Count < slotCount;
    }
    public bool TryReserve(CharacterBase attacker)
    {
        if (!Owner || !attacker) return false;

        //이미 예약되있음
        if (reservations.ContainsKey(attacker)) return true;

        //빈 슬롯 없음
        if (reservations.Count >= slotCount) return false;

        //첫 공격자의 접근 방향을 슬롯 기준으로 사용
        if(reservations.Count == 0)
        {
            Vector3 direction = attacker.transform.position - Owner.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;

            baseAngle = MathF.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        }

        int closestIndex = -1;
        float closestDistance = Mathf.Infinity;

        for(int i = 0; i < slotCount; i++)
        {
            if (IsOccupied(i)) continue;

            Vector3 candidate = CalculateSlotPosition(i, attacker);
            float distance = (candidate - attacker.transform.position).sqrMagnitude;
            if(distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex < 0) return false;

        reservations.Add(attacker, closestIndex);
        return true;
    }

    public void Release(CharacterBase attacker)
    {
        if (!attacker) return;

        reservations.Remove(attacker);

        //모두 반환됬다면 다음 첫 공격자가 기준을 정함
        if (reservations.Count == 0) baseAngle = 0f;
    }

    bool IsOccupied(int slotIndex)
    {
        foreach(int reservedIndex in reservations.Values)
        {
            if (reservedIndex == slotIndex) return true;
        }
        return false;
    }

    Vector3 CalculateSlotPosition(int slotIndex, CharacterBase attacker)
    {
        float interval = 360f / slotCount;
        float angle = baseAngle + interval * slotIndex;
        float radian = angle * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(Mathf.Cos(radian), 0f, Mathf.Sin(radian));

        float attackRange = attacker.GetModule<AttackRangeModule>().Range;
        float distance = Owner.Status.colliderRadius + attacker.Status.colliderRadius + Mathf.Max(0f, attackRange - margin);

        //타겟 위치만 따라가며 타겟 회전은 사용하지 않음
        return Owner.transform.position + direction * distance;
    }
}
