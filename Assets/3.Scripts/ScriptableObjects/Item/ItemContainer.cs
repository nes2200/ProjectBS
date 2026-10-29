using UnityEngine;

public enum ItemType
{
    Equipment = 500, 
    Consumable = 400, 
    Important = 100,
    Material = 50, 
    Quest = 40, 
    Miscellaneous = 0,
    Length
}

[CreateAssetMenu(fileName = "ItemContainer", menuName = "Item/ItemBase")]
public class ItemContainer :InfoContainer
{
    [Header("Item Detail")]
    public ItemType type;
    public int maxStack;

    public virtual int CompareByType(ItemContainer other)
    {
        if (other == null) return 1;

        // - : 왼쪽이 작다
        // 0 : 같다
        // + : 왼쪽이 크다
        int result = type - other.type;
        if (result != 0) return result;
        return id - other.id;
    }
    public virtual int CompareByType(ItemSlot mySlot, ItemSlot otherSlot)
    {
        return default;
    }
}
