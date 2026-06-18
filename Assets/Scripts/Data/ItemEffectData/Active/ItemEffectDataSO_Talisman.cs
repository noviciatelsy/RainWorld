using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Talisman", fileName = "ItemEffectData_Talisman")]
public class ItemEffectDataSO_Talisman : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerTalismanThrower playerTalismanThrower=player.GetComponentInChildren<PlayerTalismanThrower>();
        if(playerTalismanThrower != null )
        {
            playerTalismanThrower.TryThrowTalisman();
            AudioManager.Instance.PlaySFX("UseItemWhooshSFX");
            return true;
        }
        return false;
    }
}
