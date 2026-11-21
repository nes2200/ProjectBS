using UnityEngine;

internal sealed class PoisonDamageBehavior : EffectBehavior
{
    const double TickTimeTolerance = 1e-9d;

    readonly EffectInstance instance;
    readonly CharacterBase target;
    readonly HitPointModule hitPoints;
    readonly DamageStruct damage;
    readonly double tickInterval;
    double timeUntilTick;

    public PoisonDamageBehavior(EffectInstance instance, CharacterBase target, int damagePerTick, float tickInterval)
    {
        this.instance = instance;
        this.target = target;
        this.tickInterval = Mathf.Max(0.1f, tickInterval);
        hitPoints = target.GetModule<HitPointModule>();

        var causer = instance.Causer;
        damage = new DamageStruct
        {
            from = causer ? causer.gameObject : null,
            instigator = causer ? causer.Controller : null,
            damageAmount = damagePerTick,
            defensePenetration = -1
        };
    }

    public override void Apply() => timeUntilTick = tickInterval;

    public override void Remove() {}

    public override void Update(double elapsedTime)
    {
        timeUntilTick -= elapsedTime;
        while (timeUntilTick <= TickTimeTolerance && instance.IsActive && hitPoints &&
            target && target.IsAlive && target.gameObject.activeInHierarchy)
        {
            timeUntilTick += tickInterval;
            hitPoints.TakeDamage(damage);
        }
    }
}
