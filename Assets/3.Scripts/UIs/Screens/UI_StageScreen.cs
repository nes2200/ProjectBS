using UnityEngine;

public class UI_StageScreen : UI_BattlefieldScreen
{
    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        
    }

    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
    }

    public override void Open()
    {
        base.Open();
        
        StageManager.OnBattleStart -= HidePreparationUI;
        StageManager.OnBattleStart += HidePreparationUI;

        StageManager.OnBattleEnd -= OpenBattleResult;
        StageManager.OnBattleEnd += OpenBattleResult;

        if (stageManager) ConnectStage(stageManager);

    }
    public override void Close()
    {
        base.Close();

        StageManager.OnBattleStart -= HidePreparationUI;
        StageManager.OnBattleEnd -= OpenBattleResult;
    }

    private void OpenBattleResult(bool isPlayerLoose)
    {
        UIBase instance = UIManager.ClaimOpenUI(UIType.BattleResult);
        UI_BattleResultWindow resultWindow = instance as UI_BattleResultWindow;

        resultWindow.SetResult(isPlayerLoose, starController.GetStageResult());
    }
}
