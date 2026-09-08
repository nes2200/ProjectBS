using UnityEngine;

public class UI_Button_OpenScreen : MonoBehaviour
{
    [SerializeField] UIType wantType;
    [SerializeField] ScreenChangeType changeType;
    public void OpenScreen()
    {
        UIManager.ClaimOpenScreen(wantType, changeType);
    }
    public void OpenScreenCloseMenu()
    {
        OpenScreen();
        UIManager.ClaimCloseUI(UIType.Menu);
        if(wantType != UIType.Stage)
        {
            GameManager.UnPause();
        }
    }
    public void OpenSaveScreen()
    {
        UI_SaveLoadScreen saveScreen = UIManager.ClaimGetUI(UIType.SaveSlot) as UI_SaveLoadScreen;
        saveScreen.saveSlot.IsSave = true;
        OpenScreen();
    }
    public void OpenLoadScreen()
    {
        UI_SaveLoadScreen saveScreen = UIManager.ClaimGetUI(UIType.SaveSlot) as UI_SaveLoadScreen;
        saveScreen.saveSlot.IsSave = false;
        OpenScreen();
    }
    public void OpenSandbox()
    {
        GameManager.SceneLoad.LoadSandbox("StageScene");
    }
}
