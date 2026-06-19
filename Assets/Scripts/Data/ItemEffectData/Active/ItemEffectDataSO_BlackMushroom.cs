using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Setup/ItemEffect Data/BlackMushroom", fileName = "ItemEffectData_BlackMushroom")]
public class ItemEffectDataSO_BlackMushroom : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        BlackMushroom blackMushroom=player.GetComponentInChildren<BlackMushroom>();
        if (playerVitals != null && blackMushroom != null)
        {
            playerVitals.ReduceHunger(40);
            playerVitals.AddHealth(40);
            blackMushroom.HideSpritesTemporarily(60);
            AudioManager.Instance.PlaySFX("UseItemEatSFX");
            return true;
        }
        return false;
    }
}
