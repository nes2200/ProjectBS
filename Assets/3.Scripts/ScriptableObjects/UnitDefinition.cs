using UnityEngine;

[CreateAssetMenu(fileName = "UnitDefinition", menuName = "Scriptable Objects/UnitDefinition")]
public class UnitDefinition : ScriptableObject
{
    [SerializeField] UnitStatus status;
    [SerializeField] GameObject malePrefab;
    [SerializeField] GameObject femalePrefab;

    public UnitStatus Status => status;
    public GameObject MalePrefab => malePrefab;
    public GameObject FemalePrefab => femalePrefab;
}
