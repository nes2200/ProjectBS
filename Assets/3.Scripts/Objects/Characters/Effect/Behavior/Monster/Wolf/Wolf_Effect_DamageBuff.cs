using UnityEngine;

public class Wolf_Effect_DamageBuff : EffectBehavior
{
    readonly CharacterBase target;
    readonly EffectContainer effect;
    readonly EffectModule effectModule;
    readonly StatModifier[] modifiers;
    readonly GameObject indicator;
    bool applied;

    public Wolf_Effect_DamageBuff(EffectInstance instance, CharacterBase target, int damageBonus)
    {
        this.target = target;
        effect = instance.Effect;
        effectModule = target.GetModule<EffectModule>();
        indicator = effectModule ? effectModule.test : null;
        modifiers = new[] { new StatModifier { stat = StatType.Damage, value = damageBonus } };
    }

    public override void Apply()
    {
        if (applied || !target) return;
        applied = true;
        target.AddStatModifiers(modifiers);
        if (indicator) indicator.SetActive(true);
    }

    public override void Remove()
    {
        if (!applied) return;
        applied = false;
        if (target) target.RemoveStatModifiers(modifiers);

        // ClearEffect marks this instance inactive before calling Remove.
        // Keep the indicator on while another independent stack remains.
        if (indicator)
            indicator.SetActive(effectModule && effectModule.HasActiveEffect(effect));
    }
}
