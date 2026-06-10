using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Treasure", fileName = "ItemEffectData_Treasure")]
public class ItemEffectDataSO_Treasure : ItemEffectDataSO
{
    public override bool SecondaryUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        inventoryPlayer.DropItem(item.ItemData).AddComponent<DroppedTreasure>();
        return true;
    }
}
