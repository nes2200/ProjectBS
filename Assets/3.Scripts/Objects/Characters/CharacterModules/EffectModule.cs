using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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

        GameManager.OnUpdateCharacter -= UpdateEffects;
        GameManager.OnUpdateCharacter += UpdateEffects;
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        GameManager.OnUpdateCharacter -= UpdateEffects;

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

    public void ApplyEffect(CharacterBase causer, EffectContainer effect)
    {
        if (effect.Category == EffectCategory.Buff) 
        {
            foreach (EffectInstance buff in buffs)
            {
                if(buff.CanReapply(causer, effect))
                {
                    buff.Reapply();
                    return;
                }
            }
            buffs.Add(CreateEffectInstance(causer, effect));
            OnEffectAdded?.Invoke(effect);
        }
        if (effect.Category == EffectCategory.Debuff)
        {
            foreach (EffectInstance debuff in debuffs)
            {
                if (debuff.CanReapply(causer, effect))
                {
                    debuff.Reapply();
                    return;
                }
            }
            debuffs.Add(CreateEffectInstance(causer, effect));
            OnEffectAdded?.Invoke(effect);
        }
    }

    public EffectInstance CreateEffectInstance(CharacterBase causer, EffectContainer effect)
    {
        EffectInstance newInstance = new();
        newInstance.Initialize(causer, effect);
        return newInstance;
    }

    public void UpdateEffects(float deltaTime)
    {
        for(int i = buffs.Count - 1; i >= 0; i--)
        {
            buffs[i].UpdateDuration(deltaTime);

            if (!buffs[i].IsActive)
            {
                OnEffectRemoved?.Invoke(buffs[i].Effect);
                buffs.RemoveAt(i);
            }
        }
        for (int i = debuffs.Count - 1; i >= 0; i--)
        {
            debuffs[i].UpdateDuration(deltaTime);

            if (!debuffs[i].IsActive)
            {
                OnEffectRemoved?.Invoke(debuffs[i].Effect);
                debuffs.RemoveAt(i);
            }
        }
    }
}
