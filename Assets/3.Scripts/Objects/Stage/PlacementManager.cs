using UnityEngine;
using UnityEngine.AI;

public delegate void UnitSpawnEvent(CharacterBase targetUnit);

public class PlacementManager : MonoBehaviour
{
    [Header("StageManager")]
    [SerializeField] StageManager stageManager;

    UnitDefinition selectedUnitData;
    TeamID selectedTeam;

    [Header("Each Team Parent")]
    [SerializeField] Transform teamA_Parent;
    [SerializeField] Transform teamB_Parent;

    [Header("Indicator")]
    [SerializeField] UnitPlaceIndicator indicator;
    public UnitPlaceIndicator Indicator => indicator;

    TeamLine teamLine;

    public static event UnitSpawnEvent OnUnitSpawn;
    public static event UnitSpawnEvent OnUnitDespawn;

    private void OnEnable()
    {
        InputManager.OnMouseLeftButton -= HandlePlacementClick;
        InputManager.OnMouseLeftButton += HandlePlacementClick;

        InputManager.OnUnitSelect -= ChangeCurrentSelectedUnit;
        InputManager.OnUnitSelect += ChangeCurrentSelectedUnit;
    }
    private void OnDisable()
    {
        InputManager.OnMouseLeftButton -= HandlePlacementClick;
        InputManager.OnUnitSelect -= ChangeCurrentSelectedUnit;
    }

    public void SetTeamLine(TeamLine newTeamLine)
    {
        teamLine = newTeamLine;
    }

    public bool IsInsideTeamArea(TeamID team, Vector3 worldPosition, float footprintRadius = 0f)
    {
        return teamLine != null && teamLine.Contains(team, worldPosition, footprintRadius);
    }

    public bool CanRemove(CharacterBase target)
    {
        if (!target) return false;
        if (!GameManager.Instance.IsPlaying) return false;
        if (stageManager.CurrentState != StageState.Ready) return false;

        if (stageManager.IsSandbox)
        {
            return target.Team == TeamID.TeamA || target.Team == TeamID.TeamB;
        }

        return target.Team == TeamID.TeamA && IsInsideTeamArea(TeamID.TeamA, target.transform.position);
    }

    private void HandlePlacementClick(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (value) return;
        if (GameManager.Input.IsMouseOverUI) return;

        switch (indicator.CurrentMode)
        {
            case UnitPlacementMode.Place:
                TryUnitSpawn(value, screenPosition, worldPosition);
                break;
            case UnitPlacementMode.Remove:
                TryUnitDespawn(value, screenPosition, worldPosition);
                break;
        }
    }

    private void TryUnitSpawn(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (stageManager.CurrentState != StageState.Ready) return;
        if (!selectedUnitData || !selectedUnitData.Status) return;
        if (!IsCostEnoughToSpawn(selectedUnitData.Status.Cost)) return;
        if (!indicator.CanSpawn) return;

        GameObject spawnPrefab = selectedUnitData.GetRandomPrefab();
        Vector3 position = indicator.GetCurrentIndicatorLoaction();
        Quaternion rotation = selectedTeam == TeamID.TeamA
            ? Quaternion.Euler(0f, 90f, 0f)
            : Quaternion.Euler(0f, 270f, 0f);

        if (!TrySpawnUnit(spawnPrefab, selectedUnitData.Status, selectedTeam,
                position, rotation, out CharacterBase targetCharacter))
            return;

        // 준비 단계에서의 배치 비용과 배치 이벤트만 이곳에서 처리한다.
        if (!stageManager.IsSandbox)
            stageManager.CostIncreasByUnitSpawn(targetCharacter.Status.Cost);
        OnUnitSpawn?.Invoke(targetCharacter);
    }

    // 위치를 정하는 쪽에서 프리팹을 한 번 선택해 전달한다.
    // Indicator, 선택 중인 유닛, 비용, 전투 상태에 의존하지 않는 공통 생성 함수.
    public bool TrySpawnUnit(GameObject prefab, UnitStatus status, TeamID team,
        Vector3 position, Quaternion rotation, out CharacterBase spawnedCharacter)
    {
        spawnedCharacter = null;
        if (!prefab || !status || !stageManager || !stageManager.CharacterRegistry)
            return false;
        if (team != TeamID.TeamA && team != TeamID.TeamB) return false;

        Transform unitParent = team == TeamID.TeamA ? teamA_Parent : teamB_Parent;
        Transform hostileParent = team == TeamID.TeamA ? teamB_Parent : teamA_Parent;
        if (!unitParent || !hostileParent) return false;

        GameObject newUnit = ObjectManager.CreateObjectWithoutRegistration(prefab, unitParent);
        if (!newUnit) return false;

        // Agent를 끄고 배치한 뒤 다시 켜서 NavMesh 위치를 맞춘다.
        NavMeshAgent agent = newUnit.GetComponent<NavMeshAgent>();
        if (agent) agent.enabled = false;
        newUnit.transform.SetPositionAndRotation(position, rotation);

        if (!TryConfigureUnit(newUnit, status, out CharacterBase character))
        {
            Destroy(newUnit);
            return false;
        }
        character.SetTeam(team);

        if (agent)
        {
            agent.enabled = true;
            if (!agent.isOnNavMesh || !agent.Warp(position))
            {
                Destroy(newUnit);
                return false;
            }
        }

        // 위치, 스탯, 팀 설정이 끝난 후 컨트롤러와 모듈을 초기화한다.
        ObjectManager.RegistrationObject(newUnit);
        TargetingModule targeting = newUnit.GetComponent<TargetingModule>();
        if (targeting) targeting.SetHostileGroupParents(hostileParent);
        stageManager.CharacterRegistry.Register(character, team);
        spawnedCharacter = character;
        return true;
    }

    private void TryUnitDespawn(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (value) return;
        if (!indicator.TryGetLockedRemoveTarget(out CharacterBase targetCharacter)) return;
        if (!CanRemove(targetCharacter)) return;
      
        int unitCost = targetCharacter.Status.Cost;

        if (!stageManager.IsSandbox) stageManager.CostDecreaseByUnitDespawn(unitCost);
        OnUnitDespawn?.Invoke(targetCharacter);
        stageManager.CharacterRegistry.Unregister(targetCharacter);
        ObjectManager.DestroyObject(targetCharacter.gameObject); 
    }

    //유닛 선택 버튼 클릭시 해당 유닛 정보를 받아오는 기능
    public void ChangeCurrentSelectedUnit(UnitDefinition newUnitData, TeamID newTeam)
    {
        selectedUnitData = newUnitData;
        selectedTeam = newTeam;
    }

    bool TryConfigureUnit(GameObject unit, UnitStatus status, out CharacterBase character)
    {
        character = unit.GetComponent<CharacterBase>();
        if (!character)
        {
            Debug.LogError($"[PlacementManager] '{unit.name}'에 CharacterBase가 없습니다.");
            return false;
        }

        Animator animator = unit.GetComponentInChildren<Animator>(true);
        if (!animator)
        {
            Debug.LogError($"[PlacementManager] '{unit.name}'에 Animator가 없습니다.");
            return false;
        }

        UnitAppearance appearance = unit.GetComponent<UnitAppearance>();
        if (appearance && !appearance.ApplyAppearance()) return false;

        character.SetStatus(status);
        return true;
    }

    public bool IsCostEnoughToSpawn(int unitCost)
    {
        return stageManager.IsSandbox || stageManager.IsCostEnoughToSpawn(unitCost);
    }
    
}
