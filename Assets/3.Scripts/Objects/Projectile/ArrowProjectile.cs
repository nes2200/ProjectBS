using UnityEngine;

public class ArrowProjectile : ProjectileBase
{
    [Header("Time")]
    [SerializeField, Min(0f)] float hitRemainTime = 0.5f;

    [Header("Component")]
    [SerializeField] TrailRenderer trail;

    Transform attachedTarget;
    Vector3 attachedLocalPosition;
    Quaternion attachedLocalRotation;

    protected override void OnInitialized()
    {
        if (!trail) return;

        trail.Clear();
        trail.emitting = true;
    }

    protected override void OnHit(CharacterBase target, Collider hitCollider)
    {
        attachedTarget = hitCollider.transform;
        attachedLocalPosition = attachedTarget.InverseTransformPoint(transform.position);
        attachedLocalRotation = Quaternion.Inverse(attachedTarget.rotation) * transform.rotation;

        if (trail) trail.emitting = false;

        GameManager.OnLateObject -= FollowTarget;
        GameManager.OnLateObject += FollowTarget;

        ReturnAfter(hitRemainTime);
    }

    private void FollowTarget(float deltaTime)
    {
        if (!hasHit) return;
        if (!attachedTarget)
        {
            ObjectManager.DestroyObject(gameObject);
            return;
        }

        Vector3 worldPosition = attachedTarget.TransformPoint(attachedLocalPosition);
        Quaternion worldRotation = attachedTarget.rotation * attachedLocalRotation;
        transform.SetPositionAndRotation(worldPosition, worldRotation);
    }

    protected override void OnReleased()
    {
        GameManager.OnLateObject -= FollowTarget;

        if (trail)
        {
            trail.emitting = false;
            trail.Clear();
        }

        attachedTarget = null;
        attachedLocalPosition = Vector3.zero;
        attachedLocalRotation = Quaternion.identity;
    }
}
