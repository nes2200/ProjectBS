public enum StatType
{
    MaxHP,
    Damage,
    Defense,
    AttackSpeedRate,
    MoveSpeedRate
}

[System.Serializable]
public struct StatModifier
{
    public StatType stat;
    public float value;
}
