using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void EffectEvent(EffectContainer effect);

public class EffectModule : CharacterModule
{
    List<EffectInstance> effects = new();

    public event EffectEvent OnEffectAdded;
    public event EffectEvent OnEffectRemoved;

    public GameObject test;

    public override Type RegistrationType => typeof(EffectModule);
    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
     
        foreach (EffectInstance effect in effects)
        {
            effect.ClearEffect();
        }

        GameManager.OnUpdateCharacter -= UpdateEffects;
        GameManager.OnUpdateCharacter += UpdateEffects;
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        GameManager.OnUpdateCharacter -= UpdateEffects;
    
        foreach (EffectInstance effect in effects)
        {
            effect.ClearEffect();
        }

        base.OnUnregistration(oldOwner);
    }

    public void ApplyEffect(CharacterBase causer, EffectContainer wantEffect)
    {
        foreach (EffectInstance effect in effects)
        {
            if (effect.CanReapply(causer, wantEffect))
            {
                effect.Reapply();
                return;
            }
        }
        effects.Add(CreateEffectInstance(causer, wantEffect));
        OnEffectAdded?.Invoke(wantEffect);
    }

    public EffectInstance CreateEffectInstance(CharacterBase causer, EffectContainer effect)
    {
        EffectInstance newInstance = new();
        newInstance.Initialize(this, causer, effect);
        return newInstance;
    }

    public void UpdateEffects(float deltaTime)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            effects[i].UpdateDuration(deltaTime);

            if (!effects[i].IsActive)
            {
                OnEffectRemoved?.Invoke(effects[i].Effect);
                effects.RemoveAt(i);
            }
        }
    }
}
