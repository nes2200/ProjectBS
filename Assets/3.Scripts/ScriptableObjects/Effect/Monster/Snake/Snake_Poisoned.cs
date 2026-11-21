using UnityEngine;

[CreateAssetMenu(fileName = "Snake_Poisoned", menuName = "Scriptable Objects/Effect/Monster/Snake/Snake_Poisoned")]
public class Snake_Poisoned : EffectContainer
{
    [SerializeField, Min(1)] int damagePerTick = 1;
    [SerializeField, Min(0.1f)] float tickInterval = 1f;

    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new PoisonDamageBehavior(instance, target, damagePerTick, tickInterval);
    }
}
