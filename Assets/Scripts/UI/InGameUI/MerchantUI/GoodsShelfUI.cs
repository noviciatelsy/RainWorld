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

    [Header("香气果实")]
    [SerializeField] private ItemDataSO fragrantFruitItemData;
    [SerializeField] private bool canGiftFragrantFruitItself = false; // 是否允许赠品再次随机到香气果实本身

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

        bool isSellingFragrantFruit = IsSameItem(soldItemData, fragrantFruitItemData);

        playerInventory.AddMoney(soldItemData.itemSellPrice);

        if (soldItemData.itemType == ItemType.Note)
        {
            IntelligenceArchiveManager.Instance.UnlockRandomNonImportantIntelligenceByNote();
        }

        draggedItemUI.EndDrag();

        if (merchantUnlockManager == null)
        {
            merchantUnlockManager = MerchantUnlockManager.Instance;
        }

        if (merchantUnlockManager != null)
        {
            merchantUnlockManager.NotifyItemSold(soldItemData);
        }

        if (isSellingFragrantFruit)
        {
            TryGiveRandomUnlockedMerchantItemByFragrantFruit();
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

    private void TryGiveRandomUnlockedMerchantItemByFragrantFruit()
    {
        if (playerInventory == null)
        {
            return;
        }

        if (fragrantFruitItemData == null)
        {
            Debug.LogWarning("香气果实赠品失败：fragrantFruitItemData 没有配置。");
            return;
        }

        if (merchantUnlockManager == null)
        {
            merchantUnlockManager = MerchantUnlockManager.Instance;
        }

        if (merchantUnlockManager == null)
        {
            Debug.LogWarning("香气果实赠品失败：场景中没有 MerchantUnlockManager。");
            return;
        }

        List<ItemDataSO> unlockedItems = merchantUnlockManager.GetUnlockedMerchantItemsSorted();

        RemoveInvalidGiftCandidates(unlockedItems);

        if (unlockedItems.Count <= 0)
        {
            Debug.Log("香气果实赠品失败：当前没有可作为赠品的已解锁商品。");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, unlockedItems.Count);
        ItemDataSO giftItemData = unlockedItems[randomIndex];

        bool added = playerInventory.AddItem(giftItemData);

        if (!added)
        {
            Debug.Log($"香气果实赠品添加失败：玩家背包没有足够空间放入 {giftItemData.itemDisplayName}。");
            return;
        }

        Debug.Log($"香气果实触发赠品：获得 {giftItemData.itemDisplayName}。");
    }

    private void RemoveInvalidGiftCandidates(List<ItemDataSO> giftCandidates)
    {
        if (giftCandidates == null)
        {
            return;
        }

        for (int i = giftCandidates.Count - 1; i >= 0; i--)
        {
            ItemDataSO candidate = giftCandidates[i];

            if (candidate == null)
            {
                giftCandidates.RemoveAt(i);
                continue;
            }

            if (candidate.backpackItemData == null)
            {
                giftCandidates.RemoveAt(i);
                continue;
            }

            if (!canGiftFragrantFruitItself && IsSameItem(candidate, fragrantFruitItemData))
            {
                giftCandidates.RemoveAt(i);
                continue;
            }
        }
    }

    private bool IsSameItem(ItemDataSO itemA, ItemDataSO itemB)
    {
        if (itemA == null || itemB == null)
        {
            return false;
        }

        if (itemA == itemB)
        {
            return true;
        }

        if (string.IsNullOrEmpty(itemA.saveID) || string.IsNullOrEmpty(itemB.saveID))
        {
            return false;
        }

        return itemA.saveID == itemB.saveID;
    }
}