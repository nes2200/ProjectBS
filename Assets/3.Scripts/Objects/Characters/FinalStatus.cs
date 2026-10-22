using System.Collections.Generic;
using UnityEngine;

public class FinalStatus
{
    public int maxHP { get; private set; }
    public int damage { get; private set; }
    public int defense { get; private set; }
    public float attackSpeed { get; private set; }
    public float moveSpeed { get; private set; }

    public void Calculate(UnitStatus origin, IEnumerable<StatModifier> modifiers)
    {
        float hpBonus = 0f;
        float damageBonus = 0f;
        float defenseBonus = 0f;
        float attackSpeedRate = 0f;
        float moveSpeedRate = 0f;

        foreach (var modifier in modifiers)
        {
            switch (modifier.stat)
            {
                case StatType.MaxHP:
                    hpBonus += modifier.value;
                    break;
                case StatType.Damage:
                    damageBonus += modifier.value;
                    break;
                case StatType.Defense:
                    defenseBonus += modifier.value;
                    break;
                case StatType.AttackSpeedRate:
                    attackSpeedRate += modifier.value;
                    break;
                case StatType.MoveSpeedRate:
                    moveSpeedRate += modifier.value;
                    break;
            }
        }

        maxHP = Mathf.Max(1, origin.MaxHP + Mathf.RoundToInt(hpBonus));
        damage = Mathf.Max(0, origin.Damage + Mathf.RoundToInt(damageBonus));
        defense = Mathf.Max(0, origin.Defense + Mathf.RoundToInt(defenseBonus));

        attackSpeed = Mathf.Max(0.01f, origin.AttackSpeed * (1f + attackSpeedRate));
        moveSpeed = Mathf.Max(0f, origin.MoveSpeed * (1f + moveSpeedRate));
    }
}
