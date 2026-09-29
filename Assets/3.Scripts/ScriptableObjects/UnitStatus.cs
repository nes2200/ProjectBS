using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // 에디터 기능을 쓰기 위해 필요한 도구함
#endif

[CreateAssetMenu(fileName = "UnitStatus", menuName = "Scriptable Objects/UnitStatus")]
public class UnitStatus : ScriptableObject
{
    public string unitName;
    public int cost;
    public int maxHP;
    public int damage;
    public float attackSpeed;
    public float moveSpeed;

    [Header("자동으로 계산될 값")]
    public float colliderRadius;

    // --- 여기 아래는 버튼을 만들기 위한 특수한 코드입니다 ---
#if UNITY_EDITOR
    public void UpdateRadiusFromPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("프리팹이 비어있습니다!");
            return;
        }

        // 1. "Character"라는 이름의 레이어가 몇 번인지 숫자를 알아냅니다.
        int characterLayer = LayerMask.NameToLayer("Character");

        if (characterLayer == -1) // 레이어 이름이 없으면 -1을 반환합니다.
        {
            Debug.LogError("'Character' 레이어가 유니티 설정에 존재하지 않습니다! 레이어 이름을 확인해 주세요.");
            return;
        }

        // 2. 모든 캡슐 콜라이더를 가져옵니다.
        CapsuleCollider[] allCapsules = prefab.GetComponentsInChildren<CapsuleCollider>();
        CapsuleCollider targetCollider = null;

        // 3. 레이어가 "Character"인 놈만 필터링합니다.
        foreach (var capsule in allCapsules)
        {
            if (capsule.gameObject.layer == characterLayer)
            {
                targetCollider = capsule;
                break;
            }
        }

        // 4. 저장 로직
        if (targetCollider != null)
        {
            // 월드 기준의 실제 크기를 가져오기 위해 lossyScale을 사용합니다.
            float scale = Mathf.Max(targetCollider.transform.lossyScale.x, targetCollider.transform.lossyScale.z);
            colliderRadius = targetCollider.radius * scale;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[성공] 레이어 'Character'에서 반지름({colliderRadius}) 추출 완료!");
        }
        else
        {
            Debug.LogError($"{prefab.name} 자식 중에 'Character' 레이어가 설정된 콜라이더가 없습니다.");
        }
    }
#endif
}

// --- 인스펙터에 버튼을 만들어주는 코드 ---
#if UNITY_EDITOR
[CustomEditor(typeof(UnitStatus))]
public class UnitStatusEditor : Editor
{
    private GameObject targetPrefab;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // 기존 변수들(이름, 체력 등)을 화면에 보여줍니다.

        UnitStatus status = (UnitStatus)target;

        GUILayout.Space(20); // 한 줄 띄우기
        EditorGUILayout.LabelField("반지름 추출기", EditorStyles.boldLabel);

        // 프리팹을 끌어다 놓을 칸을 만듭니다.
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("대상 유닛 프리팹", targetPrefab, typeof(GameObject), false);

        GUI.backgroundColor = Color.cyan; // 버튼 색깔을 하늘색으로!
        if (GUILayout.Button("프리팹에서 반지름 가져오기"))
        {
            status.UpdateRadiusFromPrefab(targetPrefab);
        }
    }
}
#endif