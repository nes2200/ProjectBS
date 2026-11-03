using UnityEngine;

//저장할 데이터에 대한 정보
[System.Serializable]
public class StageObject
{
    public string name;
    public string prefabName;
    public string unitDefinitionName;
    public string parentName;
    public SerializableVector3 position;
    public SerializableVector3 scale;
    public SerializableQuaternion rotation;
}

[System.Serializable]
public class StageUnitEntry
{
    public string unitPrefabName;
}
