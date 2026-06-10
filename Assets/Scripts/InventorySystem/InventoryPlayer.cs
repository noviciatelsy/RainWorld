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
    private PlayerHeldItem playerHeldItem;
    [Header("手持主动道具")]
    [SerializeField] private bool clearHoldingItemWhenInvalid = true;

    public InventoryItem holdingItem { get; private set; }
    public int money { get; private set; } = 0;

    public event Action<InventoryItem> onHoldingItemChange;
    public event Action<int> onMoneyChanged;
    public event Action<int> onMoneyAdd;

    [Header("可拾取物品")]
    [SerializeField] private PickableObject pickableObject;

    [Header("丢弃物品")]
    [SerializeField] private Transform itemDropPosition;

    [Header("遗失物品存档id")]
    [SerializeField] private string retrieveInventorySaveID = "retrieveInventory";

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
        playerHeldItem = GetComponent<PlayerHeldItem>();

        EnsureSlotListSize();
        SanitizeEmptyItemShells();
        ValidateQuickItems(null);
        ValidateHoldingItem(null);
    }

    private void Start()
    {
        if(GameStateManager.Instance.currentGameState==GameState.Base
            || GameStateManager.Instance.currentGameState == GameState.Game)
        {
            LoadData();
        }
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance.currentGameState == GameState.Base
            || GameStateManager.Instance.currentGameState == GameState.Game)
        {
            SaveData();
        }
    }

    private void OnEnable()
    {
        SaveManager.Instance.OnGameRunDataOverwrite += LoadData;
    }

    private void OnDisable()
    {
        SaveManager.Instance.OnGameRunDataOverwrite -= LoadData;
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
        int randomIndex = Random.Range(0, itemDataBase.itemList.Length);
        AddItem(itemDataBase.itemList[randomIndex]);
    }

    public InventoryItem GetHoldingItem()
    {
        return holdingItem;
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

            bool stillBelongsToPlayer = ContainsItem(item);

            bool isBeingDragged =
                temporarilyAllowedItem != null &&
                item == temporarilyAllowedItem;


            if (!stillBelongsToPlayer && !isBeingDragged)
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

    public bool TryToggleHoldingItem(InventoryItem itemToHold)
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();

        if (itemToHold == null || itemToHold.ItemData == null)
        {
            return false;
        }

        if (!ContainsItem(itemToHold))
        {
            Debug.Log($"手持失败：{itemToHold.ItemData.itemDisplayName} 当前不在玩家背包里。");
            return false;
        }

        // 已经手持这个物品时，再次尝试手持它 = 取消手持
        if (holdingItem == itemToHold)
        {
            ClearHoldingItem();
            return true;
        }

        SetHoldingItem(itemToHold);
        return true;
    }

    public bool TryHoldQuickItem(int quickSlotIndex)
    {
        InventoryItem quickItem = GetQuickItem(quickSlotIndex);

        if (quickItem == null)
        {
            return false;
        }

        return TryToggleHoldingItem(quickItem);
    }

    public void SetHoldingItem(InventoryItem itemToHold)
    {
        if (itemToHold == null || itemToHold.ItemData == null)
        {
            ClearHoldingItem();
            return;
        }

        if (holdingItem == itemToHold)
        {
            return;
        }

        InventoryItem oldHoldingItem = holdingItem;

        if (oldHoldingItem != null)
        {
            oldHoldingItem.EndHoldingItem(this);
        }

        holdingItem = itemToHold;

        if (playerHeldItem != null)
        {
            playerHeldItem.StartHoldingItem(holdingItem.ItemData);
        }

        holdingItem.StartHoldingItem(this);

        onHoldingItemChange?.Invoke(holdingItem);
    }

    public bool ClearHoldingItem()
    {
        if (holdingItem == null)
        {
            return false;
        }

        InventoryItem oldHoldingItem = holdingItem;

        oldHoldingItem.EndHoldingItem(this);

        holdingItem = null;

        if (playerHeldItem != null)
        {
            playerHeldItem.EndHoldingItem();
        }

        onHoldingItemChange?.Invoke(null);
        return true;
    }

    public void ValidateHoldingItem(InventoryItem temporarilyAllowedItem)
    {
        if (!clearHoldingItemWhenInvalid)
        {
            return;
        }

        if (holdingItem == null)
        {
            return;
        }

        bool stillBelongsToPlayer = ContainsItem(holdingItem);

        bool isBeingDragged =
            temporarilyAllowedItem != null &&
            holdingItem == temporarilyAllowedItem;

        if (!stillBelongsToPlayer && !isBeingDragged)
        {
            ClearHoldingItem();
        }
    }

    public bool TryMainUseHoldingItem()
    {
        if (holdingItem == null || holdingItem.ItemData == null)
        {
            return false;
        }

        InventoryItem itemToUse = holdingItem;

        if (!ContainsItem(itemToUse))
        {
            ValidateHoldingItem(null);
            return false;
        }

        // 只有主动道具类型才有具体 MainUse。
        // 其他类型短按 MainUse 直接失败。
        if (itemToUse.ItemData.itemType != ItemType.Active)
        {
            Debug.Log($"{itemToUse.ItemData.itemDisplayName} 不是主动道具，没有 MainUse。");
            return false;
        }

        bool useSucceeded = itemToUse.MainUse(this);

        if (!useSucceeded)
        {
            return false;
        }

        ActiveItemDataSO activeItemData = itemToUse.ItemData as ActiveItemDataSO;

        bool isConsumable =
            activeItemData != null &&
            activeItemData.isConsumable;

        if (isConsumable)
        {
            ConsumeItemFromPlayerInventory(itemToUse);
        }

        return true;
    }

    public bool TrySecondaryUseHoldingItem()
    {
        if (holdingItem == null || holdingItem.ItemData == null)
        {
            return false;
        }

        InventoryItem itemToUse = holdingItem;

        if (!ContainsItem(itemToUse))
        {
            ValidateHoldingItem(null);
            return false;
        }

        bool useSucceeded = itemToUse.SecondaryUse(this);

        if (!useSucceeded)
        {
            return false;
        }

        // 之后当 SecondaryUse 真正实现“丢出可拾取实体”并返回 true 时，
        // 这里就把物品从玩家背包里移除。
        RemoveItemFromPlayerAfterSecondaryUse(itemToUse);

        return true;
    }

    private void ConsumeItemFromPlayerInventory(InventoryItem itemToConsume)
    {
        if (itemToConsume == null)
        {
            return;
        }

        if (holdingItem == itemToConsume)
        {
            ClearHoldingItem();
        }

        ClearQuickItem(itemToConsume);

        RemoveItem(itemToConsume);

        ValidateQuickItems(null);
        ValidateHoldingItem(null);
    }

    private void RemoveItemFromPlayerAfterSecondaryUse(InventoryItem itemToRemove)
    {
        if (itemToRemove == null)
        {
            return;
        }

        if (holdingItem == itemToRemove)
        {
            ClearHoldingItem();
        }

        ClearQuickItem(itemToRemove);

        RemoveItem(itemToRemove);

        ValidateQuickItems(null);
        ValidateHoldingItem(null);
    }

    public void NotifyHoldingItemConsumed(InventoryItem consumedItem)
    {
        if (consumedItem == null)
        {
            return;
        }

        if (holdingItem == consumedItem)
        {
            ClearHoldingItem();
        }

        ClearQuickItem(consumedItem);
        ValidateQuickItems(null);
    }

    public bool ClearQuickItem(InventoryItem itemToClear)
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();

        bool changed = ClearQuickItemInternal(itemToClear);

        if (changed)
        {
            onQuickItemsChange?.Invoke();
        }

        return changed;
    }

    public GameObject DropItem(ItemDataSO itemToDrop)
    {
        bool facingRight = GetComponent<PlayerControl>().facingDir > 0 ? true : false;
        GameObject itemDropped = Instantiate(
        pickableObject.gameObject,
        itemDropPosition.position,
        Quaternion.identity
    );
        itemDropped.GetComponent<PickableObject>()
                   .SetupObject(itemToDrop, facingRight);
        return itemDropped;
    }

    public void AddMoney(int amount)
    {
        if(amount>=0)
        {
            money += amount;
            onMoneyChanged?.Invoke(money);
            onMoneyAdd?.Invoke(amount);
        }
    }

    public void ReduceMoney(int amount)
    {
        if(amount>=0)
        {
            money -= amount;
            if(money < 0)
            {
                money = 0;
            }
            onMoneyChanged?.Invoke(money);
        }
    }

    public bool MoneyCanAfford(int amount)
    {
        return money >=amount;
    }
    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
        onMoneyChanged?.Invoke(money);
    }
    public override void ClearInventoryItems()
    {
        ClearHoldingItem();

        for (int i = 0; i < quickItemSlotList.Count; i++)
        {
            if (quickItemSlotList[i] != null)
            {
                quickItemSlotList[i].Clear();
            }
        }

        for (int i = inventoryItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = inventoryItems[i];

            if (item != null)
            {
                OnItemRemoved(item);
            }
        }

        ClearAllSlotsOnly();
        inventoryItems.Clear();

        onQuickItemsChange?.Invoke();
        TriggerInventoryChanged();
    }

    private Dictionary<string, InventoryItem> BuildRuntimeItemMap()
    {
        Dictionary<string, InventoryItem> result = new Dictionary<string, InventoryItem>();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            InventoryItem item = inventoryItems[i];

            if (item == null || item.ItemData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(item.runtimeSaveID))
            {
                continue;
            }

            result[item.runtimeSaveID] = item;
        }

        return result;
    }

    public override void SaveData()
    {
        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return;
        }

        runData.EnsureDataValid();

        PlayerInventorySaveData playerSaveData = runData.playerInventorySaveData;
        playerSaveData.EnsureDataValid();

        playerSaveData.inventorySaveData = CreateInventorySaveData();
        playerSaveData.inventorySaveData.inventorySaveID = "playerInventory";

        playerSaveData.money = money;

        playerSaveData.quickItemRuntimeIDs.Clear();

        for (int i = 0; i < quickItemSlotList.Count; i++)
        {
            InventoryItem quickItem = GetQuickItem(i);

            if (quickItem != null && quickItem.ItemData != null)
            {
                playerSaveData.quickItemRuntimeIDs.Add(quickItem.runtimeSaveID);
            }
            else
            {
                playerSaveData.quickItemRuntimeIDs.Add("");
            }
        }

        if (holdingItem != null && holdingItem.ItemData != null)
        {
            playerSaveData.holdingItemRuntimeID = holdingItem.runtimeSaveID;
        }
        else
        {
            playerSaveData.holdingItemRuntimeID = "";
        }
        SaveManager.Instance.SaveGame();
    }

    public override void LoadData()
    {
        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return;
        }

        runData.EnsureDataValid();

        PlayerInventorySaveData playerSaveData = runData.playerInventorySaveData;

        if (playerSaveData == null)
        {
            ClearInventoryItems();
            SetMoney(0);
            return;
        }

        playerSaveData.EnsureDataValid();

        ClearHoldingItem();

        LoadFromInventorySaveData(playerSaveData.inventorySaveData);

        SetMoney(playerSaveData.money);

        Dictionary<string, InventoryItem> itemMap = BuildRuntimeItemMap();

        EnsureSlotListSize();

        for (int i = 0; i < quickItemSlotList.Count; i++)
        {
            quickItemSlotList[i].Clear();
        }

        for (int i = 0; i < playerSaveData.quickItemRuntimeIDs.Count && i < quickItemSlotList.Count; i++)
        {
            string runtimeID = playerSaveData.quickItemRuntimeIDs[i];

            if (string.IsNullOrEmpty(runtimeID))
            {
                continue;
            }

            if (itemMap.TryGetValue(runtimeID, out InventoryItem item))
            {
                quickItemSlotList[i].itemInSlot = item;
            }
        }

        if (!string.IsNullOrEmpty(playerSaveData.holdingItemRuntimeID))
        {
            if (itemMap.TryGetValue(playerSaveData.holdingItemRuntimeID, out InventoryItem holdingItemToLoad))
            {
                SetHoldingItem(holdingItemToLoad);
            }
        }
        else
        {
            ClearHoldingItem();
        }

        ValidateQuickItems(null);
        ValidateHoldingItem(null);

        onQuickItemsChange?.Invoke();
        TriggerInventoryChanged();
    }

    public void SaveCurrentItemsToRetrieveInventoryAndClearSelf()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("保存遗失物品失败：SaveManager.Instance 为空。");
            return;
        }

        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();

        if (runData == null)
        {
            Debug.LogWarning("保存遗失物品失败：当前没有 GameRunData。");
            return;
        }

        runData.EnsureDataValid();

        InventorySaveData retrieveSaveData = CreateInventorySaveData();
        retrieveSaveData.inventorySaveID = retrieveInventorySaveID;

        // 直接覆盖最新一份遗失物品
        runData.inventorySaveDataMap[retrieveInventorySaveID] = retrieveSaveData;

        // 只清物品、快捷栏、手持，不清钱
        ClearInventoryItems();
        SaveData();
        SaveManager.Instance.SaveGame();
    }
}