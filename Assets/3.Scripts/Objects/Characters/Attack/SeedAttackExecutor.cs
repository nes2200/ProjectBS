using UnityEngine;

public class SeedAttackExecutor : AttackExecutor
{
    [SerializeField] string projectilePoolName = "Seed";
    [SerializeField] Transform[] spawnPoints;

    public System.Collections.Generic.IReadOnlyList<Transform> SpawnPoints => spawnPoints;

    public override void Execute(in AttackInfo info)
    {
        if (!info.target || !info.target.IsAlive) return;
        if (spawnPoints == null) return;

        Vector3 targetPosition = info.target.AimPosition;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!spawnPoint) continue;

            Vector3 spawnPosition = spawnPoint.position;
            Vector3 direction = targetPosition - spawnPosition;

            if (direction.sqrMagnitude < 0.001f) continue;

            Quaternion rotation = Quaternion.LookRotation(direction.normalized);

            GameObject seedObject = ObjectManager.CreateObject(projectilePoolName, spawnPosition, rotation);

            if (!seedObject) continue;

            if (!seedObject.TryGetComponent(out ProjectileBase projectile))
            {
                Debug.LogError("Seed에 투사체 컴포넌트가 없습니다.", seedObject);
                ObjectManager.DestroyObject(seedObject);
                continue;
            }

            projectile.Initialize(info, spawnPosition, direction);
        }
    }
}
