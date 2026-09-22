using UnityEngine;

public class UI_TitleScreen : UI_ScreenBase
{
    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        InputManager.OnCancel -= ToggleCloseConfirm;
        InputManager.OnCancel += ToggleCloseConfirm;
    }
    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
        InputManager.OnCancel -= ToggleCloseConfirm;
    }

    public override void Open()
    {
        base.Open();
        InputManager.OnCancel -= ToggleCloseConfirm;
        InputManager.OnCancel += ToggleCloseConfirm;
    }
    public override void Close()
    {
        InputManager.OnCancel -= ToggleCloseConfirm;
        base.Close();
    }

    void ToggleCloseConfirm(bool value) 
    {
        if (UIManager.ClaimCheckOpen(UIType.RegisterWindow, out IOpenable register))
        {
            register.Close();
            return;
        }
        if (UIManager.ClaimCheckOpen(UIType.LogInWindow, out IOpenable login))
        {
            login.Close();
            return;
        }

        UIManager.ClaimToggleUI(UIType.GameQuit); 
    }
}
