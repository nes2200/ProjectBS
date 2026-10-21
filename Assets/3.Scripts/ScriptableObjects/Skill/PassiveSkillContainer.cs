using UnityEngine;

public abstract class PassiveSkillContainer : SkillContainer
{
    public abstract void RegistrateEvents(CharacterBase owner);
    public abstract void UnregistrateEvent(CharacterBase owner);
}
