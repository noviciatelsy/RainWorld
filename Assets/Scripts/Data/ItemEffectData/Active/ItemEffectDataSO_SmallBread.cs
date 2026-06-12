using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/SmallBread", fileName = "ItemEffectData_SmallBread")]
public class ItemEffectDataSO_SmallBread : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.AddHealth(10);
            playerVitals.ReduceHunger(10);
            return true;
        }
        return false;
    }
}
