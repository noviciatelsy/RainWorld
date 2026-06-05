using System;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public string runtimeSaveID; // 运行时唯一ID，用于存档中区分同类物品的不同实例

    public ItemDataSO ItemData; // 物品信息

    // 这里的 itemEffect 将不再指向 Asset，本质变为“运行时克隆出来的一份实例”
    public ItemEffectDataSO itemEffect;

    public ItemRotateState rotateState;

    public InventoryItem(ItemDataSO myItemData)
        : this(myItemData, Guid.NewGuid().ToString())
    {

    }

    public InventoryItem(ItemDataSO myItemData, string myRuntimeSaveID)
    {
        ItemData = myItemData;
        runtimeSaveID = string.IsNullOrEmpty(myRuntimeSaveID)
            ? Guid.NewGuid().ToString()
            : myRuntimeSaveID;

        if (ItemData != null && ItemData.itemEffectData != null)
        {
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