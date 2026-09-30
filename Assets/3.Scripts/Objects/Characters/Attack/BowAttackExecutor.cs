using UnityEngine;

public class BowAttackExecutor : AttackExecutor
{
    [SerializeField] GameObject arrowInHand;
    [SerializeField] ArrowProjectile projectilePrefab;
    [SerializeField] Transform spawnPoint;

    public override void Execute(in AttackInfo info)
    {
        if (!projectilePrefab || !spawnPoint) return;
        if (!info.target || !info.target.IsAlive) return;

        Vector3 targetPosition = info.target.AimPosition;
        Vector3 direction = targetPosition - spawnPoint.position;

        arrowInHand.SetActive(false);   

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized);
        GameObject arrowObject = ObjectManager.CreateObject("ArrowBasic", spawnPoint.position, rotation);
        ArrowProjectile arrow = arrowObject.GetComponent<ArrowProjectile>();
        arrow.Initialize(info, spawnPoint.position, direction);
    }

    public override void Prepare()
    {
        arrowInHand.SetActive(true);
    }
}
