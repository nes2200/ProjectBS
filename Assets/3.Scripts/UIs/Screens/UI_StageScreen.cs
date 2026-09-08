using UnityEngine;

public class UI_StageScreen : UI_ScreenBase
{
    public static event System.Action OnMenuOpen;
    public static event System.Action OnMenuClose;

    StageManager stageManager;

    [Header("Star")]
    [SerializeField] UI_CostStarController starController;
    [SerializeField] GameObject starPrefab;

    [Header("HPBar Group")]
    [SerializeField] UI_HPBarGroup hpBarGroup;

    [Header("Components")]
    [SerializeField] GameObject startButton;
    [SerializeField] CanvasGroup SelectArea;
    [SerializeField] UI_Button_UnitRemove unitRemoveButton;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        GameManager.StageLoad.OnStageLoaded -= ConnectStage;
        GameManager.StageLoad.OnStageLoaded += ConnectStage;
    }

    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
        GameManager.StageLoad.OnStageLoaded -= ConnectStage;

        hpBarGroup.Disconnect();
        hpBarGroup.ClearAllHPBars();
        unitRemoveButton.Disconnect();
    }

    public override void Open()
    {
        base.Open();

        GameManager.Camera.AddCameraController();
        GameManager.ResetBattle();

        InputManager.OnCancel -= ToggleMenu;
        InputManager.OnCancel += ToggleMenu;

        StageManager.OnBattleStart -= HidePreparationUI;
        StageManager.OnBattleStart += HidePreparationUI;

        StageManager.OnBattleEnd -= OpenBattleResult;
        StageManager.OnBattleEnd += OpenBattleResult;

        startButton.SetActive(true);

        if (stageManager) ConnectStage(stageManager);

    }
    public override void Close()
    {
        base.Close();
        GameManager.Camera.RemoveCameraController();
        GameManager.ResetBattle();

        InputManager.OnCancel -= ToggleMenu;
        StageManager.OnBattleStart -= HidePreparationUI;
        StageManager.OnBattleEnd -= OpenBattleResult;

        hpBarGroup.Disconnect();
        hpBarGroup.ClearAllHPBars();
        unitRemoveButton.Disconnect();
    }

    public void ToggleMenu(bool value)
    {
        UIManager.ClaimToggleUI(UIType.Menu);

        bool isMenuOpen = UIManager.ClaimCheckOpen(UIType.Menu, out _);
        if (isMenuOpen)
        {
            GameManager.Pause();
            OnMenuOpen?.Invoke();
        }
        else
        {
            GameManager.UnPause();
            OnMenuClose?.Invoke();
        }
    }

    public static void ClaimOnMenuClose()
    {
        OnMenuClose?.Invoke();
    }

    private void SetCostLimitText(int[] costLimits)
    {
        starController.SetAllCostLimitText(costLimits);
    }

    private void OpenBattleResult(bool isPlayerLoose)
    {
        UIBase instance = UIManager.ClaimOpenUI(UIType.BattleResult);
        UI_BattleResultWindow resultWindow = instance as UI_BattleResultWindow;

        resultWindow.SetResult(isPlayerLoose, starController.GetStageResult());
    }

    private void ConnectStage(StageManager newStageManager)
    {
        stageManager = newStageManager;

        //화면 전환이 끝나기 전이라면 Open에서 다시 연결됨
        if (!isActiveAndEnabled) return;

        hpBarGroup.Connect(stageManager.CharacterRegistry);
        SetCostLimitText(stageManager.GetCostLimits());
        starController.RefreshState(stageManager.GetCurrentCost());
        unitRemoveButton.Connect(newStageManager.Indicator);

        startButton.SetActive(true);
        SetSelectAreaVisible(true);
    }

    public void HidePreparationUI()
    {
        startButton.SetActive(false);
        SetSelectAreaVisible(false);
    }

    private void SetSelectAreaVisible(bool visible)
    {
        SelectArea.alpha = visible ? 1f : 0f;
        SelectArea.interactable = visible;
        SelectArea.blocksRaycasts = visible;
    }
}
