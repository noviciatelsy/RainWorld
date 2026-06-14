using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/GreenMushroom", fileName = "ItemEffectData_GreenMushroom")]
public class ItemEffectDataSO_GreenMushroom : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerVitals != null && playerControl != null)
        {
            playerVitals.ReduceHunger(40);
            playerVitals.AddHealth(40);
            playerControl.ReduceJumpForceTemporarily(4, 60);
            return true;
        }
        return false;
    }

}
