using UnityEngine;

public class UI_SandboxScreen : UI_BattlefieldScreen
{
    UI_CostChangeWindow costChangeWindow;
    [SerializeField] UI_UnitEntryArea unitEntryArea;

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
    public override void ConnectStage(StageManager newStageManager)
    {
        base.ConnectStage(newStageManager);
        if (!isActiveAndEnabled || !stageManager) return;

        unitEntryArea.Rebuild(stageManager);
    }

    public void SetCurrentCostTexts()
    {
        if (costChangeWindow) costChangeWindow.SetCurrentCostTexts(stageManager.GetCostLimits());
    }


    public bool TrySaveCostLimits(int[] limits)
    {
        if (!stageManager || !stageManager.TrySetCostLimits(limits)) return false;

        starController.SetAllCostLimitText(stageManager.GetCostLimits());
        starController.RefreshState(stageManager.GetCurrentCost());
        return true;
    }

    public void OpenSaveMapWindow()
    {
        UI_SaveMapWindow window = UIManager.ClaimGetUI(UIType.SaveMapWindow) as UI_SaveMapWindow;
        window.OpenForSave(stageManager.GetAuthoring());
    }
}
