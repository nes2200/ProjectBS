using System.Collections.Generic;
using UnityEngine;

public enum SelectAreaType { Unit, Enemy }

public class UI_UnitSelectArea : MonoBehaviour
{
    [Header("Unit Button")]
    [SerializeField] UI_Button_UnitSelect buttonPrefab;
    [SerializeField] Transform buttonParent;

    [Header("Area Type")]
    [SerializeField] SelectAreaType areaType;

    readonly List<UI_Button_UnitSelect> createdButtons = new();

    private void OnEnable()
    {
        if (GameManager.StageLoad == null) return;
        GameManager.StageLoad.OnSelectableCharactersLoaded -= RebuildButtons;
        GameManager.StageLoad.OnSelectableCharactersLoaded += RebuildButtons;
        RebuildButtons();
    }

    private void OnDisable()
    {
        if (GameManager.StageLoad == null) return;
        GameManager.StageLoad.OnSelectableCharactersLoaded -= RebuildButtons;
    }

    public void RebuildButtons()
    {
        ClearButtons();

        IReadOnlyList<GameObject> prefabs = areaType == SelectAreaType.Unit ?
            GameManager.StageLoad.SelectableUnits : GameManager.StageLoad.SelectableEnemies;
        TeamID team = areaType == SelectAreaType.Unit ? TeamID.TeamA : TeamID.TeamB;

        foreach(GameObject prefab in prefabs)
        {
            if (!prefab) continue;

            UI_Button_UnitSelect button = Instantiate(buttonPrefab, buttonParent);
            button.Initialize(prefab, team);
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

    IReadOnlyList<GameObject> GetCurrentPrefabs()
    {
        return areaType switch
        {
            SelectAreaType.Unit => GameManager.StageLoad.SelectableUnits,
            SelectAreaType.Enemy => GameManager.StageLoad.SelectableEnemies,
            _ => null
        };
    }
}
