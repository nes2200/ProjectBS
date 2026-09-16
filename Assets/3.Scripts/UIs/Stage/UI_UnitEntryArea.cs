using System.Collections.Generic;
using UnityEngine;

public class UI_UnitEntryArea : MonoBehaviour
{
    [SerializeField] UI_Toggle_UnitEntry togglePrefab;
    [SerializeField] Transform toggleParent;

    readonly List<UI_Toggle_UnitEntry> createdToggles = new();

    public void Rebuild(StageManager manager)
    {
        ClearToggles();

        if (!manager || !manager.IsSandbox) return;

        HashSet<GameObject> added = new();

        foreach(GameObject prefab in GameManager.StageLoad.SelectableUnits)
        {
            if (!prefab || !added.Add(prefab)) continue;

            UI_Toggle_UnitEntry toggle = Instantiate(togglePrefab, toggleParent);
            toggle.Initialize(prefab, manager);
            createdToggles.Add(toggle);
        }
    }

    private void ClearToggles()
    {
        foreach (UI_Toggle_UnitEntry toggle in createdToggles)
        {
            if (toggle)
            {
                toggle.gameObject.SetActive(false);
                Destroy(toggle.gameObject);
            }
        }
        createdToggles.Clear();
    }
}
