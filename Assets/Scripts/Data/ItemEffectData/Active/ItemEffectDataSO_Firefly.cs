using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Firefly", fileName = "ItemEffectData_Firefly")]
public class ItemEffectDataSO_Firefly : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerFireflySpawner playerFireflySpawner = player.GetComponentInChildren<PlayerFireflySpawner>();
        if (playerFireflySpawner != null)
        {
            playerFireflySpawner.SpawnFireFly();
            AudioManager.Instance.PlaySFX("UseItemWhooshSFX");
            return true;
        }

        return false;
    }

    public override bool SecondaryUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerFireflyThrower thrower = player.GetComponentInChildren<PlayerFireflyThrower>();
        if (thrower != null)
        {
            return thrower.TryThrowFirefly();
        }

        return false;
    }
}
