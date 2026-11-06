using UnityEngine;

[CreateAssetMenu(fileName = "Knight_Passive_DamageDebuff", menuName = "Scriptable Objects/Skill/Human/Knight/Knight_Passive_DamageDebuff")]
public class Knight_Passive_DamageDebuff : PassiveSkillContainer
{
    [SerializeField]EffectContainer effect;
    AttackModule atkModule;
    
    public override void RegistrateEvents(CharacterBase owner)
    {
        AttackModule attackModule = owner.GetModule<AttackModule>();
        if (!attackModule) return;

        attackModule.OnAfterAttack -= ApplyEffect;
        attackModule.OnAfterAttack += ApplyEffect;
    }

    public override void UnregistrateEvent(CharacterBase owner)
    {
        AttackModule attackModule = owner.GetModule<AttackModule>();
        if (!attackModule) return;

        attackModule.OnAfterAttack -= ApplyEffect;
    }

    public void ApplyEffect(CharacterBase attacker, CharacterBase target)
    {
        if (!target || !target.IsAlive || !effect) return;

        EffectModule effectModule = target.GetModule<EffectModule>();
        if (!effectModule) return;

        effectModule.ApplyEffect(attacker, effect);
    }
}
