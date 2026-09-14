using UnityEngine;

public class UI_SandboxScreen : UI_BattlefieldScreen
{
    UI_CostChangeWindow costChangeWindow;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        costChangeWindow = UIManager.ClaimGetUI(UIType.CostChangeWindow) as UI_CostChangeWindow;
    }
    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
    }

    public override void Open()
    {
        base.Open();
        SetCurrentCostTexts();
    }

    public void SetCurrentCostTexts()
    {
        if (costChangeWindow) costChangeWindow.SetCurrentCostTexts(stageManager.GetCostLimits());
    }

    public void RegisterSelectableUnit(GameObject prefab)
    {
        if (!stageManager) return;

        stageManager.RegisterSelectableUnit(prefab);
    }

    public bool TrySaveCostLimits(int[] limits)
    {
        if (!stageManager || !stageManager.TrySetCostLimits(limits)) return false;

        starController.SetAllCostLimitText(stageManager.GetCostLimits());
        starController.RefreshState(stageManager.GetCurrentCost());
        return true;
    }
}
