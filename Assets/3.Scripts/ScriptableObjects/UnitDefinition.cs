using UnityEngine;

[CreateAssetMenu(fileName = "UnitDefinition", menuName = "Scriptable Objects/UnitDefinition")]
public class UnitDefinition : ScriptableObject
{
    [SerializeField] UnitStatus status;
    [SerializeField] GameObject[] spawnPrefabs;


    public UnitStatus Status => status;
    public GameObject GetRandomPrefab()
    {
        if (spawnPrefabs == null || spawnPrefabs.Length == 0)
        {
            Debug.LogError($"[{name}] 소환할 프리팹이 없습니다.", this);
            return null;
        }

        int index = Random.Range(0, spawnPrefabs.Length);
        return spawnPrefabs[index];
    }
}
