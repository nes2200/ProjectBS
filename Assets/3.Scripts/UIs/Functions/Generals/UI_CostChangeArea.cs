using TMPro;
using UnityEngine;

public class UI_CostChangeArea : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI warningText;

    private UI_CostChangeWindow window;
    private int index;

    public void Initialize(UI_CostChangeWindow owner, int areaIndex)
    {
        window = owner;
        index = areaIndex;
        SetWarning("");
    }

    public void ChangeCurrentCostText(int costLimit)
    {
        costText.text = costLimit.ToString();
    }

    public void OnEndEdit(string input)
    {
        if (!window) return;
        OnValueChanged(input);
        window.TryChangeCost(index, input);
    }

    public void OnValueChanged(string input)
    {
        if (!window || !int.TryParse(input, out int value))
        {
            SetWarning("");
            return;
        }
        SetWarning(window.GetCostWarning(index, value));
    }

    private void SetWarning(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}
