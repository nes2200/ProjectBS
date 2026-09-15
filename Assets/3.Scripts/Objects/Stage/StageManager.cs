using UnityEngine;
using System.Collections.Generic;

//스테이지의 현재 상태
public enum StageState
{
    Ready,
    Battle,
    Result
}

public enum BattleFieldMode
{
    Stage, Sandbox
}

public delegate void StageStateChangeEvent(StageState oldState, StageState newState);
public delegate void BattleStartEvent();
public delegate void BattleEndEvent(bool isPlayer);

public class StageManager : MonoBehaviour
{
    public static event StageStateChangeEvent OnStageStateChange;
    public static event BattleStartEvent OnBattleStart;
    public static event BattleEndEvent OnBattleEnd;

    private StageState _currentState;
    public StageState CurrentState => _currentState;

    [Header("Stage Components")]
    [SerializeField] StageCharacterRegistry characterRegistry;
    [SerializeField] PlacementManager placementManager;
    [SerializeField] CostTracker costTracker;
    [SerializeField] StageDataAuthoring stageDataAuthoring;
    TeamLine teamLine;

    [Header("Initial Objects")]
    [SerializeField] Transform floor;
    [SerializeField] Transform probs;
    [SerializeField] Transform teamA;
    [SerializeField] Transform teamB;


    public Transform Floor => floor;
    public Transform Probs => probs;
    public Transform TeamA => teamA;
    public Transform TeamB => teamB;

    public StageCharacterRegistry CharacterRegistry => characterRegistry;
    public UnitPlaceIndicator Indicator => placementManager.Indicator;

    BattleFieldMode fieldMode = BattleFieldMode.Stage;
    public BattleFieldMode FieldMode => fieldMode;
    public bool IsSandbox => FieldMode == BattleFieldMode.Sandbox;

    //스테이지 상태 변경
    public void ChangeState(StageState newState)
    {
        if (CurrentState == newState) return;

        //바뀌었으니까 바뀐 상태로 바꿔주고 이벤트 발동
        StageState oldState = CurrentState;
        _currentState = newState;
        OnStageStateChange?.Invoke(oldState, newState);
    }

    public void SetFieldMode(BattleFieldMode newMode)
    {
        fieldMode = newMode;
    }

    public void StartBattle()
    {
        GameManager.StartBattle();
        OnBattleStart?.Invoke();
        ChangeState(StageState.Battle);
    }
    public void EndBattle(bool isPlayerLoose)
    {
        GameManager.EndBattle();
        OnBattleEnd?.Invoke(isPlayerLoose);
        ChangeState(StageState.Result);
    }   

    //ReadyBattle은 따로 안만듬?
    //Ready -> Battle -> Result는 일방향임. 
    //되돌아 간다는 것은 다시하기 등을 통해 씬을 새로 로드헀거나, 완전히 새로고침 했다는 것
    //그렇기에 따로 Ready를 만들지 않고, 나중에 씬 분리 과정에서 만드는게 더 좋을 것 같음

    //현재 코스트 증감 함수
    public void CostIncreasByUnitSpawn(int unitCost)
    {
        costTracker.IncreaseCost(unitCost);
    }
    public void CostDecreaseByUnitDespawn(int unitCost)
    {
        costTracker.DecreaseCost(unitCost);
    }

    //샌드박스 모드에서 유닛 등록하는 용도
    public void SetSelectableUnit(GameObject prefab, bool select)
    {
        if (!IsSandbox || !stageDataAuthoring) return;

        stageDataAuthoring.SetSelectableUnit(prefab, select);
    }
    public bool IsSelectableUnit(GameObject prefab)
    {
        return stageDataAuthoring && stageDataAuthoring.IsSelectableUnit(prefab);
    }
   
    //저장된 스테이지를 열었을 때 등록 목록을 복구하는 기능
    public void InitializeSelectableUnits(IEnumerable<GameObject> prefabs)
    {
        if (!IsSandbox || !stageDataAuthoring) return;

        stageDataAuthoring.SetSelectableUnits(prefabs);
    }

    public void InitializeCostLimits(int[] limits)
    {
        if (limits == null || limits.Length == 0) return;
        
        costTracker.Initialize(limits);

        if (IsSandbox) stageDataAuthoring.SetCostLimits(costTracker.GetCostLimits());
    }


    //텍스트 세팅시, UI가 각 코스트 한계 비용을 얻어오기 위한 함수
    public int[] GetCostLimits()
    {
        return costTracker.GetCostLimits();
    }

    public bool TrySetCostLimits(int[] limits)
    {
        if (!costTracker.TrySetCostLimits(limits)) return false;

        if(IsSandbox) stageDataAuthoring.SetCostLimits(costTracker.GetCostLimits());

        return true;
    }

    public int GetCurrentCost() => costTracker.GetCurrentCost();

    public bool IsCostEnoughToSpawn(int unitCost)
    {
        return costTracker.IsCostEnoughToSpawn(unitCost);
    }

    public void SetTeamLine(TeamLine newTeamLine)
    {
        teamLine = newTeamLine;
        placementManager.SetTeamLine(teamLine);
    }
}