using System.Collections.Generic;
using UnityEngine;

public class AttackRangeModule : CharacterModule
{
    public sealed override System.Type RegistrationType => typeof(AttackRangeModule);

    readonly HashSet<CharacterBase> targetsInRange = new();

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        targetsInRange.Clear();
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);
        targetsInRange.Clear();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!Owner) return;

        if (!other.TryGetComponent<CharacterBase>(out var target) || target == Owner) return;

        targetsInRange.Add(target);
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<CharacterBase>(out var target)) return;

        targetsInRange.Remove(target);
    }
    public bool Contains(CharacterBase target)
    {
        return target && target.IsAlive && targetsInRange.Contains(target);
    }

    public bool TryGetClosestTarget(out CharacterBase closestTarget)
    {
        closestTarget = null;
        float closestDistance = Mathf.Infinity;

        //검사 전 타겟 검증
        targetsInRange.RemoveWhere(target => !target || !target.IsAlive);

        foreach(CharacterBase target in targetsInRange)
        {
            if (target == Owner) continue;
            if (target.Team == Owner.Team) continue;

            float distance = (target.transform.position - transform.position).sqrMagnitude;
            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestTarget = target;
        }
        return closestTarget;
    }
}
