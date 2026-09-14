using TMPro;
using UnityEngine;

public class UI_CostChangeArea : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI costText;

    private UI_CostChangeWindow window;
    private int index;

    public void Initialize(UI_CostChangeWindow owner, int areaIndex)
    {
        window = owner;
        index = areaIndex;
    }

    public void ChangeCurrentCostText(int costLimit)
    {
        costText.text = costLimit.ToString();
    }

    public void OnEndEdit(string input)
    {
        if (!window) return;
        window.TryChangeCost(index, input);
    }
}
