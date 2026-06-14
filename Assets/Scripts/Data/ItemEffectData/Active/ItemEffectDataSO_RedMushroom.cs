using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/RedMushroom", fileName = "ItemEffectData_RedMushroom")]
public class ItemEffectDataSO_RedMushroom : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.ReduceHunger(100);
            playerVitals.AddHealth(100);
            playerVitals.ReduceHealthOverTime(50, 50);
            return true;
        }
        return false;
    }
}
