using UnityEngine;

public class UI_Button_OpenScreen : MonoBehaviour
{
    [SerializeField] UIType wantType;
    [SerializeField] ScreenChangeType changeType;
    [SerializeField] TextAsset sandboxData;
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
    public void OpenSandbox()
    {
        GameManager.SceneLoad.LoadSandbox("StageScene", sandboxData);
    }
}
