using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/MushroomPan", fileName = "ItemEffectData_MushroomPan")]
public class ItemEffectDataSO_MushroomPan : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        MushroomPan mushroomPan=player.GetComponentInChildren<MushroomPan>();
        if (mushroomPan != null)
        {
            mushroomPan.RoastAllMushroom();
            AudioManager.Instance.PlaySFX("UseItemMushroomPanSFX");
            return true;
        }
        return false;
    }
}
