using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/AntigravityRope", fileName = "ItemEffectData_AntigravityRope")]
public class ItemEffectDataSO_AntigravityRope : ItemEffectDataSO
{
    public override void StartHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.StartHoldingItem(item, inventoryPlayer);
        PlayerAntigravityRopeSpawner playerAntigravityRopeSpawner=player.GetComponentInChildren<PlayerAntigravityRopeSpawner>();
 
        if (playerAntigravityRopeSpawner != null)
        {
            playerAntigravityRopeSpawner.SetPreviewEnabled(true);
        }
    }

    public override void EndHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.EndHoldingItem(item, inventoryPlayer);
        PlayerAntigravityRopeSpawner playerAntigravityRopeSpawner = player.GetComponentInChildren<PlayerAntigravityRopeSpawner>();

        if (playerAntigravityRopeSpawner != null)
        {
            playerAntigravityRopeSpawner.SetPreviewEnabled(false);
        }
    }

    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerAntigravityRopeSpawner playerAntigravityRopeSpawner = player.GetComponentInChildren<PlayerAntigravityRopeSpawner>();
        if (playerAntigravityRopeSpawner != null)
        {
            playerAntigravityRopeSpawner.SpawnRope();
            return true;
        }
        return false;
    }
}
