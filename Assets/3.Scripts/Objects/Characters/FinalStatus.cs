using UnityEngine;

public class FinalStatus
{
    public int maxHP { get; private set; }
    public int damage { get; private set; }
    public int defense { get; private set; }
    public float attackSpeed { get; private set; }
    public float moveSpeed { get; private set; }

    public void Calculate(
        UnitStatus origin, int hpBonus, int damageBonus, int defenseBonus, float attackSpeedMultiplier, float moveSpeedMultiplier)
    {
        maxHP = Mathf.Max(1, origin.maxHP + hpBonus);
        damage = Mathf.Max(0, origin.damage + damageBonus);
        defense = Mathf.Max(0, origin.defense + defenseBonus);

        attackSpeed = Mathf.Max(0.01f, origin.attackSpeed * attackSpeedMultiplier);
        moveSpeed = Mathf.Max(0f, origin.moveSpeed * moveSpeedMultiplier);
    }
}
