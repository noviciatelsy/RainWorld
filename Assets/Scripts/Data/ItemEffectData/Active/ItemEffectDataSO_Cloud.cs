using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Cloud", fileName = "ItemEffectData_Cloud")]
public class ItemEffectDataSO_Cloud : ItemEffectDataSO
{
    public override void StartHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.StartHoldingItem(item, inventoryPlayer);
        PlayerCloudSpawner playerCloudSpawner=player.GetComponentInChildren<PlayerCloudSpawner>();
        if(playerCloudSpawner != null )
        {
            playerCloudSpawner.EnablePreviewCloudSpawnPosition();
        }
    }

    public override void EndHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.EndHoldingItem(item, inventoryPlayer);
        PlayerCloudSpawner playerCloudSpawner = player.GetComponentInChildren<PlayerCloudSpawner>();
        if (playerCloudSpawner != null)
        {
            playerCloudSpawner.DisablePreviewCloudSpawnPosition();
        }
    }

    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerCloudSpawner playerCloudSpawner = player.GetComponentInChildren<PlayerCloudSpawner>();
        if (playerCloudSpawner != null)
        {
            playerCloudSpawner.SpawnCloudPlatform();
            AudioManager.Instance.PlaySFX("UseItemWhooshSFX");
            return true;
        }
        return false;
    }
}
