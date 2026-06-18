using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/ToyCar", fileName = "ItemEffectData_ToyCar")]
public class ItemEffectDataSO_ToyCar : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerToyCarSpawner playerToyCarSpawner = player.GetComponentInChildren<PlayerToyCarSpawner>();
        if (playerToyCarSpawner != null)
        {
            AudioManager.Instance.PlaySFX("UseItemToyCarSFX");
            playerToyCarSpawner.TrySpawnToyCar();
            return true;
        }
        return false;

    }
}
