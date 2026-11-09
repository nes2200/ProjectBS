using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Passive_DamageBuff", menuName = "Scriptable Objects/Skill/Monster/Wolf/Wolf_Passive_DamageBuff")]
public class Wolf_Passive_DamageBuff : PassiveSkillContainer
{
    [SerializeField] UnitDefinition pupDefinition;
    [SerializeField] EffectContainer effect;

    // The asset is shared; each Wolf needs its own callbacks and cleanup.
    readonly Dictionary<CharacterBase, Action> subscriptions = new();

    public override void RegistrateEvents(CharacterBase owner)
    {
        if (!owner) return;
        UnregistrateEvent(owner);

        TeamEliminateNotifier team = owner.GetComponentInParent<TeamEliminateNotifier>();
        StageManager stage = team ? team.Stage : null;
        StageCharacterRegistry registry = stage ? stage.CharacterRegistry : null;
        if (!registry || !pupDefinition || !effect) return;

        void OnCharacterDied(CharacterBase dead)
        {
            if (!owner || !owner.IsAlive || !owner.gameObject.activeInHierarchy ||
                !stage || stage.CurrentState != StageState.Battle || !dead ||
                owner.Team == TeamID.None || dead.Team != owner.Team) return;

            PrefabIdentity identity = dead.GetComponent<PrefabIdentity>();
            if (!identity || identity.UnitDefinition != pupDefinition) return;

            owner.GetModule<EffectModule>()?.ApplyEffect(owner, effect);
        }

        void OnOwnerFaint() => UnregistrateEvent(owner);
        void OnCharacterRemoved(CharacterBase removed)
        {
            if (removed == owner) UnregistrateEvent(owner);
        }

        subscriptions.Add(owner, () =>
        {
            if (registry)
            {
                registry.OnCharacterDied -= OnCharacterDied;
                registry.OnCharacterRemoved -= OnCharacterRemoved;
            }
            owner.OnFaint -= OnOwnerFaint;
            owner.OnCharacterRemoved -= OnCharacterRemoved;
        });

        registry.OnCharacterDied += OnCharacterDied;
        registry.OnCharacterRemoved += OnCharacterRemoved;
        owner.OnFaint += OnOwnerFaint;
        owner.OnCharacterRemoved += OnCharacterRemoved;
    }

    public override void UnregistrateEvent(CharacterBase owner)
    {
        if (ReferenceEquals(owner, null) || !subscriptions.TryGetValue(owner, out Action cleanup)) return;
        subscriptions.Remove(owner);
        cleanup();
    }

    void OnDisable()
    {
        foreach (Action cleanup in subscriptions.Values)
            cleanup();
        subscriptions.Clear();
    }
}
