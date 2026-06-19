using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/RoastedMushroom", fileName = "ItemEffectData_RoastedMushroom")]
public class ItemEffectDataSO_RoastedMushroom : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.ReduceHunger(20);
            playerVitals.AddHealth(20);
            AudioManager.Instance.PlaySFX("UseItemEatSFX");
            return true;
        }
        return false;
    }
}
