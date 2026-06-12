using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/BigBread", fileName = "ItemEffectData_BigBread")]
public class ItemEffectDataSO_BigBread : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.AddHealth(40);
            playerVitals.ReduceHunger(40);
            return true;
        }
        return false;
    }
}
