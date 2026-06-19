using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Bandage", fileName = "ItemEffectData_Bandage")]
public class ItemEffectDataSO_Bandage : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.AddHealth(30);
            AudioManager.Instance.PlaySFX("UseItemBindUpSFX");
            return true;
        }
        return false;
    }
}
