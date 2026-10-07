using UnityEngine;

public class UI_CostChangeWindow : OpenableUIBase
{
    [Header("Cost Change Area")]
    [SerializeField] UI_CostChangeArea[] costChangeArea;

    UI_SandboxScreen sandboxScreen;
    int[] wantCostLimits;


    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        sandboxScreen = UIManager.ClaimGetUI(UIType.Sandbox) as UI_SandboxScreen;
    }
    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
    }

    public override void Open()
    {
        base.Open();
        //SetCurrentCostTexts();
    }
    public override void Close()
    {
        base.Close();
    }

    public void SetCurrentCostTexts(int[] costLimits)
    {
        this.wantCostLimits = (int[])costLimits.Clone();

        for (int i = 0; i < costLimits.Length; i++)
        {
            costChangeArea[i].Initialize(this, i);
            costChangeArea[i].ChangeCurrentCostText(costLimits[i]);
        }
    }

    public string GetCostWarning(int index, int value)
    {
        if (value > 1000)return "1000이상 입력 불가";
        if (value < 0) return "0 이하 입력 불가";
        if (index < wantCostLimits.Length - 1 && value >= wantCostLimits[index + 1]) return "다음 코스트보다 낮아야함";
        if (index > 0 && value <= wantCostLimits[index - 1]) return "이전 코스트보다 높아야함";
        return "";
    }

    public bool TryChangeCost(int index, string input)
    {
        if (wantCostLimits == null || index < 0 || index >= wantCostLimits.Length) return false;
        if (!int.TryParse(input, out int value)) return false;
        if (!string.IsNullOrEmpty(GetCostWarning(index, value))) return false;

        wantCostLimits[index] = value;
        costChangeArea[index].ChangeCurrentCostText(value);
        return true;
    }

    public void SaveCostLimits()
    {
        if (!sandboxScreen) return;
        if (!sandboxScreen.TrySaveCostLimits(wantCostLimits)) return;

        Close();
    }
}
