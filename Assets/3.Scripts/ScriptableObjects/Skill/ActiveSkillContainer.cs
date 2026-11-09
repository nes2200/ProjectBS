using UnityEngine;

public abstract class ActiveSkillContainer : SkillContainer
{
    public abstract void UseSkill(CharacterBase user, CharacterBase target, System.Action requestCooldown);
    public virtual bool CanUse(CharacterBase user, CharacterBase target)
    {
        return user && user.IsAlive;
    }
}
