using UnityEngine;

public enum EffectCategory
{
    Buff,
    Debuff
}

public abstract class EffectContainer : InfoContainer
{
    [SerializeField] EffectCategory category;
    [SerializeField, Min(0f)] float duration;
    [SerializeField, Min(1)] int maxStack;
    [SerializeField] bool refreshDurationOnReapply; //재부여시 지속시간 초기화
    [SerializeField] bool allowMultipleInstances; //재부여시 기존 효과가 있든 말든 그냥 새 효과가 생김.

    public EffectCategory Category => category;
    public float Duration => duration;
    public int MaxStack => maxStack;
    public bool RefreshDurationOnReapply => refreshDurationOnReapply;
    public bool AllowMultipleInstance => allowMultipleInstances;


    public abstract void Apply(CharacterBase target);
    public abstract void Remove();
}
