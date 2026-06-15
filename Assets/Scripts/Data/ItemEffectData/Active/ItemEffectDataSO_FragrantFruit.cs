using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Setup/ItemEffect Data/FragrantFruit", fileName = "ItemEffectData_FragrantFruit")]

public class ItemEffectDataSO_FragrantFruit : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.ReduceHunger(10);
            return true;
        }
        return false;
    }
}
