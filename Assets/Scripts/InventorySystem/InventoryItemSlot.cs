using System;
using UnityEngine;

[Serializable]
public class InventoryItemSlot
{
    public InventoryItem itemInSlot; // 槽位所填充的物品Inventory_Item对象

    public bool HasItem() // 该槽位是否含Inventory_Item对象
    {
        return itemInSlot != null && itemInSlot.ItemData != null;
    }
}
