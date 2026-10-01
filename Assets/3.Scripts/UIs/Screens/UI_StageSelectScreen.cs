using TMPro;
using UnityEngine;

public class UI_StageSelectScreen : UI_ScreenBase
{
    [SerializeField] TextMeshProUGUI chapterText;
    [SerializeField] UI_Button_StageSelect[] stageButtons;
    int chapter;

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

    public void SetChapter(int chapter)
    {
        this.chapter = chapter;
        chapterText.text = $"{chapter}ц╘ем";

        for(int i = 0; i < stageButtons.Length; i++)
        {
            stageButtons[i].SetStageText(chapter);
        }
    }
}
