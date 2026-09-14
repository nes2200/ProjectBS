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

    public bool TryChangeCost(int index, string input)
    {
        if (wantCostLimits == null || index < 0 || index >= wantCostLimits.Length) return false;

        if (!int.TryParse(input, out int value)) return false;

        int[] copy = (int[])wantCostLimits.Clone();
        copy[index] = value;

        //코스트 제한에서 20 30 40은 가능. 30 30 40같은건 불가능
        for(int i = 1; i < copy.Length; i++)
        {
            if (copy[i - 1] >= copy[i]) return false;
        }

        wantCostLimits = copy;
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
