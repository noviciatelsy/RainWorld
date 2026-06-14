using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Baguette", fileName = "ItemEffectData_Baguette")]
public class ItemEffectDataSO_Baguette : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.ReduceHunger(60);
            playerVitals.AddHealth(60);

            return true;
        }
        return false;
    }
}
