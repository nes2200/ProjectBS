using UnityEngine;

[CreateAssetMenu(fileName = "HermitKing_Stun", menuName = "Scriptable Objects/Effect/Monster/HermitKing/HermitKing_Stun")]
public class HermitKing_Stun : EffectContainer
{
    public override EffectBehavior CreateBehavior(EffectInstance instance, CharacterBase target)
    {
        return new StunBehavior(target);
    }

    sealed class StunBehavior : EffectBehavior
    {
        readonly CharacterBase target;

        public StunBehavior(CharacterBase target)
        {
            this.target = target;
        }

        public override void Apply()
        {
            MovementModule movement = target.GetModule<MovementModule>();
            movement?.StopMovement();
            movement?.Rotate(Vector3.zero);
            target.GetModule<AttackModule>()?.OnAttackAnimationEnd();
        }

        public override void Remove() { }
    }
}
