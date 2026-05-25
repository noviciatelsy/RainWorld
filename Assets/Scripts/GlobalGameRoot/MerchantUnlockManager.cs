using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MerchantUnlockManager : MonoBehaviour
{
    public static MerchantUnlockManager Instance { get; private set; }

    public event Action onMerchantItemsChanged;

    [Header("Database")]
    [SerializeField] private ItemListDataSO itemDataBase;

    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        GameData gameData = GetGameData();

        if (gameData == null || itemDataBase == null || itemDataBase.itemList == null)
        {
            return;
        }

        bool changed = false;

        for (int i = 0; i < itemDataBase.itemList.Length; i++)
        {
            ItemDataSO itemData = itemDataBase.itemList[i];

            if (!IsValidMerchantItem(itemData))
            {
                continue;
            }

            // 给所有可购买体系内的物品准备出售次数字段，避免之后到处判空
            if (!gameData.itemSellAmount.ContainsKey(itemData.saveID))
            {
                gameData.itemSellAmount[itemData.saveID] = 0;
                changed = true;
            }

            // 开局自动解锁的商品，只要有购买资格，就加入已解锁列表
            if (itemData.autoUnlock && !gameData.unlockedMerchantItems.Contains(itemData.saveID))
            {
                gameData.unlockedMerchantItems.Add(itemData.saveID);

                changed = true;
            }
        }

        if (changed)
        {
            SaveManager.Instance.SaveGame();
            onMerchantItemsChanged?.Invoke();
        }
    }

    public bool NotifyItemSold(ItemDataSO soldItemData)
    {
        EnsureInitialized();

        GameData gameData = GetGameData();

        if (gameData == null || soldItemData == null || string.IsNullOrEmpty(soldItemData.saveID))
        {
            return false;
        }

        if (!gameData.itemSellAmount.ContainsKey(soldItemData.saveID))
        {
            gameData.itemSellAmount[soldItemData.saveID] = 0;
        }

        gameData.itemSellAmount[soldItemData.saveID]++;

        bool newlyUnlocked = false;

        // 不能在商店购买的物品，卖多少次都不解锁购买资格
        if (IsValidMerchantItem(soldItemData))
        {
            bool alreadyUnlocked = gameData.unlockedMerchantItems.Contains(soldItemData.saveID);
            int requiredSellAmount = Mathf.Max(1, soldItemData.sellAmountToUnlock);
            int currentSellAmount = gameData.itemSellAmount[soldItemData.saveID];

            if (!alreadyUnlocked && currentSellAmount >= requiredSellAmount)
            {
                gameData.unlockedMerchantItems.Add(soldItemData.saveID);
                newlyUnlocked = true;
            }
        }

        SaveManager.Instance.SaveGame();

        if (newlyUnlocked)
        {
            onMerchantItemsChanged?.Invoke();
        }

        return newlyUnlocked;
    }

    public List<ItemDataSO> GetUnlockedMerchantItemsSorted()
    {
        EnsureInitialized();

        List<ItemDataSO> result = new List<ItemDataSO>();

        GameData gameData = GetGameData();

        if (gameData == null || itemDataBase == null || itemDataBase.itemList == null)
        {
            return result;
        }

        Dictionary<string, int> unlockOrderMap = new Dictionary<string, int>();

        for (int i = 0; i < gameData.unlockedMerchantItems.Count; i++)
        {
            string saveID = gameData.unlockedMerchantItems[i];

            if (!string.IsNullOrEmpty(saveID) && !unlockOrderMap.ContainsKey(saveID))
            {
                unlockOrderMap.Add(saveID, i);
            }
        }

        result = itemDataBase.itemList
            .Where(item => IsValidMerchantItem(item))
            .Where(item => unlockOrderMap.ContainsKey(item.saveID))
            .OrderBy(item => item.rarity)
            .ThenBy(item => unlockOrderMap[item.saveID])
            .ToList();

        return result;
    }

    public bool IsUnlocked(ItemDataSO itemData)
    {
        EnsureInitialized();

        GameData gameData = GetGameData();

        if (gameData == null || itemData == null || string.IsNullOrEmpty(itemData.saveID))
        {
            return false;
        }

        return gameData.unlockedMerchantItems.Contains(itemData.saveID);
    }

    private bool IsValidMerchantItem(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(itemData.saveID))
        {
            return false;
        }

        if (!itemData.canBuyFromMerchant)
        {
            return false;
        }

        return true;
    }

    private GameData GetGameData()
    {
        if (SaveManager.Instance == null)
        {
            return null;
        }

        return SaveManager.Instance.GetRunTimeGameData();
    }
}