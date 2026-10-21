using UnityEngine;

public class StatModifierBehavior : EffectBehavior
{
    readonly CharacterBase target;
    readonly StatModifier[] modifiers;

    public StatModifierBehavior(CharacterBase target, StatModifier[] modifiers)
    {
        this.target = target;
        this.modifiers = (StatModifier[])modifiers.Clone();
    }

    public override void Apply()
    {
        target.AddStatModifiers(modifiers);
    }

    public override void Remove()
    {
        if (target) target.RemoveStatModifiers(modifiers);
    }
}
