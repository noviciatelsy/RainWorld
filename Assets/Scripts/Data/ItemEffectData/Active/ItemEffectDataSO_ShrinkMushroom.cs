using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/ShrinkMushroom", fileName = "ItemEffectData_ShrinkMushroom")]
public class ItemEffectDataSO_ShrinkMushroom : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        ShrinkMushroom shrinkMushroom=player.GetComponentInChildren<ShrinkMushroom>();
        if (shrinkMushroom != null)
        {
            shrinkMushroom.UseMushroom();
            AudioManager.Instance.PlaySFX("UseItemEatSFX");
            AudioManager.Instance.PlaySFX("UseItemShrinkMushroomSFX");
            return true;
        }
        return false;

    }
}
