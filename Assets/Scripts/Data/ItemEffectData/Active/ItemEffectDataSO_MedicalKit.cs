using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/MedicalKit", fileName = "ItemEffectData_MedicalKit")]
public class ItemEffectDataSO_MedicalKit : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.AddHealth(100);
            return true;
        }
        return false;
    }
}
