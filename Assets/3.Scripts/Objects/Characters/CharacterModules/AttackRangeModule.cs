using UnityEngine;

public enum AttackRangeState
{
    InvalidTarget,
    OutOfRange,
    NeedRotation,
    Ready
}

public class AttackRangeModule : CharacterModule
{
    public sealed override System.Type RegistrationType => typeof(AttackRangeModule);

    [Header("몸통 바깥으로 닿는 공격 거리")]
    [SerializeField, Min(0f)] float attackRange = 0.2f;

    [Header("정면 기준 좌우 공격 허용 각도")]
    [SerializeField, Range(0f, 90f)] float attackHalfAngle = 30f;

    [Header("사거리 표시용 상대 반지름")]
    [SerializeField, Min(0f)] float previewTargetRadius = 0.4f;

    float minFacingDot;

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        UpdateFacingThreshold();
    }

    private void OnValidate()
    {
        UpdateFacingThreshold();
    }

    private void UpdateFacingThreshold()
    {
        minFacingDot = Mathf.Cos(attackHalfAngle * Mathf.Deg2Rad);
    }

    public AttackRangeState Evaluate(CharacterBase target)
    {
        if (!Owner || !Owner.IsAlive
            || !target || !target.IsAlive
            || !target.gameObject.activeInHierarchy
            || target == Owner
            || target.Team == Owner.Team)
        {
            return AttackRangeState.InvalidTarget;
        }

        Vector3 offset = target.transform.position - Owner.transform.position;
        offset.y = 0f;

        // 몸통 사이 거리 <= 공격 거리와 같은 판정
        float totalRange = attackRange + Owner.Status.colliderRadius + target.Status.colliderRadius;

        if (offset.sqrMagnitude > totalRange * totalRange)
        {
            return AttackRangeState.OutOfRange;
        }

        // 중심이 겹치면 방향을 정할 수 없으므로 공격 허용
        if (offset.sqrMagnitude <= 0.000001f)
        {
            return AttackRangeState.Ready;
        }

        Vector3 forward = Owner.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.000001f)
        {
            return AttackRangeState.NeedRotation;
        }

        float dot = Vector3.Dot(
            forward.normalized,
            offset.normalized
        );

        return dot >= minFacingDot
            ? AttackRangeState.Ready
            : AttackRangeState.NeedRotation;
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 편집 중에는 Owner가 등록되지 않으므로 부모에서 찾음
        CharacterBase character = Owner ? Owner : GetComponentInParent<CharacterBase>();

        if (!character || !character.Status) return;

        Vector3 center = character.transform.position;
        Vector3 forward = character.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.000001f) return;
        forward.Normalize();

        float selfRadius = character.Status.colliderRadius;
        float radius = selfRadius + attackRange + previewTargetRadius;

        Vector3 left = Quaternion.AngleAxis(-attackHalfAngle, Vector3.up) * forward;

        Vector3 right = Quaternion.AngleAxis(attackHalfAngle, Vector3.up) * forward;

        Color previousColor = UnityEditor.Handles.color;

        // 자기 몸통 반지름
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, selfRadius);

        // 방향을 제외한 거리 조건
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, radius);

        // 거리와 방향을 모두 만족하는 영역
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.DrawWireArc(center, Vector3.up, left, attackHalfAngle * 2f, radius);

        UnityEditor.Handles.DrawLine(center, center + left * radius);
        UnityEditor.Handles.DrawLine(center, center + right * radius);

        UnityEditor.Handles.color = previousColor;
    }
#endif
}