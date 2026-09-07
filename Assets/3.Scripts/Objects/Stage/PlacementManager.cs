using UnityEngine;

public delegate void UnitSpawnEvent(CharacterBase targetUnit);

public class PlacementManager : MonoBehaviour
{
    [Header("StageManager")]
    [SerializeField] StageManager stageManager;

    UnitDefinition selectedUnitDefinition;

    [Header("Each Team Parent")]
    [SerializeField] Transform teamA_Parent;
    [SerializeField] Transform teamB_Parent;

    [Header("Indicator")]
    [SerializeField] UnitPlaceIndicator indicator;
    public UnitPlaceIndicator Indicator => indicator;

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
        //게임이 멈춰있다면 생성 안함
        if (!GameManager.Instance.IsPlaying) return;

        ////마우스 누를때는 유닛 생성 안함
        //if (value) return;

        //준비 상태 아니라면 소환 안하기
        if (stageManager.CurrentState != StageState.Ready) return;

        //프리팹에 유닛이 저장되있지 않으면 생성 안함
        if(!selectedUnitDefinition || !selectedUnitDefinition.IsValid) return;

        //생성할 유닛 비용이 추가될 시 리미트 최고점을 넘으면 생성 안함
        if (!IsCostEnoughToSpawn(selectedUnitDefinition.Status.cost)) return;

        //생성 불가능한 위치라면 생성 안함
        if (!indicator.CanSpawn) return;

        //위치가 생성 불가한 위치인지 체크하고 불가하면 안함
        if (worldPosition.x > 0) return;

        //바닥에 맞았으면 유닛 생성
        //GameObject newUnit = ObjectManager.CreateObject(selectedUnitDefinition.UnitPrefab);
        GameObject newUnit = ObjectManager.CreateObjectWithoutRegistration(selectedUnitDefinition.UnitPrefab);

        //생성됬으면 등록하기
        if (newUnit)
        {
            if (!TryConfigureUnit(newUnit, selectedUnitDefinition, out CharacterBase targetCharacter))
            {
                Destroy(newUnit);
                return;
            }

            Transform unitParent = teamA_Parent;
            //유닛의 부모 설정으로 팀 배정
            newUnit.transform.SetParent(unitParent, false);
            newUnit.transform.position = indicator.GetCurrentIndicatorLoaction();
            newUnit.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            ObjectManager.RegistrationObject(newUnit);

            //추적할 적 유닛 등록하기
            TargetingModule targetModule = newUnit.GetComponent<TargetingModule>();
            if (targetModule)
            {
                //부모가 A면 적은 B, 부모가 B면 적은 A
                targetModule.SetHostileGroupParents((unitParent == teamA_Parent) ? teamB_Parent : teamA_Parent);
            }

            //배치한 만큼 코스트 증가시키기
            if (targetCharacter)
            {
                stageManager.CharacterRegistry.Register(targetCharacter, TeamID.TeamA);

                int unitCost = targetCharacter.Status.cost;
                stageManager.CostIncreasByUnitSpawn(unitCost);
            }
            OnUnitSpawn?.Invoke(targetCharacter);
        }
    }

    private void TryUnitDespawn(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (value) return;
        if (!indicator.TryGetLockedRemoveTarget(out CharacterBase targetCharacter)) return;
        if (targetCharacter.transform.position.x > 0f) return;
      
        int unitCost = targetCharacter.Status.cost;

        stageManager.CostDecreaseByUnitDespawn(unitCost);
        OnUnitDespawn?.Invoke(targetCharacter);
        stageManager.CharacterRegistry.Unregister(targetCharacter);
        ObjectManager.DestroyObject(targetCharacter.gameObject); 
    }

    //유닛 선택 버튼 클릭시 해당 유닛 정보를 받아오는 기능
    public void ChangeCurrentSelectedUnit(UnitDefinition newUnitDefinition)
    {
        selectedUnitDefinition = newUnitDefinition;
    }

    bool TryConfigureUnit(GameObject unit, UnitDefinition definition, out CharacterBase character)
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

        MaleUnitAppearance appearance = unit.GetComponent<MaleUnitAppearance>();
        if (!appearance) appearance = unit.AddComponent<MaleUnitAppearance>();
        if (!appearance.ApplyAppearance()) return false;

        character.SetStatus(definition.Status);
        return true;
    }

    public bool IsCostEnoughToSpawn(int unitCost)
    {
        return stageManager.IsCostEnoughToSpawn(unitCost);
    }
    
}
