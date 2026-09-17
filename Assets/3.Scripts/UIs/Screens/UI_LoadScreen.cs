using TMPro;
using UnityEngine;

public class UI_LoadScreen : UI_ScreenBase
{
    [SerializeField] UI_FileViewer fileViewer;

    void BackToTitle(bool value) 
    {
        UIManager.ClaimOpenScreen(UIType.Title, ScreenChangeType.ScreenChanger);
    }

    public override void Open()
    {
        base.Open();

        InputManager.OnCancel -= BackToTitle;
        InputManager.OnCancel += BackToTitle;

        fileViewer.Clear();
        fileViewer.ReadLocalFiles();
    }
    public override void Close()
    {
        InputManager.OnCancel -= BackToTitle;
        base.Close();
    }
}
