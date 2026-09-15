using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Toggle_UnitEntry : MonoBehaviour
{
    [Header("오브젝트 구성 요소")]
    [SerializeField] Toggle toggle;
    [SerializeField] TextMeshProUGUI unitNameText;
    [SerializeField] TextMeshProUGUI unitCostText;

    GameObject unitPrefab;
    StageManager stageManager;

    public void Initialize(GameObject prefab, StageManager newManager)
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);

        unitPrefab = prefab;
        stageManager = newManager;

        if(!prefab || !stageManager || !prefab.TryGetComponent<CharacterBase>(out CharacterBase character) || !character.Status)
        {
            toggle.interactable = false;
            return;
        }

        unitNameText.text = character.Status.unitName;
        unitCostText.text = character.Status.cost.ToString();

        toggle.interactable = true;
        toggle.SetIsOnWithoutNotify(stageManager.IsSelectableUnit(prefab));
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    void OnValueChanged(bool selected)
    {
        if (!stageManager || !unitPrefab) return;

        stageManager.SetSelectableUnit(unitPrefab, selected);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
    }
}
