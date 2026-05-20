using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public ItemDataSO ItemData; // 物品信息

    // 这里的 itemEffect 将不再指向 Asset，本质变为“运行时克隆出来的一份实例”
    public ItemEffectDataSO itemEffect; 
    public ItemRotateState rotateState;
    public InventoryItem(ItemDataSO ItemData)
    {
        this.ItemData = ItemData;

        // 不再把 Asset 直接塞给 itemEffect，而是对配置的 ItemEffect 做一次运行时克隆
        if (ItemData.itemEffectData != null)
        {
            // 这里用的是 UnityEngine.Object.Instantiate，
            // 生成一份只存在于运行时内存中的 ScriptableObject 实例
            itemEffect = UnityEngine.Object.Instantiate(ItemData.itemEffectData);
        }
        else
        {
            itemEffect = null;
        }

    }

    public void SubscribeToPlayer(Player player)
    {
        itemEffect?.Subscribe(player); 
    }

    public void UnsubscribeToPlayer()
    {
        itemEffect?.Unsubscribe(); 
    }

    public void StartHoldingItem(InventoryPlayer inventoryPlayer)
    {
        itemEffect?.StartHoldingItem(this, inventoryPlayer);
    }

    public void EndHoldingItem(InventoryPlayer inventoryPlayer)
    {
        itemEffect?.EndHoldingItem(this, inventoryPlayer);
    }

    public bool MainUse(InventoryPlayer inventoryPlayer)
    {
        if (itemEffect == null)
        {
            return false;
        }

        return itemEffect.MainUse(this, inventoryPlayer);
    }

    public bool SecondaryUse(InventoryPlayer inventoryPlayer)
    {
        if (itemEffect == null)
        {
            return false;
        }

        return itemEffect.SecondaryUse(this, inventoryPlayer);
    }

}
