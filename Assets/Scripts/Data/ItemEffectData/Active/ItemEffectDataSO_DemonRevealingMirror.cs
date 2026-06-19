using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/DemonRevealingMirror", fileName = "ItemEffectData_DemonRevealingMirror")]
public class ItemEffectDataSO_DemonRevealingMirror : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        DemonRevealingMirror demonRevealingMirror=player.GetComponentInChildren<DemonRevealingMirror>();
        if(demonRevealingMirror != null )
        {
            demonRevealingMirror.UseMirror();
            AudioManager.Instance.PlaySFX("UseItemDemonRevealingMirrorSFX");
            return true;
        }
        return false;
    }
}
