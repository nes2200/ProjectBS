using UnityEngine;

[CreateAssetMenu(fileName = "Knight_Effect_DamageDebuff", menuName = "Scriptable Objects/Effect/Human/Knight/Knight_Effect_DamageDebuff")]
public class Knight_Effect_DamageDebuff : EffectContainer
{
    [SerializeField] int damage;

    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new Knight_Effect_DamageDebuff_Behavior(instance, target, damage);
    }
}
