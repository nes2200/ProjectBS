using UnityEngine;

public class UI_BattlefieldScreen : UI_ScreenBase
{
    public static event System.Action OnMenuOpen;
    public static event System.Action OnMenuClose;
    
    protected StageManager stageManager;

    [Header("Star")]
    [SerializeField] protected UI_CostStarController starController;
    [SerializeField] protected GameObject starPrefab;

    [Header("HPBar Group")]
    [SerializeField] protected UI_HPBarGroup hpBarGroup;

    [Header("Components")]
    [SerializeField] protected GameObject startButton;
    [SerializeField] protected CanvasGroup SelectArea;
    [SerializeField] protected UI_Button_UnitRemove unitRemoveButton;

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

        SetSelectAreaVisible(true);

        InputManager.OnCancel -= ToggleMenu;
        InputManager.OnCancel += ToggleMenu;

        startButton.SetActive(true);

        if (stageManager) ConnectStage(stageManager);
    }
    public override void Close()
    {
        base.Close();
        GameManager.Camera.RemoveCameraController();
        GameManager.ResetBattle();

        InputManager.OnCancel -= ToggleMenu;

        hpBarGroup.Disconnect();
        hpBarGroup.ClearAllHPBars();
        unitRemoveButton.Disconnect();
    }

    public void ToggleMenu(bool value)
    {
        if (!value) return;

        var movable = UIManager.ClaimGetUI(UIType.Movable) as UI_MovableScreen;
        if (movable != null && movable.TryClosePopup()) return;
        if (UIManager.ClaimCloseUI(
            UIType.GameQuit, UIType.SaveMapWindow, UIType.CostChangeWindow, UIType.Inventory, UIType.BattleResult))
        {
            return;
        }

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

    public virtual void ConnectStage(StageManager newStageManager)
    {
        stageManager = newStageManager;

        //화면 전환이 끝나기 전이라면 Open에서 다시 연결됨
        if (!isActiveAndEnabled) return;

        hpBarGroup.Connect(stageManager.CharacterRegistry);
        starController.SetAllCostLimitText(stageManager.GetCostLimits());
        starController.RefreshState(stageManager.GetCurrentCost());
        unitRemoveButton.Connect(newStageManager.Indicator);
        stageManager.Indicator.gameObject.SetActive(true);

        startButton.SetActive(true);
    }

    public void HidePreparationUI()
    {
        startButton.SetActive(false);
        SetSelectAreaVisible(false);
    }

    protected void SetSelectAreaVisible(bool visible)
    {
        SelectArea.alpha = visible ? 1f : 0f;
        SelectArea.interactable = visible;
        SelectArea.blocksRaycasts = visible;
    }
}
