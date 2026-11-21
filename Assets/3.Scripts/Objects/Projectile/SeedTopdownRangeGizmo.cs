using UnityEngine;

[DisallowMultipleComponent]
public class SeedTopdownRangeGizmo : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] Blossom_Active_Topdown skill;
    [SerializeField] Color color = new Color(1f, 0f, 0f, 1f);

    bool hasImpactPosition;
    Vector3 impactPosition;

    public void SetImpactPosition(Vector3 position, Blossom_Active_Topdown source)
    {
        skill = source;
        impactPosition = position;
        hasImpactPosition = true;
    }

    private void OnDrawGizmos()
    {
        // Scene/Prefab 뷰에서만 표시하고 Game 뷰에는 표시하지 않는다.
        Camera camera = Camera.current;
        if (!enabled || !skill || !camera || camera.cameraType != CameraType.SceneView)
            return;

        Vector3 center = hasImpactPosition ? impactPosition : transform.position;
        Color previousColor = UnityEditor.Handles.color;
        UnityEditor.Handles.color = color;
        // 실제 판정처럼 월드 XZ 평면의 원이며 프리팹 스케일의 영향을 받지 않는다.
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, skill.DamageRadius);
        UnityEditor.Handles.color = previousColor;
    }
#endif
}
