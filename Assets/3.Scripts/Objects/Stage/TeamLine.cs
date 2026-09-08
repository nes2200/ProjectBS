using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TeamLine : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] bool teamAOnNegativeSide = true;
    [SerializeField, Min(0f)] float lineHalfWidth = 0f;

    [Header("Visual")]
    [SerializeField] DecalProjector decal;

    private void OnEnable()
    {
        StageManager.OnBattleStart -= HideVisual;
        StageManager.OnBattleStart += HideVisual;
    }

    private void OnDisable()
    {
        StageManager.OnBattleStart -= HideVisual;
    }

    public float GetSignedDistance(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - transform.position;
        // CrossLine의 로컬 X축을 좌우 판정 축으로 사용
        return Vector3.Dot(offset, transform.right);
    }

    public bool Contains(TeamID team, Vector3 worldPosition, float footPrintRadius = 0f)
    {
        if (team != TeamID.TeamA && team != TeamID.TeamB) return false;

        float distance = GetSignedDistance(worldPosition);
        float requiredDistance = lineHalfWidth + footPrintRadius;

        bool wantsNegativeSide = team == TeamID.TeamA ? teamAOnNegativeSide : !teamAOnNegativeSide;
        return wantsNegativeSide ? distance <= -requiredDistance : distance <= requiredDistance;
    }

    public TeamID GetTeamAt(Vector3 worldPosition)
    {
        float distance = GetSignedDistance(worldPosition);

        if (Mathf.Abs(distance) <= lineHalfWidth) return TeamID.None;
        bool isNegativeSide = distance < 0f;

        return isNegativeSide == teamAOnNegativeSide ? TeamID.TeamA : TeamID.TeamB;
    }

    public void HideVisual()
    {
        if (decal) decal.enabled = false;
    }
}
