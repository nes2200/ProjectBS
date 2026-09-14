using UnityEngine;

public delegate void CostChangeEvent(int currentCost);

public class CostTracker : MonoBehaviour
{
    //UI쪽에서 이걸 구독할거임
    //코스트 바뀌면 자동적으로 UI 바뀔 수 있게
    public static event CostChangeEvent OnCostChange;

    [Header("Stage Manager")]
    [SerializeField] StageManager stageManager;

    int[] costLimits;

    private FillValue costValue;

    private void OnDisable()
    {
        costValue.OnChanged -= InvokeCostChange;
    }

    public void Initialize(int[] limits)
    {
        if (limits is null || limits.Length == 0) return;
        costValue.OnChanged -= InvokeCostChange;

        costLimits = (int[])limits.Clone();
        costValue = new FillValue(0, costLimits[costLimits.Length - 1]);
        costValue.OnChanged += InvokeCostChange;
    }
    private void InvokeCostChange()
    {
        OnCostChange?.Invoke(costValue.Current);
    }

    public void IncreaseCost(int amount)
    {
        costValue.IncreaseCurrent(amount);
    }
    public void DecreaseCost(int amount)
    {
        costValue.DecreaseCurrent(amount);
    }
    
    public int[] GetCostLimits()
    {
        int[] result = new int[costLimits.Length];

        for(int i = 0; i < result.Length; i++)
        {
            result[i] = costLimits[i];
        }

        return result;
    }

    public bool TrySetCostLimits(int[] limits)
    {
        if (limits == null || costLimits == null || limits.Length != costLimits.Length || limits.Length == 0) return false;

        if (limits[0] < 0) return false;

        for (int i = 1; i < limits.Length; i++)
        {
            if (limits[i - 1] >= limits[i]) return false;
        }

        int max = limits[limits.Length - 1];
        if (max < costValue.Current) return false;

        costLimits = (int[])limits.Clone();
        costValue.SetMax(max);
        return true;
    }

    public int GetCurrentCost()
    {
        return costValue.Current;
    }

    //소환할 유닛 비용이 최고점을 넘는다면?
    public bool IsCostEnoughToSpawn(int unitCost)
    {
        if(costValue.Current + unitCost > costValue.Max)
        {
            return false;
        }
        return true;
    }
}
