using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Water", fileName = "ItemEffectData_Water")]
public class ItemEffectDataSO_Water : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.PauseAutoIncreaseHunger(300);
            AudioManager.Instance.PlaySFX("UseItemDrinkSFX");
            return true;
        }
        return false;
    }

    public override bool SecondaryUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        inventoryPlayer.DropItem(item.ItemData).AddComponent<DroppedLiquid>();
        return true;
    }
}
