using UnityEngine;

[CreateAssetMenu(fileName = "Knight_Active_DefenseUp", menuName = "Scriptable Objects/Skill/Human/Knight/Knight_Active_DefenseUp")]
public class Knight_Active_DefenseUp : ActiveSkillContainer
{
    [SerializeField] EffectContainer effect;

    public override void UseSkill(CharacterBase user, CharacterBase target, System.Action requestCooldown)
    {
        EffectModule module = user.GetModule<EffectModule>();
        module.ApplyEffect(user, effect);
        requestCooldown?.Invoke();
    }
}