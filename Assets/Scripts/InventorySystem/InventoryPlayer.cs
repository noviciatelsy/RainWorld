using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPlayer : InventoryBase
{
    [Header("快捷栏位")]
    [SerializeField] private int quickItemSlotSize = 4;
    public List<InventoryItemSlot> quickItemSlotList = new List<InventoryItemSlot>(); // 快捷栏物品槽位列表
    public event Action onQuickItemsChange;
    private Player player;
    [SerializeField] private ItemDataSO test_1;
    [SerializeField] private ItemDataSO test_2;
    [SerializeField] private ItemDataSO test_3;
    [SerializeField] private ItemDataSO test_4;
    [SerializeField] private ItemDataSO test_5; 
    [SerializeField] private ItemDataSO test_6;
    [SerializeField] private ItemDataSO test_7;
    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddItem(test_1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddItem(test_2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AddItem(test_3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            AddItem(test_4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            AddItem(test_5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            AddItem(test_6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            AddItem(test_7);
        }

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
        if (quickItemSlotSize< 1)
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
}