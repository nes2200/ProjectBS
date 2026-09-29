using UnityEngine;

public class BowAttackExecutor : AttackExecutor
{
    [SerializeField] GameObject arrowInHand;
    [SerializeField] ArrowProjectile projectilePrefab;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float targetHeightOffset = 1f;

    public override void Execute(in AttackInfo info)
    {
        if (!projectilePrefab || !spawnPoint) return;
        if (!info.target || !info.target.IsAlive) return;

        Vector3 targetPosition = info.target.transform.position + Vector3.up * targetHeightOffset;
        Vector3 direction = targetPosition - spawnPoint.position;

        arrowInHand.SetActive(false);   

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized);
        ArrowProjectile arrow = Instantiate(projectilePrefab, spawnPoint.position, rotation);

        arrow.Initialize(info, direction);
    }

    public override void Prepare()
    {
        arrowInHand.SetActive(true);
    }
}
