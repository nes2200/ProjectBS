using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

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

    bool waitingForActiveCooldown;
    int activeUseId;

    public override Type RegistrationType => typeof(SkillModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);

        activeUseId++;
        waitingForActiveCooldown = false;

        passiveSkill?.RegistrateEvents(newOwner);
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        GameManager.OnUpdateCharacter -= SkillCooldownUpdate;
        activeCooldownRemaining = 0f;
        passiveCooldownRemaining = 0f;

        passiveSkill?.UnregistrateEvent(oldOwner);

        base.OnUnregistration(oldOwner);
    }

    public void TryUseActiveSkill(CharacterBase target)
    {
        if (!activeSkill || waitingForActiveCooldown || activeCooldownRemaining > 0f) return;

        var skill = activeSkill;
        int useId = ++activeUseId;
        float cooldown = Mathf.Max(0f, skill.Cooltime);

        waitingForActiveCooldown = true;

        OnBeforeUse?.Invoke(Owner, target);

        // 이벤트 처리 중 스킬 교체나 모듈 해제가 발생했을 경우
        if (useId != activeUseId) return;

        skill.UseSkill(Owner, target, () => ActiveCooldownStart(useId, cooldown));

        OnAfterUse?.Invoke(Owner, target);
    }

    public void TryTriggerPassiveSkill()
    {
        if (!passiveSkill || passiveCooldownRemaining > 0f) return;
        PassiveCooldownStart();

    }

    void ActiveCooldownStart(int useId, float cooldown)
    {
        if (useId != activeUseId || !waitingForActiveCooldown)
            return;

        waitingForActiveCooldown = false;
        activeCooldownRemaining = cooldown;
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
        activeUseId++;
        waitingForActiveCooldown = false;
        OnActiveSkillChanged?.Invoke(activeSkill, newSkill);
        activeSkill = newSkill;
    }
    public void ChangePassiveSkill(PassiveSkillContainer newSkill)
    {
        OnPassiveSkillChanged?.Invoke(passiveSkill, newSkill);
        passiveSkill = newSkill;
    }
}
