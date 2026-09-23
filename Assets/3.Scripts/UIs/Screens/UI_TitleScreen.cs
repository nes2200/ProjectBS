using UnityEngine;

public class UI_TitleScreen : UI_ScreenBase
{
    [Header("LogIn/Out Buttons")]
    [SerializeField] GameObject loginButton;
    [SerializeField] GameObject logoutButton;


    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        InputManager.OnCancel -= ToggleCloseConfirm;
        InputManager.OnCancel += ToggleCloseConfirm;

        GameManager.DB.OnAuthStateChanged -= UpdateAuthButtons;
        GameManager.DB.OnAuthStateChanged += UpdateAuthButtons;
        UpdateAuthButtons(GameManager.DB.IsLoggedIn);

    }
    public override void Unregistration(UIManager manager)
    {
        InputManager.OnCancel -= ToggleCloseConfirm;
        GameManager.DB.OnAuthStateChanged -= UpdateAuthButtons;
        base.Unregistration(manager);
    }

    public override void Open()
    {
        base.Open();
        InputManager.OnCancel -= ToggleCloseConfirm;
        InputManager.OnCancel += ToggleCloseConfirm;
        UpdateAuthButtons(GameManager.DB.IsLoggedIn);

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

    private void UpdateAuthButtons(bool isLoggedIn)
    {
        loginButton.SetActive(!isLoggedIn);
        logoutButton.SetActive(isLoggedIn);
    }
}
