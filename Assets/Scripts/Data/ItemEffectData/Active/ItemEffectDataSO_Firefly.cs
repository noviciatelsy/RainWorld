using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Firefly", fileName = "ItemEffectData_Firefly")]
public class ItemEffectDataSO_Firefly : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerFireflySpawner playerFireflySpawner=player.GetComponentInChildren<PlayerFireflySpawner>();
        if(playerFireflySpawner != null )
        {
            playerFireflySpawner.SpawnFireFly();
            return true;
        }
        return false;
    }
}
