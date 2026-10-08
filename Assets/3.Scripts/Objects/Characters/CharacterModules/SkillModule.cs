using System;
using UnityEngine;

public class SkillModule : CharacterModule
{
    public override Type RegistrationType => typeof(SkillModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);
    }

    //스킬 사용
    //스킬 
}
