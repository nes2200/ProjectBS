using UnityEngine;

[CreateAssetMenu(fileName = "KnightEffect01", menuName = "Scriptable Objects/Effect/Knight/Effect01")]
public class KnightEffect01 : EffectContainer
{
    [SerializeField] StatModifier[] modifiers;

    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new StatModifierBehavior(target, modifiers);
    }
}
