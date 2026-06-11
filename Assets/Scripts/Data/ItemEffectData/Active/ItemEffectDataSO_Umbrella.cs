using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Umbrella", fileName = "ItemEffectData_Umbrella")]
public class ItemEffectDataSO_Umbrella : ItemEffectDataSO
{
    public override void StartHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.StartHoldingItem(item, inventoryPlayer);
        PlayerUmbrella playerUmbrella=player.GetComponentInChildren<PlayerUmbrella>();
        if (playerUmbrella != null )
        {
            playerUmbrella.OpenUmbrella();
        }
    }

    public override void EndHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.EndHoldingItem(item, inventoryPlayer);
        PlayerUmbrella playerUmbrella = player.GetComponentInChildren<PlayerUmbrella>();
        if (playerUmbrella != null)
        {
            playerUmbrella.CloseUmbrella();
        }
    }
}
