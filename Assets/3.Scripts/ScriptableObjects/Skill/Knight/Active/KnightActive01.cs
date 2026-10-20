using UnityEngine;

[CreateAssetMenu(fileName = "KnightActive01", menuName = "Scriptable Objects/Skill/Knight/Active01")]
public class KnightActive01 : ActiveSkillContainer
{
    [SerializeField] EffectContainer effect;

    public override void UseSkill(CharacterBase user, CharacterBase target)
    {
        EffectModule module = user.GetModule<EffectModule>();
        module.ApplyEffect(user, effect);
    }
}
