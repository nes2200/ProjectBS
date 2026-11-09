using UnityEngine;

public class MagicAttackExecutor : AttackExecutor
{
    [SerializeField] string projectilePoolName = "FireballBasic";
    [SerializeField] Transform spawnPoint;

    public override void Execute(in AttackInfo info)
    {
        if (!spawnPoint) return;
        if (!info.target || !info.target.IsAlive) return;

        Vector3 spawnPosition = spawnPoint.position;
        Vector3 direction = info.target.AimPosition - spawnPosition;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized);

        GameObject projectileObject = ObjectManager.CreateObject(projectilePoolName, spawnPosition, rotation);

        if (!projectileObject) return;

        if (!projectileObject.TryGetComponent(out MagicProjectile magic))
        {
            Debug.LogError("마법 프리팹에 MagicProjectile이 없습니다.", projectileObject);
            ObjectManager.DestroyObject(projectileObject);
            return;
        }
        magic.Initialize(info, spawnPosition, direction);
    }
}