using UnityEngine;

public class MeleeAttackExecutor : AttackExecutor
{
    public override void Execute(in AttackInfo info)
    {
        if (!info.target || !info.target.IsAlive) return;

        HitPointModule targetHP = info.target.GetModule<HitPointModule>();
        if (!targetHP) return;

        GameObject attacker = info.instigator && info.instigator.gameObject ? info.instigator.Character.gameObject : gameObject;

        targetHP.TakeDamage(new DamageStruct
        {
            from = attacker,
            instigator = info.instigator,
            damageAmount = info.damageAmount
        });
    }
}


