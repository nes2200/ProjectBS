using UnityEngine;

[CreateAssetMenu(fileName = "SnakeNaga_Poisoned", menuName = "Scriptable Objects/Effect/Monster/SnakeNaga/SnakeNaga_Poisoned")]
public class SnakeNaga_Poisoned : EffectContainer
{
    [SerializeField, Min(1)] int damagePerTick = 2;
    [SerializeField, Min(0.1f)] float tickInterval = 0.5f;

    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new PoisonDamageBehavior(instance, target, damagePerTick, tickInterval);
    }
}
