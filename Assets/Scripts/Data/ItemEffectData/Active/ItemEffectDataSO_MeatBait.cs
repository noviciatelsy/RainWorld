using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/MeatBait", fileName = "ItemEffectData_MeatBait")]
public class ItemEffectDataSO_MeatBait : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerMeatBaitThrower playerMeatBaitThrower=player.GetComponentInChildren<PlayerMeatBaitThrower>();
        if(playerMeatBaitThrower != null )
        {
            playerMeatBaitThrower.TryThrowMeatBait();
            return true;
        }
        return false;
    }
}
