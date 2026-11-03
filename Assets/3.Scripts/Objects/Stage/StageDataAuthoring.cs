using System.Collections.Generic;
using UnityEngine;

public class StageDataAuthoring : MonoBehaviour
{
    [SerializeField] List<UnitDefinition> selectableUnitsEntry = new();
    [SerializeField] List<UnitDefinition> selectableEnemiesEntry = new();
    [SerializeField] private int[] costLimits;

    public IReadOnlyList<UnitDefinition> SelectableUnitsEntry => selectableUnitsEntry;
    public IReadOnlyList<UnitDefinition> SelectableEnemiesEntry => selectableEnemiesEntry;


    //청소 및 리셋용
    public void ClearData()
    {
        selectableUnitsEntry.Clear();
        selectableEnemiesEntry.Clear();
        costLimits = null;
    }

    //로드할 때 내용 채워넣기
    public void SetSelectableUnits(IEnumerable<UnitDefinition> unitPrefabs)
    {
        selectableUnitsEntry.Clear();

        if (unitPrefabs == null) return;

        foreach (UnitDefinition unitPrefab in unitPrefabs)
        {
            if (!unitPrefab) continue;

            if (selectableUnitsEntry.Contains(unitPrefab)) continue;

            selectableUnitsEntry.Add(unitPrefab);   
        }
    }
    public void SetSelectableEnemies(IEnumerable<UnitDefinition> enemyPrefabs)
    {
        selectableEnemiesEntry.Clear();

        if (enemyPrefabs == null) return;

        foreach (UnitDefinition enemyPrefab in enemyPrefabs)
        {
            if (!enemyPrefab) continue;
            if (selectableEnemiesEntry.Contains(enemyPrefab)) continue;

            selectableEnemiesEntry.Add(enemyPrefab);
        }
    }

    public void SetSelectableUnit(UnitDefinition prefab, bool selected)
    {
        if (!prefab) return;
        if (selected)
        {
            if (!selectableUnitsEntry.Contains(prefab)) selectableUnitsEntry.Add(prefab);
        }
        else
        {
            selectableUnitsEntry.Remove(prefab);
        }
    }
    public bool IsSelectableUnit(UnitDefinition prefab)
    {
        return selectableUnitsEntry.Contains(prefab);
    }

    public int[] GetCostLimits() => (int[])costLimits?.Clone();
    public void SetCostLimits(int[] limits)
    {
        costLimits = (int[])limits?.Clone();
    }
}
