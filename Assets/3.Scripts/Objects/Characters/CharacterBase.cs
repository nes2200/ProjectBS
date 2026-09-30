using System.Collections.Generic;
using UnityEngine;

public delegate void MovementEvent  (Vector3 move);
public delegate void LookAtEvent    (Vector3 direction);
public delegate void DamageEvent    (in DamageStruct info);
public delegate void RestoreEvent   (in RestoreStruct info);
public delegate void FaintEvent();

public enum TeamID
{
    None, TeamA, TeamB
}

public class CharacterBase : MonoBehaviour
{
    public event System.Action<CharacterBase> OnCharacterReady;
    public event System.Action<CharacterBase> OnCharacterRemoved;
    public event MovementEvent  OnMovement;
    public event LookAtEvent    OnLookAt;
    public event DamageEvent    OnDamage;
    public event RestoreEvent   OnRestore;
    public event FaintEvent     OnFaint;

    public void MovementNotify(Vector3 move)         => OnMovement?.Invoke(move);
    public void LookAtNotify(Vector3 direction)      => OnLookAt?.Invoke(direction);
    public void DamageNotify(in DamageStruct info)   => OnDamage?.Invoke(info);
    public void RestoreNotify(in RestoreStruct info) => OnRestore?.Invoke(info);
    public void FaintNotify()                        => OnFaint?.Invoke();

    ControllerBase _controller;
    public ControllerBase Controller => _controller;

    protected Vector3 _lookRotation;
    public Vector3 LookRotation => _lookRotation;

    //유닛 스테이터스가 담인 스크립터블 오브젝트
    [Header("Unit Status")]
    [SerializeField] UnitStatus _status;
    public UnitStatus Status => _status;

    public void SetStatus(UnitStatus newStatus)
    {
        _status = newStatus;
    }

    [Header("Unit Anchors")]
    [SerializeField] Transform hpBarAnchor;
    [SerializeField] Transform aimPosition;
    public Transform HPBarAnchor => hpBarAnchor;
    public Vector3 AimPosition => aimPosition ? aimPosition.position : transform.position;


    bool _isAlive = true;
    public bool IsAlive => _isAlive;

    //팀 구분용
    TeamID _team;
    public TeamID Team => _team;

    public virtual string DisplayName => Status.displayName;

    //모듈 저장하기
    //List : 추가/제거가 쉽다 <-> 메모리 효율이 낮고, 전체 순환이 느리다
    //Array : 추가/제거 어렵다 <-> 메모리 효율 높고, 전체 순환이 빠르다
    Dictionary<System.Type, CharacterModule> moduleDictionary = new();
    //추가 / 제거 / 검색
    public void AddModule(System.Type wantType, CharacterModule wantModule)
    {
        if(moduleDictionary.TryAdd(wantType, wantModule))
        {
            wantModule.OnRegistration(this);
        }
    }
    public void AddModuleWithoutRegistration(System.Type wantType, CharacterModule wantModule)
    {
        moduleDictionary.TryAdd(wantType, wantModule);
    }
    public void AddAllModuleFromObject(GameObject target)
    {
        if (!target) return;

        foreach(CharacterModule currentModule in target.GetComponentsInChildren<CharacterModule>())
        {
            AddModuleWithoutRegistration(currentModule.RegistrationType, currentModule);
        }
        foreach(CharacterModule currentModule in moduleDictionary.Values)
        {
            currentModule.OnRegistration(this);
        }
    }
    public void RemoveModule(System.Type wantType)
    {
        if(moduleDictionary.ContainsKey(wantType)) 
        {
            moduleDictionary[wantType].OnUnregistration(this);
            moduleDictionary.Remove(wantType);
        }
    }
    public void RemoveAllModule()
    {
        foreach(CharacterModule currentModule in moduleDictionary.Values)
        {
            currentModule.OnUnregistration(this);
        }
        moduleDictionary.Clear();
    }
    public T GetModule<T>() where T : CharacterModule
    {
        moduleDictionary.TryGetValue(typeof(T), out CharacterModule result);
        return result as T;
    }

    public virtual void OnPossessed(ControllerBase newController) {}
    public ControllerBase Possessed(ControllerBase from)
    {
        if (Controller) Unpossessed();

        _controller = from;
        AddAllModuleFromObject(gameObject);
        OnPossessed(Controller);
        OnCharacterReady?.Invoke(this);

        return Controller;
    }

    public virtual void OnUnpossessed(ControllerBase oldController) {}
    public void Unpossessed()
    {
        OnCharacterRemoved?.Invoke(this);
        if(Controller) OnUnpossessed(Controller);
        RemoveAllModule();
        _controller = null;
    }
    public bool Unpossessed(ControllerBase oldController)
    {
        if (Controller != oldController) return false;

        Unpossessed();
        return true;
    }

    public void SetAlive(bool value)
    {
        _isAlive = value;
    }

    public void SetTeam(TeamID newTeam)
    {
        _team = newTeam;
    }
}
