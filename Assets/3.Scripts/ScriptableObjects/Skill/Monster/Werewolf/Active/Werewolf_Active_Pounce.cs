using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Werewolf_Active_Pounce", menuName = "Scriptable Objects/Skill/Monster/Werewolf/Werewolf_Active_Pounce")]
public class Werewolf_Active_Pounce : ActiveSkillContainer
{
    public override void UseSkill(CharacterBase user, CharacterBase target, Action requestCooldown)
    {
        throw new NotImplementedException();
    }
}
