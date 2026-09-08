using System.Collections.Generic;
using UnityEngine;

public class UI_UnitSelectArea : MonoBehaviour
{
    [Header("Unit Button")]
    [SerializeField] UI_Button_UnitSelect buttonPrefab;
    [SerializeField] Transform buttonParent;

    readonly List<UI_Button_UnitSelect> createdButtons = new();

    private void OnEnable()
    {
        if (GameManager.StageLoad == null) return;
        GameManager.StageLoad.OnSelectableUnitsLoaded -= RebuildButtons;
        GameManager.StageLoad.OnSelectableUnitsLoaded += RebuildButtons;
        RebuildButtons(GameManager.StageLoad.SelectableUnits);
    }

    private void OnDisable()
    {
        GameManager.StageLoad.OnSelectableUnitsLoaded -= RebuildButtons;
    }

    public void RebuildButtons(IReadOnlyList<GameObject> unitPrefabs)
    {
        ClearButtons();

        if (unitPrefabs == null) return;

        foreach(GameObject unitPrefab in unitPrefabs)
        {
            if (!unitPrefab) continue;

            UI_Button_UnitSelect button = Instantiate(buttonPrefab, buttonParent);
            button.Initialize(unitPrefab);
            createdButtons.Add(button);
        }
    }

    private void ClearButtons()
    {
        foreach (UI_Button_UnitSelect button in createdButtons)
        {
            if (button)
            {
                Destroy(button.gameObject);
            }
        }
        createdButtons.Clear();
    }
}
