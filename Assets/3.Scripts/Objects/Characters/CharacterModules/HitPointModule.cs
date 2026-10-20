using System;
using UnityEngine;

public delegate void HitEvent(DamageStruct damage);
public delegate void HealEvent(RestoreStruct restore); 

public struct DamageStruct
{
    public GameObject from;
    public ControllerBase instigator;
    public int damageAmount;
}
public struct RestoreStruct
{
    public GameObject from;
    public ControllerBase instigator;
    public int restoreAmount;
}

public class HitPointModule : CharacterModule
{
    protected FillValue fill; /*= new FillValue(20, 20);*/
    public override Type RegistrationType => typeof(HitPointModule);

    public int HPMax => fill.Max;
    public int HPMin => fill.Min;
    public float HPPercent => fill.Percent;
    public bool IsFullHealth => fill.IsMax;
    public bool IsHPEmpty => fill.IsEmpyt;

    //이벤트 중계자
    public event FillValueChangeEvent OnHPChanged
    {
        add => fill.OnChanged += value;
        remove => fill.OnChanged -= value;
    }

    public event HitEvent OnBeforeHit;
    public event HitEvent OnAfterHit;

    public event RestoreEvent OnBeforeRestore;
    public event RestoreEvent OnAfterRestore;

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        SetFillValue(Owner.finalStatus);
        fill.OnChanged -= FaintCheck;
        fill.OnChanged += FaintCheck;
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);
        fill.OnChanged -= FaintCheck;
    }
    protected void SetFillValue(FinalStatus unitstatus)
    {
        fill = new FillValue(unitstatus.maxHP, unitstatus.maxHP);
    }

    public int TakeDamage(in DamageStruct damageInfo)
    {
        OnBeforeHit?.Invoke(damageInfo);

        int finalDamage = Mathf.Max(0, damageInfo.damageAmount - Owner.finalStatus.defense);
        fill.DecreaseCurrent(finalDamage);
        
        OnAfterHit?.Invoke(damageInfo);

        if (Owner.IsAlive) Owner.DamageNotify(damageInfo);

        return finalDamage;

    }
    public int TakeRestore(in RestoreStruct restoreInfo)
    {
        OnBeforeRestore?.Invoke(restoreInfo);
        fill.IncreaseCurrent(restoreInfo.restoreAmount);
        OnAfterRestore?.Invoke(restoreInfo);

        return restoreInfo.restoreAmount;
    }

    public void FaintCheck()
    {
        if (!IsHPEmpty || !Owner.IsAlive) return;
      
        Owner.SetAlive(false);
        Owner.FaintNotify();
    }
}
