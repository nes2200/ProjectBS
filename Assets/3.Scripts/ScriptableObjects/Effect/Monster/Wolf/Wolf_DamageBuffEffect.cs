using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_DamageBuffEffect", menuName = "Scriptable Objects/Effect/Monster/Wolf/Wolf_DamageBuffEffect")]
public class Wolf_DamageBuffEffect : EffectContainer
{
    [SerializeField, Min(0)] int damageBonus = 5;

    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new Wolf_Effect_DamageBuff(instance, target, damageBonus);
    }
}
