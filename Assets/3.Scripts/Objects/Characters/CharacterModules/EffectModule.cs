using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void EffectEvent(EffectContainer effect);

public class EffectModule : CharacterModule
{
    List<EffectInstance> buffs = new();
    List<EffectInstance> debuffs = new();

    public event EffectEvent OnEffectAdded;
    public event EffectEvent OnEffectRemoved;

    public override Type RegistrationType => typeof(EffectModule);
    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        foreach (EffectInstance buff in buffs)
        {
            buff.ClearEffect();
        }
        foreach (EffectInstance buff in debuffs)
        {
            buff.ClearEffect();
        }

        GameManager.OnUpdateCharacter -= UpdateAllEffectsDuration;
        GameManager.OnUpdateCharacter += UpdateAllEffectsDuration;
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        GameManager.OnUpdateCharacter -= UpdateAllEffectsDuration;

        foreach (EffectInstance buff in buffs)
        {
            buff.ClearEffect();
        }
        foreach (EffectInstance buff in debuffs)
        {
            buff.ClearEffect();
        }

        base.OnUnregistration(oldOwner);
    }

    public void GetEffect(EffectContainer effect)
    {
        if (effect.Category == EffectCategory.Buff) return; //리스트에 하나 추가하고 거기다가 집어넣기
    }

    public void UpdateAllEffectsDuration(float deltaTime)
    {
        foreach (EffectInstance buff in buffs)
        {
            buff.UpdateDuration(deltaTime);
        }
        foreach (EffectInstance buff in debuffs)
        {
            buff.UpdateDuration(deltaTime);
        }
    }
}
