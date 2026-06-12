using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Coffee", fileName = "ItemEffectData_Coffee")]
public class ItemEffectDataSO_Coffee : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerVitals != null&&playerControl!=null)
        {
            playerVitals.AddHunger(10);
            playerControl.AddMoveSpeedTemporarily(2, 30);
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
        return true;
    }
}
