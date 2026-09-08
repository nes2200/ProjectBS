using UnityEngine;

public class UI_SandboxSelectScreen : UI_ScreenBase
{
    public override void Open()
    {
        base.Open();
        InputManager.OnCancel -= BackToTitle;
        InputManager.OnCancel += BackToTitle;
    }
    public override void Close()
    {
        InputManager.OnCancel -= BackToTitle;
        base.Close();
    }

    void BackToTitle(bool value) => UIManager.ClaimOpenScreen(UIType.Title, ScreenChangeType.ScreenChanger);
}
