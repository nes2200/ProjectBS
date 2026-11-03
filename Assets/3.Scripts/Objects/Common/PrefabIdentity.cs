using UnityEngine;

public class PrefabIdentity : MonoBehaviour
{
    [SerializeField] string prefabKey;
    public string PrefabKey => prefabKey;

    [SerializeField] UnitDefinition unitDefinition;
    public UnitDefinition UnitDefinition => unitDefinition;
}
