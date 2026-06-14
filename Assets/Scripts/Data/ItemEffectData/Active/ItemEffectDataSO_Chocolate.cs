using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Chocolate", fileName = "ItemEffectData_Chocolate")]
public class ItemEffectDataSO_Chocolate : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.ReduceHunger(5);
            playerVitals.AddHealth(15);

            return true;
        }
        return false;
    }


}
