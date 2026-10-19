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

    public EffectCategory Category => category;
    public float Duration => duration;
    public int MaxStack => maxStack;
    public bool RefreshDurationOnReapply => refreshDurationOnReapply;


    public abstract void Apply();
    public abstract void Remove();
}
