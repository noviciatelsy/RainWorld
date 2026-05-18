using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class InventoryPlayer : InventoryBase
{
    [Header("快捷栏位")]
    [SerializeField] private int quickItemSlotSize = 4;

    public List<InventoryItemSlot> quickItemSlotList = new List<InventoryItemSlot>(); // 快捷栏物品槽位列表

    public event Action onQuickItemsChange;

    private Player player;
    private PlayerControl playerControl;

    [Header("测试物品")]
    [SerializeField] private ItemDataSO[] testItems;


    public int QuickItemSlotSize
    {
        get
        {
            return Mathf.Max(1, quickItemSlotSize);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<Player>();
        playerControl = GetComponent<PlayerControl>();

        EnsureSlotListSize();
        SanitizeEmptyItemShells();
        ValidateQuickItems(null);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            GetRandomItem();
        }
    }

    private void GetRandomItem()
    {
        int randomIndex = Random.Range(0, testItems.Length);
        AddItem(testItems[randomIndex]);
    }


    public InventoryItem GetQuickItem(int quickSlotIndex)
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();

        if (!IsValidQuickSlotIndex(quickSlotIndex))
        {
            return null;
        }

        InventoryItemSlot slot = quickItemSlotList[quickSlotIndex];

        if (slot == null || !slot.HasItem())
        {
            if (slot != null)
            {
                slot.itemInSlot = null;
            }

            return null;
        }

        return slot.itemInSlot;
    }

    public bool SetQuickItem(InventoryItem itemToSet, int quickSlotIndex)
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();

        if (!IsValidQuickSlotIndex(quickSlotIndex))
        {
            Debug.LogWarning($"设置快捷栏失败：快捷栏下标 {quickSlotIndex} 不合法。");
            return false;
        }

        if (itemToSet == null || itemToSet.ItemData == null)
        {
            return ClearQuickItem(quickSlotIndex);
        }

        if (itemToSet.ItemData.itemType != ItemType.Active)
        {
            Debug.Log($"设置快捷栏失败：{itemToSet.ItemData.itemDisplayName} 不是主动道具。");
            return false;
        }

        if (!ContainsItem(itemToSet))
        {
            Debug.Log($"设置快捷栏失败：{itemToSet.ItemData.itemDisplayName} 当前不在玩家背包里。");
            return false;
        }

        InventoryItem currentItem = GetQuickItem(quickSlotIndex);

        // 如果这个栏位已经装的是同一个物品，则取消装备
        if (currentItem == itemToSet)
        {
            return ClearQuickItem(quickSlotIndex);
        }

        bool changed = false;

        // 同一个物品不建议同时占多个快捷栏位，所以先从其他快捷栏里清掉
        changed |= ClearQuickItemInternal(itemToSet);

        quickItemSlotList[quickSlotIndex].itemInSlot = itemToSet;
        changed = true;

        if (changed)
        {
            onQuickItemsChange?.Invoke();
        }

        return true;
    }

    public bool ClearQuickItem(int quickSlotIndex)
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();

        if (!IsValidQuickSlotIndex(quickSlotIndex))
        {
            return false;
        }

        bool changed = ClearQuickItemAtInternal(quickSlotIndex);

        if (changed)
        {
            onQuickItemsChange?.Invoke();
        }

        return changed;
    }



    public void ValidateQuickItems(InventoryItem temporarilyAllowedItem)
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();

        bool changed = false;

        for (int i = 0; i < quickItemSlotList.Count; i++)
        {
            InventoryItem item = GetQuickItem(i);

            if (item == null)
            {
                continue;
            }

            bool isActiveItem =
                item.ItemData != null &&
                item.ItemData.itemType == ItemType.Active;

            bool stillBelongsToPlayer =
                ContainsItem(item);

            bool isBeingDragged =
                temporarilyAllowedItem != null &&
                item == temporarilyAllowedItem;

            if (!isActiveItem || (!stillBelongsToPlayer && !isBeingDragged))
            {
                quickItemSlotList[i].itemInSlot = null;
                changed = true;
            }
        }

        if (changed)
        {
            onQuickItemsChange?.Invoke();
        }
    }

    public bool ContainsItem(InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        SanitizeEmptyItemShells();

        if (inventoryItems != null && inventoryItems.Contains(item))
        {
            return true;
        }

        for (int i = 0; i < itemSlotList.Count; i++)
        {
            InventoryItemSlot slot = itemSlotList[i];

            if (slot == null || !slot.HasItem())
            {
                continue;
            }

            if (slot.itemInSlot == item)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsValidQuickSlotIndex(int quickSlotIndex)
    {
        return quickSlotIndex >= 0 && quickSlotIndex < quickItemSlotList.Count;
    }

    protected override void OnItemPlaced(InventoryItem item)
    {
        item?.SubscribeToPlayer(player);
    }

    protected override void OnItemRemoved(InventoryItem item)
    {
        item?.UnsubscribeToPlayer();
    }

    protected override void EnsureSlotListSize()
    {
        base.EnsureSlotListSize();

        if (quickItemSlotSize < 1)
        {
            quickItemSlotSize = 1;
        }

        if (quickItemSlotList == null)
        {
            quickItemSlotList = new List<InventoryItemSlot>();
        }

        while (quickItemSlotList.Count < quickItemSlotSize)
        {
            quickItemSlotList.Add(new InventoryItemSlot()); // 补空槽
        }

        if (quickItemSlotList.Count > quickItemSlotSize)
        {
            quickItemSlotList.RemoveRange(quickItemSlotSize, quickItemSlotList.Count - quickItemSlotSize);
        }
    }

    protected override void SanitizeEmptyItemShells()
    {
        base.SanitizeEmptyItemShells();

        if (quickItemSlotList == null)
        {
            return;
        }

        for (int i = 0; i < quickItemSlotList.Count; i++)
        {
            if (quickItemSlotList[i] == null)
            {
                quickItemSlotList[i] = new InventoryItemSlot();
                continue;
            }

            quickItemSlotList[i].ClearIfInvalid();
        }
    }

    private bool ClearQuickItemInternal(InventoryItem itemToClear)
    {
        if (itemToClear == null)
        {
            return false;
        }

        bool changed = false;

        for (int i = 0; i < quickItemSlotList.Count; i++)
        {
            if (quickItemSlotList[i] == null)
            {
                quickItemSlotList[i] = new InventoryItemSlot();
                continue;
            }

            if (quickItemSlotList[i].itemInSlot == itemToClear)
            {
                quickItemSlotList[i].itemInSlot = null;
                changed = true;
            }
        }

        return changed;
    }

    private bool ClearQuickItemAtInternal(int quickSlotIndex)
    {
        if (!IsValidQuickSlotIndex(quickSlotIndex))
        {
            return false;
        }

        if (quickItemSlotList[quickSlotIndex] == null)
        {
            quickItemSlotList[quickSlotIndex] = new InventoryItemSlot();
            return false;
        }

        if (quickItemSlotList[quickSlotIndex].itemInSlot == null)
        {
            return false;
        }

        quickItemSlotList[quickSlotIndex].itemInSlot = null;
        return true;
    }
}