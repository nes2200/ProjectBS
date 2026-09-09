using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

public enum UnitPlacementMode
{
    Place, Remove
}


public class UnitPlaceIndicator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float navMeshCheckRadius = 0.1f;
    [SerializeField] float heightOffset = 5f;
    [SerializeField] LayerMask floorLayer;
    [SerializeField] LayerMask unitLayer;
    [SerializeField] LayerMask removeTargetLayer;
    [SerializeField] PlacementManager placementManager;

    [Header("Indicator")]
    [SerializeField] GameObject indicator;
    [SerializeField] Material indicatorMat;
    [SerializeField] DecalProjector decal;
    [SerializeField] BoxCollider indicatorCollider;
    [SerializeField] UnitPlacementMode currentMode = UnitPlacementMode.Place;

    //현재 유닛 설치모드인가 제거모드인가
    public UnitPlacementMode CurrentMode => currentMode;
    public bool IsRemoveMode => currentMode == UnitPlacementMode.Remove;

    CharacterBase lockedRemovedTarget;

    Camera mainCam;
    Material runtimeIndicatorMat;

    TeamID selectedTeam;

    readonly int tintColorPorpertyID = Shader.PropertyToID("_Tint");

    float size;
    bool selected = false;

    bool _canSpawn;
    public bool CanSpawn => _canSpawn;

    private void OnEnable()
    {
        InputManager.OnUnitSelect -= ChangeCurrentSelectedUnit;
        InputManager.OnUnitSelect += ChangeCurrentSelectedUnit;

        InputManager.OnMouseMove -= MoveToMouse;
        InputManager.OnMouseMove += MoveToMouse;

        CameraManager.OnCameraViewChanged -= RefreshIndicatorStatus;
        CameraManager.OnCameraViewChanged += RefreshIndicatorStatus;

        StageManager.OnBattleStart -= DisableIndicator;
        StageManager.OnBattleStart += DisableIndicator;

        UI_StageScreen.OnMenuOpen -= UpdateIndicatorStatusByMenuOpen;
        UI_StageScreen.OnMenuOpen += UpdateIndicatorStatusByMenuOpen;

        UI_StageScreen.OnMenuClose -= UpdateIndicatorStatusByMenuClose;
        UI_StageScreen.OnMenuClose += UpdateIndicatorStatusByMenuClose;

        PlacementManager.OnUnitSpawn -= OnUnitSpawned;
        PlacementManager.OnUnitSpawn += OnUnitSpawned;

        PlacementManager.OnUnitDespawn -= OnUnitDespawned;
        PlacementManager.OnUnitDespawn += OnUnitDespawned;

        if(runtimeIndicatorMat == null)
        {
            runtimeIndicatorMat = new Material(indicatorMat);
            decal.material = runtimeIndicatorMat;
        }
        runtimeIndicatorMat.SetColor(tintColorPorpertyID, Color.gray);

        mainCam = Camera.main;
    }
    private void OnDisable()
    {
        InputManager.OnUnitSelect -= ChangeCurrentSelectedUnit;
        InputManager.OnMouseMove -= MoveToMouse;
        CameraManager.OnCameraViewChanged -= RefreshIndicatorStatus;
        StageManager.OnBattleStart -= DisableIndicator;
        UI_StageScreen.OnMenuOpen -= UpdateIndicatorStatusByMenuOpen;
        UI_StageScreen.OnMenuClose -= UpdateIndicatorStatusByMenuClose;
        PlacementManager.OnUnitSpawn -= OnUnitSpawned;
        PlacementManager.OnUnitDespawn -= OnUnitDespawned;
    }

    private void OnDestroy()
    {
        if (runtimeIndicatorMat != null)
        {
            Destroy(runtimeIndicatorMat);
        }
    }

    void MoveToMouse(Vector2 screenPosition, Vector3 worldPosition)
    {
        //게임이 멈춘 상태라면 갱신 안함
        if (!GameManager.Instance.IsPlaying)
        {
            return;
        }

        //UI위에 있으면 그냥 사라질거임
        if(GameManager.Input.IsMouseOverUI)
        {
            lockedRemovedTarget = null;
            SetIndicatorActive(false);
            _canSpawn = false;
            return;
        }
        UpdateIndicatorStatus(screenPosition);
    }

    void UpdateIndicatorStatus(Vector2 screenPosition)
    {
        switch (currentMode)
        {
            case UnitPlacementMode.Place:
                UpdatePlaceIndicator(screenPosition);
                break;
            case UnitPlacementMode.Remove:
                UpdateRemoveIndicator(screenPosition);
                break;
        }
    }
    void UpdatePlaceIndicator(Vector2 screenPosition)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, floorLayer))
        {
            SetIndicatorActive(true);

            transform.position = hit.point + new Vector3(0f, heightOffset, 0f);
            indicatorCollider.center = new Vector3(0f, 0f, heightOffset);
            CheckSpawnable(hit.point);
        }
        else
        {
            SetIndicatorActive(false);
        }
    }
    void UpdateRemoveIndicator(Vector2 screenPosition)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPosition);

        bool isHit = Physics.Raycast(ray, out RaycastHit hit, 1000f, removeTargetLayer, QueryTriggerInteraction.Collide);

        if (!isHit)
        {
            ClearRemoveTarget();
            return;
        }

        CharacterBase character = hit.collider.GetComponentInParent<CharacterBase>();
        if (!placementManager.CanRemove(character))
        {
            ClearRemoveTarget();
            return;
        }
        lockedRemovedTarget = character;

        SetIndicatorActive(true);
        transform.position = character.transform.position + Vector3.up * heightOffset;

        UpdateIndicatorColor(false);
    }

    void CheckSpawnable(Vector3 floorPosition)
    {
        //선택된 유닛이 없다면 ,지금 마우스가 가리키는 오브젝트가 UI라면 생성 안함
        if (!selected)
        {
            _canSpawn = false;
            return;
        }

        //소환할 유닛이 소환 가능 지역에 있나
        bool isOnNavMesh = NavMesh.SamplePosition(floorPosition, out NavMeshHit hit, navMeshCheckRadius, NavMesh.AllAreas);
        //소환할 유닛이 원하는 영역에 있나
        bool isInsidePlayerArea = placementManager.IsInsideTeamArea(selectedTeam, floorPosition, size);
        bool spawnableCheck = isOnNavMesh && isInsidePlayerArea;
       
        //인디케이터와 유닛 충돌 체크
        if (spawnableCheck)
        {
            spawnableCheck = CheckUnitSpawnableOnCurrentLocation();
        }

        //이전 상태와 다를때 한 번만 색상이 바뀌게
        if(_canSpawn != spawnableCheck)
        {
            _canSpawn = spawnableCheck;
            UpdateIndicatorColor(_canSpawn);
        }
    }

    //인디케이터와 유닛이 닿았는지 체크
    public bool CheckUnitSpawnableOnCurrentLocation()
    {
        if (!indicatorCollider) return true; 

        //박스 콜라이더 월드 좌표 중심 계산
        Vector3 center = indicatorCollider.bounds.center;
        //박스콜라이더의 반경
        Vector3 halfExtents = indicatorCollider.bounds.extents;
        //박스의 회전값
        Quaternion orientation = indicatorCollider.transform.rotation;

        //영역 내 'unitLayer'를 가진 콜라이더가 하나라도 있는지 검사
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, orientation, unitLayer);

        return hitColliders.Length == 0;
    }

    public bool TryGetLockedRemoveTarget(out CharacterBase target)
    {
        target = lockedRemovedTarget;

        return currentMode == UnitPlacementMode.Remove && indicator.activeSelf && placementManager.CanRemove(target);
    }
    void ClearRemoveTarget()
    {
        lockedRemovedTarget = null;
        SetIndicatorActive(false);
    }

    //설치 제거 토글
    public void ToggleMode()
    {
        SetMode(CurrentMode == UnitPlacementMode.Place ? UnitPlacementMode.Remove : UnitPlacementMode.Place);
    }
    public void SetMode(UnitPlacementMode newMode)
    {
        lockedRemovedTarget = null;
        currentMode = newMode;

        _canSpawn = false;
        RefreshIndicatorStatus();
    }

    //마우스 이동 업데이트가 멈췄을 경우 인디케이터 상태 강제 리프레시 해주기
    public void RefreshIndicatorStatus()
    {
        MoveToMouse(InputManager.CursorScreenPosition, InputManager.CursorWorldPosition);
    }

    public void OnUnitSpawned(CharacterBase _)
    {
        _canSpawn = false;
        UpdateIndicatorColor(false);
    }
    public void OnUnitDespawned(CharacterBase despawnedCharacter)
    {
        if(lockedRemovedTarget == despawnedCharacter)
        {
            ClearRemoveTarget();
        }
        StartCoroutine(CoRefreshAfterDespawn());
    }
    IEnumerator CoRefreshAfterDespawn()
    {
        //오브젝트 Destroy가 프레임 종료로 실제로 처리되기까지 대기
        yield return null;

        //transfomr과 collider 변경을 물리시스템에 적용
        Physics.SyncTransforms();
        RefreshIndicatorStatus();
    }

    public Vector3 GetCurrentIndicatorLoaction()
    {
        return transform.position - new Vector3(0f, heightOffset, 0f); 
    }

    void SetIndicatorActive(bool visible)
    {
        if (indicator.activeSelf != visible)
        {
            indicator.SetActive(visible);
        }
    }
    void UpdateIndicatorStatusByMenuOpen()
    {
        //일단은 빈칸
    }
    void UpdateIndicatorStatusByMenuClose()
    {
        UpdateIndicatorStatus(InputManager.CursorScreenPosition);
    }

    void UpdateIndicatorColor(bool value)
    {
        Color color = value ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
        runtimeIndicatorMat.SetColor(tintColorPorpertyID, color);
    }

    void ChangeCurrentSelectedUnit(GameObject selectedUnit, TeamID team)
    {
        if (!selectedUnit) return;
        CharacterBase character = selectedUnit.GetComponent<CharacterBase>();
        UnitStatus status =  null;
        if (character)
        {
             status = character.Status;
        }
        if (!status) return;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
        SetMode(UnitPlacementMode.Place);
        selected = true;
        size = status.colliderRadius;
        selectedTeam = team;

        if (decal)
        {
            decal.size = new Vector3(size * 2f, size * 2f, decal.size.z);
        }
        if (indicatorCollider)
        {
            indicatorCollider.size = new Vector3(size * 2f, size * 2f, indicatorCollider.size.z);
        }
    }

    //다시 로딩하지 않는 한 켜지지 않도록 꺼버리기(혹시나 몰라서, 나중에 필요하면 수정할꺼)
    //켜는건 UI_BattlefieldScreen에서 켜지게 만들어놨음
    void DisableIndicator()
    {
        gameObject.SetActive(false);
    }
}
