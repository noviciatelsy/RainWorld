using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Rope", fileName = "ItemEffectData_Rope")]
public class ItemEffectDataSO_Rope : ItemEffectDataSO
{
    public override void StartHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.StartHoldingItem(item, inventoryPlayer);
        PlayerRopeSpawner playerRopeSpawner=player.GetComponentInChildren<PlayerRopeSpawner>();
        if(playerRopeSpawner != null )
        {
            playerRopeSpawner.SetPreviewEnabled(true);
        }
    }

    public override void EndHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.EndHoldingItem(item, inventoryPlayer);
        PlayerRopeSpawner playerRopeSpawner = player.GetComponentInChildren<PlayerRopeSpawner>();
        if (playerRopeSpawner != null)
        {
            playerRopeSpawner.SetPreviewEnabled(false);
        }
    }

    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerRopeSpawner playerRopeSpawner = player.GetComponentInChildren<PlayerRopeSpawner>();
        if (playerRopeSpawner != null)
        {
            playerRopeSpawner.SpawnRope();
            return true;
        }
        return false;
    }
}
