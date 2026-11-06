using UnityEngine;

[CreateAssetMenu(fileName = "Knight_Effect_DefenseUp", menuName = "Scriptable Objects/Effect/Human/Knight/Knight_Effect_DefenseUp")]
public class Knight_Effect_DefenseUp : EffectContainer
{
    [SerializeField] StatModifier[] modifiers;

    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new StatModifierBehavior(target, modifiers);
    }
}
