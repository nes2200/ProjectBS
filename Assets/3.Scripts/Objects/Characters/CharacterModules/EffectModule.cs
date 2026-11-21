using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void EffectEvent(EffectContainer effect);

public class EffectModule : CharacterModule
{
    List<EffectInstance> effects = new();

    public event EffectEvent OnEffectAdded;
    public event EffectEvent OnEffectRemoved;
    public event Action<bool> OnStunChanged;

    public bool IsStunned { get; private set; }

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
            effect.OnEffectRemoved -= RefreshStunState;
        }

        base.OnUnregistration(oldOwner);
    }

    public bool HasActiveEffect(EffectContainer effect)
    {
        foreach (EffectInstance instance in effects)
        {
            if (instance.IsActive && instance.Effect == effect) return true;
        }
        return false;
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
        EffectInstance newInstance = CreateEffectInstance(causer, wantEffect);
        effects.Add(newInstance);
        newInstance.OnEffectRemoved += RefreshStunState;
        RefreshStunState();
        OnEffectAdded?.Invoke(wantEffect);
    }

    void RefreshStunState()
    {
        bool isStunned = effects.Exists(instance => instance.IsStun);
        if (IsStunned == isStunned) return;

        IsStunned = isStunned;
        OnStunChanged?.Invoke(IsStunned);
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
                effects[i].OnEffectRemoved -= RefreshStunState;
                effects.RemoveAt(i);
            }
        }
    }
}
