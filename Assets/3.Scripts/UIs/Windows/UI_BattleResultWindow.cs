using TMPro;
using UnityEngine;

public class UI_BattleResultWindow : OpenableUIBase
{
    [Header("UIs")]
    [SerializeField] Animator anim;
    [SerializeField] TextMeshProUGUI resultText;

    [Header("Stars")]
    [SerializeField] UI_CostStarResult[] stars;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
    }

    private void OnEnable()
    {
        anim.SetTrigger("Show");
    }

    public void SetResult(bool isPlayerLoose, bool[] costLimitOverResult)
    {
        if (isPlayerLoose)
        {
            resultText.text = "ÆÐ¹è";
            CostLimitOverCheck(new bool[] { true, true, true});
        }
        else
        {
            resultText.text = "½Â¸®";
            CostLimitOverCheck(costLimitOverResult);
        }
    }

    public void CostLimitOverCheck(bool[] costLimitOverResult)
    {
        for(int i = 0; i < stars.Length; i++)
        {
            stars[i].CostLimitOverCheck(costLimitOverResult[i]);
        }
    }

    public void Confirm()
    {
        UIType target = GameManager.SceneLoad.IsCustomStage ? UIType.Load : UIType.StageSelect;
        UIManager.ClaimOpenScreen(target, ScreenChangeType.SlideChanger);
    }
}
