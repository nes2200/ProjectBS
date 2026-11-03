using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneScanner
{
    //오브젝트 수집
    public static SceneSaveData Capture(Scene targetScene, StageDataAuthoring authoring, Func<GameObject, string> getPrefabName)
    {
        if(!authoring) throw new ArgumentNullException(nameof(authoring));
        if(getPrefabName == null) throw new ArgumentNullException(nameof(getPrefabName));

        SceneSaveData saveData = new();

        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach(GameObject obj in allObjects)
        {
            if (obj.scene != targetScene) continue;

            Transform parent = obj.transform.parent;
            string parentName = parent ? parent.name : "None";

            bool isSpecialObject = obj.name == "Terrain" || obj.name == "TeamLine";
            bool isContainerChild = parentName == "Probs" || parentName == "TeamB";

            if (!isSpecialObject && !isContainerChild) continue;

            string prefabName = GetPrefabName(obj);
            string definitionName = GetDefinition(obj);

            if(string.IsNullOrEmpty(prefabName)) throw new InvalidOperationException($"프리팹 이름을 찾을 수 없음: {obj.name}");

            saveData.objects.Add(new StageObject
            {
                name = obj.name,
                prefabName = prefabName,
                parentName = parentName,
                unitDefinitionName = definitionName,
                position = obj.transform.localPosition,
                scale = obj.transform.localScale,
                rotation = obj.transform.localRotation
            });
        }

        CaptureUnits(authoring.SelectableUnitsEntry, saveData.selectableUnits);
        CaptureUnits(authoring.SelectableEnemiesEntry, saveData.selectableEnemies);
        saveData.costLimits = authoring.GetCostLimits();
        return saveData;
    }

    //유닛 엔트리 수집
    private static void CaptureUnits(IReadOnlyList<UnitDefinition> units, List<StageUnitEntry> destination)
    {
        HashSet<string> savedNames = new();

        foreach (UnitDefinition unit in units)
        {
            if (!unit) continue;

            string unitKey = unit.name;
            if (!savedNames.Add(unitKey)) continue;

            destination.Add(new StageUnitEntry
            {
                unitPrefabName = unitKey
            });
        }
    }

    private static string GetPrefabName(GameObject obj)
    {
        return obj.TryGetComponent<PrefabIdentity>(out PrefabIdentity identity) ? identity.PrefabKey : null;
    }
    private static string GetDefinition(GameObject obj)
    {
        return obj.TryGetComponent<PrefabIdentity>(out PrefabIdentity identity) ? identity.UnitDefinition.name : null;
    }
}