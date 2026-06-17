using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/MosquitoCoil", fileName = "ItemEffectData_MosquitoCoil")]
public class ItemEffectDataSO_MosquitoCoil : ItemEffectDataSO
{
    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        if (inventoryPlayer == null)
        {
            return false;
        }

        Player targetPlayer = inventoryPlayer.GetComponent<Player>() ?? player;
        if (targetPlayer == null)
        {
            return false;
        }

        MosquitoCoil mosquitoCoil = targetPlayer.GetComponentInChildren<MosquitoCoil>(true);
        if (mosquitoCoil == null)
        {
            Debug.LogWarning("使用蚊香失败：Player 的 ItemAbilities 下未找到 MosquitoCoil 组件。");
            return false;
        }

        return mosquitoCoil.UseMosquitoCoil();
    }
}
