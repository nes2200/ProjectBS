using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void TargetScanEvent(Vector3 target);

public class TargetingModule : CharacterModule
{
    public sealed override Type RegistrationType => typeof(TargetingModule);

    [SerializeField] Transform _hostileGroupParent;
    public Transform HostileGroupParent => _hostileGroupParent;

    [SerializeField, Min(0.01f)] float scanInterval = 0.2f;

    readonly List<CharacterBase> hostileCharacters = new(100);
    float scanCooltime;

    public event TargetScanEvent OnTargetScanned;

    public void ClaimOnTargetScanned(Vector3 target)
    {
        OnTargetScanned?.Invoke(target);
    }

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);

        scanCooltime = UnityEngine.Random.Range(0f, scanInterval);

        StageManager.OnBattleStart -= OnBattleStart;
        StageManager.OnBattleStart += OnBattleStart;
    }

    public override void OnUnregistration(CharacterBase oldOwner)
    {
        StageManager.OnBattleStart -= OnBattleStart;
        hostileCharacters.Clear();

        base.OnUnregistration(oldOwner);
    }

    private void CacheHostileCharacters()
    {
        hostileCharacters.Clear();

        if (!HostileGroupParent) return;

        foreach (Transform child in HostileGroupParent)
        {
            if (child.TryGetComponent<CharacterBase>(out var character))
            {
                hostileCharacters.Add(character);
            }
        }
    }

    public void SetHostileGroupParents(Transform newHostileParent)
    {
        _hostileGroupParent = newHostileParent;
        CacheHostileCharacters();
    }

    // true: 이번 호출에서 검색을 실행함. 결과는 null일 수도 있음.
    // false: 검색 대기 중.
    public bool TryScanTarget(float deltaTime, out GameObject target)
    {
        target = null;

        if (!Owner) return false;

        scanCooltime -= deltaTime;
        if (scanCooltime > 0f) return false;

        // 반복 검색 시점도 유닛마다 분산
        scanCooltime = UnityEngine.Random.Range(scanInterval * 0.5f, scanInterval);

        target = ScanClosestTarget();

        if (target)
        {
            OnTargetScanned?.Invoke(target.transform.position);
        }

        return true;
    }

    public GameObject ScanClosestTarget()
    {
        if (!Owner) return null;

        CharacterBase closestTarget = null;
        float closestDistance = Mathf.Infinity;
        Vector3 origin = Owner.transform.position;

        for (int i = 0; i < hostileCharacters.Count; i++)
        {
            CharacterBase target = hostileCharacters[i];

            if (!target || target == Owner || !target.IsAlive || !target.gameObject.activeInHierarchy || target.Team == Owner.Team)
            {
                continue;
            }

            AttackSlotModule slots = target.GetModule<AttackSlotModule>();

            if (!slots || !slots.CanReserve(Owner)) continue;

            Vector3 offset = target.transform.position - origin;
            offset.y = 0f;

            float distance = offset.sqrMagnitude;
            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestTarget = target;
        }

        return closestTarget ? closestTarget.gameObject : null;
    }

    public void ForceScanReady()
    {
        scanCooltime = 0f;
    }

    private void OnBattleStart()
    {
        CacheHostileCharacters();
    }
}