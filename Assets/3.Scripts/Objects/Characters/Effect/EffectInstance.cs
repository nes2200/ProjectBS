using System;
using UnityEngine;

public class EffectInstance : MonoBehaviour
{
    EffectContainer effect;
    float duration;
    float remainingDuration;
    int currentStack;
    CharacterBase causer;

    public CharacterBase Causer => causer;

    public event Action OnEffectAdded; //추가됨
    public event Action OnEffectUpdated; //중복으로 들어와 시간 연장, 스택 추가 등등 어떠한 방식으로는 내부 데이터 변경
    public event Action OnEffectRemoved; //제거됨

    //빈칸일수도 있으니까 null을 기본으로
    public void ClearEffect()
    {
        OnEffectRemoved?.Invoke();
        effect = null;
        duration = 0;
        remainingDuration = 0;
        currentStack = 0;
        causer = null;
    }

    public void GetEffect(EffectContainer wantEffect)
    {
        ClearEffect();

        effect = wantEffect;
        duration = effect.Duration;
        remainingDuration = duration;
        currentStack = 1;
        OnEffectAdded?.Invoke();
    }

    public void UpdateDuration(float deltaTime)
    {
        remainingDuration -= deltaTime;

        if (remainingDuration <= 0f) ClearEffect();
    }
    public void UpdateStack()
    {
        currentStack = Mathf.Min(currentStack + 1, effect.MaxStack);
        OnEffectUpdated?.Invoke(); //최대 스택일시 효과 발동과 같은것을 위하여
    }
}
