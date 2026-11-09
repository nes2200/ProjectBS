using System;
using System.Collections.Generic;
using UnityEngine;

public class StageCharacterRegistry : MonoBehaviour
{
    public event Action<CharacterBase> OnCharacterAdded;
    public event Action<CharacterBase> OnCharacterRemoved;
    public event Action<CharacterBase> OnCharacterDied;

    readonly List<CharacterBase> characters = new();
    readonly Dictionary<CharacterBase, FaintEvent> deathHandlers = new();
    public IReadOnlyList<CharacterBase> Characters => characters;

    public void Register(CharacterBase character, TeamID team)
    {
        if (!character || characters.Contains(character)) return;

        character.SetTeam(team);
        characters.Add(character);

        bool deathNotified = false;
        void OnFaint()
        {
            if (!character || character.IsAlive || deathNotified) return;
            deathNotified = true;
            OnCharacterDied?.Invoke(character);
        }

        deathHandlers.Add(character, OnFaint);
        character.OnFaint += OnFaint;
        character.OnCharacterRemoved += Unregister;
        OnCharacterAdded?.Invoke(character);
    }

    public void Unregister(CharacterBase character)
    {
        if (ReferenceEquals(character, null) || !characters.Remove(character)) return;

        if (deathHandlers.TryGetValue(character, out FaintEvent handler))
        {
            character.OnFaint -= handler;
            deathHandlers.Remove(character);
        }
        character.OnCharacterRemoved -= Unregister;
        OnCharacterRemoved?.Invoke(character);
    }

    public void Clear()
    {
        for (int i = characters.Count - 1; i >= 0; i--)
            Unregister(characters[i]);
    }

    void OnDestroy()
    {
        Clear();
        OnCharacterAdded = null;
        OnCharacterRemoved = null;
        OnCharacterDied = null;
    }
}
