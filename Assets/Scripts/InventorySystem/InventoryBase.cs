using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryBase : MonoBehaviour
{
    public event Action onInventoryChange; // 改变事件

    public int maxInventorySize; // 容量
    public List<InventoryItemSlot> itemSlotList = new List<InventoryItemSlot>(); // 物品槽位列表
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    public ItemListDataSO itemDataBase; // 全物品SO

    protected virtual void Awake()
    {
        EnsureSlotListSize();
    }
  
#if UNITY_EDITOR
    private void OnValidate() { EnsureSlotListSize(); }
#endif

    private void EnsureSlotListSize()
    {
        if (itemSlotList == null) itemSlotList = new List<InventoryItemSlot>();
        while (itemSlotList.Count < maxInventorySize)
            itemSlotList.Add(new InventoryItemSlot());  // 补空槽
        if (itemSlotList.Count > maxInventorySize)
            itemSlotList.RemoveRange(maxInventorySize, itemSlotList.Count - maxInventorySize);
    }


}
