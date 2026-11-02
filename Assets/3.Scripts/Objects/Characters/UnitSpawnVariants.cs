using UnityEngine;

public class UnitSpawnVariants : MonoBehaviour
{
    [SerializeField] GameObject malePrefab;
    [SerializeField] GameObject femalePrefab;

    public GameObject GetRandomPrefab()
    {
        if (!malePrefab || !femalePrefab)
        {
            Debug.LogError("[UnitSpawnVariants] 남녀 프리팹을 모두 연결해주세요.", this);
            return null;
        }

        return Random.Range(0, 2) == 0 ? malePrefab : femalePrefab;
    }
}
