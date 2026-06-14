using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/SpottedMushroom", fileName = "ItemEffectData_SpottedMushroom")]
public class ItemEffectDataSO_SpottedMushroom : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null&&BlurEffectManager.Instance!=null)
        {
            playerVitals.ReduceHunger(40);
            playerVitals.AddHealth(40);

            BlurEffectManager.Instance.StartTemporaryBlur(60);
            return true;
        }
        return false;
    }
}
