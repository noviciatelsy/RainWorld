using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Candy", fileName = "ItemEffectData_Candy")]
public class ItemEffectDataSO_Candy : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals=player.GetComponent<PlayerVitals>();
        if(playerVitals != null)
        {
            playerVitals.AddHealth(5);
            return true;
        }
        return false;
    }
}
