using System.Collections.Generic;
using UnityEngine;

public class StageDataAuthoring : MonoBehaviour
{
    [SerializeField] List<GameObject> selectableUnitsEntry = new();
    [SerializeField] List<GameObject> selectableEnemiesEntry = new();
    [SerializeField] private int[] costLimits;

    public IReadOnlyList<GameObject> SelectableUnitsEntry => selectableUnitsEntry;
    public IReadOnlyList<GameObject> SelectableEnemiesEntry => selectableEnemiesEntry;


    //청소 및 리셋용
    public void ClearData()
    {
        selectableUnitsEntry.Clear();
        selectableEnemiesEntry.Clear();
        costLimits = null;
    }

    //로드할 때 내용 채워넣기
    public void SetSelectableUnits(IEnumerable<GameObject> unitPrefabs)
    {
        selectableUnitsEntry.Clear();

        if (unitPrefabs == null) return;

        foreach (GameObject unitPrefab in unitPrefabs)
        {
            if (!unitPrefab) continue;

            if (selectableUnitsEntry.Contains(unitPrefab)) continue;

            selectableUnitsEntry.Add(unitPrefab);   
        }
    }
    public void SetSelectableEnemies(IEnumerable<GameObject> enemyPrefabs)
    {
        selectableEnemiesEntry.Clear();

        if (enemyPrefabs == null) return;

        foreach (GameObject enemyPrefab in enemyPrefabs)
        {
            if (!enemyPrefab) continue;
            if (selectableEnemiesEntry.Contains(enemyPrefab)) continue;

            selectableEnemiesEntry.Add(enemyPrefab);
        }
    }

    public void RegisterSelectableUnit(GameObject prefab)
    {
        if (!prefab || selectableUnitsEntry.Contains(prefab)) return;

        selectableUnitsEntry.Add(prefab);
    }

    public int[] GetCostLimits() => (int[])costLimits?.Clone();
    public void SetCostLimits(int[] limits)
    {
        costLimits = (int[])limits?.Clone();
    }
}
