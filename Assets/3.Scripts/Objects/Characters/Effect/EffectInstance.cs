using System;
using UnityEngine;

public class EffectInstance
{
    EffectModule effectModule;

    EffectBehavior behavior;
    EffectContainer effect;
    float finalduration;
    float remainingDuration;
    int currentStack;
    CharacterBase causer;

    public EffectContainer Effect => effect;
    public CharacterBase Causer => causer;

    public bool IsActive => effect != null;

    public event Action OnEffectAdded; //추가됨
    public event Action OnEffectUpdated; //중복으로 들어와 시간 연장, 스택 추가 등등 어떠한 방식으로는 내부 데이터 변경
    public event Action OnEffectRemoved; //제거됨

    //빈칸일수도 있으니까 null을 기본으로
    public void ClearEffect()
    {
        if (effect == null) return;

        // 콜백 중 다시 제거를 요청해도 중복 실행하지 않도록 먼저 비활성 상태로 만들기.
        EffectBehavior removingBehavior = behavior;

        effect = null;
        behavior = null;

        removingBehavior?.Remove();

        finalduration = 0;
        remainingDuration = 0;
        currentStack = 0;
        causer = null;

        OnEffectRemoved?.Invoke();
    }

    public void Initialize(EffectModule module, CharacterBase causer, EffectContainer wantEffect)
    {
        ClearEffect();

        effectModule = module;

        effect = wantEffect;
        finalduration = effect.Duration;
        remainingDuration = finalduration;
        currentStack = 1;
        this.causer = causer;

        behavior = effect.CreateBehavior(this, effectModule.Owner);
        behavior.Apply();

        OnEffectAdded?.Invoke();
    }

    public bool CanReapply(CharacterBase causer, EffectContainer wantEffect)
    {
        if(effect == wantEffect && this.causer == causer)
        {
            if (effect.AllowMultipleInstance) return false;
            return true;
        }
        return false;
    }

    public void UpdateDuration(float deltaTime)
    {
        remainingDuration -= deltaTime;
        OnEffectUpdated?.Invoke();

        if (remainingDuration <= 0f) ClearEffect();
    }

    public void Reapply()
    {
        currentStack = Mathf.Min(currentStack + 1, effect.MaxStack);
        if (effect.RefreshDurationOnReapply) RefreshDuration();
        OnEffectUpdated?.Invoke(); //최대 스택일시 효과 발동과 같은것을 위하여
    }
    public void RefreshDuration()
    {
        remainingDuration = finalduration;
    }
}
