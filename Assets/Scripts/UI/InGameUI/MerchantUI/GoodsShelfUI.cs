using System;
using System.Collections.Generic;
using UnityEngine;

public class GoodsShelfUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform contentRoot; // Scroll View / Viewport / Content
    [SerializeField] private MerchantShelfRow shelfPrefab;
    [SerializeField] private Merchandise merchandisePrefab;
    private MerchantUnlockManager merchantUnlockManager;

    [Header("Shelf Settings")]
    [SerializeField] private int merchandiseCountPerShelf = 3;

    private InventoryPlayer playerInventory;
    private DraggedItemUI draggedItemUI;

    private readonly List<MerchantShelfRow> spawnedShelves = new List<MerchantShelfRow>();

    private void Awake()
    {
        if (draggedItemUI == null)
        {
            InGameUI inGameUI = GetComponentInParent<InGameUI>();

            if (inGameUI != null)
            {
                draggedItemUI = inGameUI.draggedItemUI;
            }
        }

        merchantUnlockManager = MerchantUnlockManager.Instance;

    }

    private void OnEnable()
    {
        if (merchantUnlockManager == null)
        {
            merchantUnlockManager = MerchantUnlockManager.Instance;
        }

        if (merchantUnlockManager != null)
        {
            merchantUnlockManager.onMerchantItemsChanged += RefreshShelves;
        }
    }

    private void OnDisable()
    {
        if (merchantUnlockManager != null)
        {
            merchantUnlockManager.onMerchantItemsChanged -= RefreshShelves;
        }
    }

    public void SetInventory(InventoryPlayer playerInventory)
    {
        this.playerInventory = playerInventory;

        if (this.playerInventory == null)
        {
            ClearShelves();
            return;
        }

        RefreshShelves();
    }

    public void RefreshShelves()
    {
        ClearShelves();

        if (playerInventory == null)
        {
            return;
        }

        if (contentRoot == null || shelfPrefab == null || merchandisePrefab == null)
        {
            Debug.LogWarning("GoodsShelfUI 刷新失败：ContentRoot / ShelfPrefab / MerchandisePrefab 没有配置完整。");
            return;
        }

        if (merchantUnlockManager == null)
        {
            merchantUnlockManager = MerchantUnlockManager.Instance;
        }

        if (merchantUnlockManager == null)
        {
            Debug.LogWarning("GoodsShelfUI 刷新失败：场景中没有 MerchantUnlockManager。");
            return;
        }

        List<ItemDataSO> unlockedItems = merchantUnlockManager.GetUnlockedMerchantItemsSorted();

        if (unlockedItems.Count <= 0)
        {
            return;
        }

        int safeCountPerShelf = Mathf.Max(1, merchandiseCountPerShelf);
        int shelfCount = Mathf.CeilToInt(unlockedItems.Count / (float)safeCountPerShelf);

        int itemIndex = 0;

        for (int shelfIndex = 0; shelfIndex < shelfCount; shelfIndex++)
        {
            MerchantShelfRow shelf = Instantiate(shelfPrefab, contentRoot);
            shelf.Clear();

            spawnedShelves.Add(shelf);

            for (int localIndex = 0; localIndex < safeCountPerShelf; localIndex++)
            {
                if (itemIndex >= unlockedItems.Count)
                {
                    break;
                }

                ItemDataSO itemData = unlockedItems[itemIndex];

                Merchandise merchandise = Instantiate(merchandisePrefab, shelf.MerchandiseRoot);
                merchandise.SetItemData(itemData);

                itemIndex++;
            }
        }
    }

    public void TryBuyItem(ItemDataSO itemToBuy)
    {
        if (itemToBuy == null)
        {
            return;
        }

        if (playerInventory == null || draggedItemUI == null)
        {
            return;
        }

        // 正在拖东西时不允许购买，避免一个鼠标上挂两个物品
        if (draggedItemUI.IsDragging)
        {
            return;
        }

        if (merchantUnlockManager != null && !merchantUnlockManager.IsUnlocked(itemToBuy))
        {
            return;
        }

        if (!playerInventory.MoneyCanAfford(itemToBuy.itemBuyPrice))
        {
            return;
        }

        playerInventory.ReduceMoney(itemToBuy.itemBuyPrice);

        // 商店购买出来的物品不直接进背包，而是立刻变成拖拽状态
        InventoryItem item = new InventoryItem(itemToBuy);
        draggedItemUI.BeginDrag(item);
    }

    public void TrySellItem()
    {
        if (playerInventory == null || draggedItemUI == null)
        {
            return;
        }

        if (!draggedItemUI.IsDragging || draggedItemUI.draggedItem == null)
        {
            return;
        }

        InventoryItem itemToSell = draggedItemUI.draggedItem;

        if (itemToSell.ItemData == null)
        {
            return;
        }

        ItemDataSO soldItemData = itemToSell.ItemData;

        playerInventory.AddMoney(soldItemData.itemSellPrice);

        //// 如果这个拖拽物本来还在玩家背包里，就把它从背包里清掉。
        //// 如果它是刚从商店买出来的，还没进背包，RemoveItem 会自然失败，不影响结果。
        //playerInventory.ClearQuickItem(itemToSell);
        //playerInventory.RemoveItem(itemToSell);
        //playerInventory.ValidateQuickItems(null);
        //playerInventory.ValidateHoldingItem(null);

        draggedItemUI.EndDrag();

        if (merchantUnlockManager == null)
        {
            merchantUnlockManager = MerchantUnlockManager.Instance;
        }

        if (merchantUnlockManager != null)
        {
            merchantUnlockManager.NotifyItemSold(soldItemData);
        }
    }

    private void ClearShelves()
    {
        for (int i = spawnedShelves.Count - 1; i >= 0; i--)
        {
            if (spawnedShelves[i] != null)
            {
                Destroy(spawnedShelves[i].gameObject);
            }
        }

        spawnedShelves.Clear();

        if (contentRoot == null)
        {
            return;
        }

        // 手动残留了旧货架，也一起清掉
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }
}