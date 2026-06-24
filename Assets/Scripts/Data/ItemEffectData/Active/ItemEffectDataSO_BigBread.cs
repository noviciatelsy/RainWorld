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
            playerVitals.ReduceHunger(30);
            playerVitals.AddHealth(30);
            AudioManager.Instance.PlaySFX("UseItemEatSFX");
            return true;
        }
        return false;
    }
}
