using UnityEngine;

[CreateAssetMenu(fileName = "KnightEffect02", menuName = "Scriptable Objects/Effect/Knight/Effect02")]
public class KnightEffect02 : EffectContainer
{
    [SerializeField] int damage;

    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new KnightEffect02Behavior(instance, target, damage);
    }
}
