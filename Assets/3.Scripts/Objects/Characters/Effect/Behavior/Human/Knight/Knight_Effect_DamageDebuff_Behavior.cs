using UnityEngine;

public class Knight_Effect_DamageDebuff_Behavior : EffectBehavior
{
    readonly EffectInstance instance;
    readonly CharacterBase target;
    readonly HitPointModule hpModule;
    readonly int damage;

    bool subscribed;

    public Knight_Effect_DamageDebuff_Behavior(EffectInstance instance, CharacterBase target, int damage)
    {
        this.instance = instance;
        this.target = target;
        this.damage = damage;

        hpModule = target.GetModule<HitPointModule>();
    }

    public override void Apply()
    {
        if (!hpModule || subscribed) return;

        hpModule.OnAfterHit -= OnAfterHit;
        hpModule.OnAfterHit += OnAfterHit;
        subscribed = true;
    }

    public override void Remove()
    {
        if (!subscribed) return;

        if (hpModule)
            hpModule.OnAfterHit -= OnAfterHit;

        subscribed = false;
    }

    void OnAfterHit(DamageStruct hit)
    {
        // 이미 소비된 효과라면 실행하지 않아.
        if (!subscribed || !instance.IsActive) return;

        // Remove가 호출되면서 구독부터 해제돼.
        instance.ClearEffect();

        if (!target || !target.IsAlive || !hpModule) return;

        hpModule.TakeDamage(new DamageStruct
        {
            from = hit.from,
            instigator = hit.instigator,
            damageAmount = damage,
            defensePenetration = -1
        });
    }
}
