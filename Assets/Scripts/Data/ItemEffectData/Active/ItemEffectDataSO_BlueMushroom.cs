using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/BlueMushroom", fileName = "ItemEffectData_BlueMushroom")]
public class ItemEffectDataSO_BlueMushroom : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerVitals != null && playerControl != null)
        {
            playerVitals.ReduceHunger(40);
            playerVitals.AddHealth(40);
            playerControl.ReduceMoveSpeedTemporarily(2, 60);
            return true;
        }
        return false;
    }
}
