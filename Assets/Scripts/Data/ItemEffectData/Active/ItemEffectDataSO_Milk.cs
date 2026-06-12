using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Milk", fileName = "ItemEffectData_Milk")]
public class ItemEffectDataSO_Milk : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerVitals != null && playerControl != null)
        {
            playerVitals.AddHealth(25);
            playerVitals.AddDefenseTemporarily(3, 60);
            return true;
        }
        return false;
    }

    public override bool SecondaryUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        inventoryPlayer.DropItem(item.ItemData).AddComponent<DroppedLiquid>();
        inventoryPlayer.DropItem(item.ItemData).AddComponent<DroppedMilk>();
        return true;
    }
}
