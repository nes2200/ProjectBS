using System;
using UnityEngine;

public delegate void SkillUseEvent(CharacterBase user, CharacterBase target);
public delegate void SkillChangeEvent(SkillContainer oldSkill, SkillContainer newSkill);

public class SkillModule : CharacterModule
{
    public event SkillUseEvent OnBeforeUse;
    public event SkillUseEvent OnAfterUse;
    public event SkillChangeEvent OnActiveSkillChanged;
    public event SkillChangeEvent OnPassiveSkillChanged;

    [SerializeField] ActiveSkillContainer activeSkill;
    [SerializeField] PassiveSkillContainer passiveSkill;
    public ActiveSkillContainer ActiveSkill => activeSkill;
    public PassiveSkillContainer PassiveSkill => passiveSkill;

    float activeCooldownRemaining;
    float passiveCooldownRemaining;

    public override Type RegistrationType => typeof(SkillModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        GameManager.OnUpdateCharacter -= SkillCooldownUpdate;
        activeCooldownRemaining = 0f;
        passiveCooldownRemaining = 0f;

        base.OnUnregistration(oldOwner);
    }

    public void TryUseActiveSkill(CharacterBase target)
    {
        if (!activeSkill || activeCooldownRemaining > 0f) return;
        OnBeforeUse?.Invoke(Owner, target);
        activeSkill.UseSkill(Owner, target);
        OnAfterUse?.Invoke(Owner, target);
    }
   

    public void TryTriggerPassiveSkill()
    {
        if (!passiveSkill || passiveCooldownRemaining > 0f) return;
    }

    void ActiveCooldownStart()
    {
        activeCooldownRemaining = Mathf.Max(0f, activeSkill.Cooltime);
        RegisterCooldownUpdate();
    }

    void PassiveCooldownStart()
    {
        passiveCooldownRemaining = Mathf.Max(0f, passiveSkill.Cooltime);
        RegisterCooldownUpdate();
    }

    void RegisterCooldownUpdate()
    {
        GameManager.OnUpdateCharacter -= SkillCooldownUpdate;

        if (activeCooldownRemaining > 0f || passiveCooldownRemaining > 0f) 
            GameManager.OnUpdateCharacter += SkillCooldownUpdate;
    }

    void SkillCooldownUpdate(float deltaTime)
    {
        activeCooldownRemaining = Mathf.Max(0f, activeCooldownRemaining - deltaTime);
        passiveCooldownRemaining = Mathf.Max(0f, passiveCooldownRemaining - deltaTime);

        // 둘 다 끝나야 갱신 해제
        if (activeCooldownRemaining <= 0f && passiveCooldownRemaining <= 0f)
            GameManager.OnUpdateCharacter -= SkillCooldownUpdate;
    }

    public void ChangeActiveSkill(ActiveSkillContainer newSkill)
    {
        OnActiveSkillChanged?.Invoke(activeSkill, newSkill);
        activeSkill = newSkill;
    }
    public void ChangePassiveSkill(PassiveSkillContainer newSkill)
    {
        OnPassiveSkillChanged?.Invoke(passiveSkill, newSkill);
        passiveSkill = newSkill;
    }
}
