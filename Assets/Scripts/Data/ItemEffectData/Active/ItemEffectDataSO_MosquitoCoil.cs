using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/MosquitoCoil", fileName = "ItemEffectData_MosquitoCoil")]
public class ItemEffectDataSO_MosquitoCoil : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        MosquitoCoil mosquitoCoil=player.GetComponentInChildren<MosquitoCoil>();
        if (mosquitoCoil != null)
        {
            mosquitoCoil.UseMosquitoCoil();
            return true;
        }
        return false;
    }
}
