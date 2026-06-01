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

    private GameRunData gameRunData;
    private bool hasInitializedFormSave=false;

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
        hasInitializedFormSave = true;
        SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;
    }

    private void OnEnable()
    {
        if(!hasInitializedFormSave)
        {
            return;
        }
        SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;    
    }

    private void OnDisable()
    {
        SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;
    }

    private void HandleCurrentGameRunDataChanged(int mySlotIndex, GameRunData myRunData)
    {
        gameRunData=myRunData;
        if (gameRunData == null || itemDataBase == null || itemDataBase.itemList == null)
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
            if (!gameRunData.itemSellAmount.ContainsKey(itemData.saveID))
            {
                gameRunData.itemSellAmount[itemData.saveID] = 0;
                changed = true;
            }

            // 开局自动解锁的商品，只要有购买资格，就加入已解锁列表
            if (itemData.autoUnlock && !gameRunData.unlockedMerchantItems.Contains(itemData.saveID))
            {
                gameRunData.unlockedMerchantItems.Add(itemData.saveID);

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

        if (gameRunData == null || soldItemData == null || string.IsNullOrEmpty(soldItemData.saveID))
        {
            return false;
        }

        if (!gameRunData.itemSellAmount.ContainsKey(soldItemData.saveID))
        {
            gameRunData.itemSellAmount[soldItemData.saveID] = 0;
        }

        gameRunData.itemSellAmount[soldItemData.saveID]++;

        bool newlyUnlocked = false;

        // 不能在商店购买的物品，卖多少次都不解锁购买资格
        if (IsValidMerchantItem(soldItemData))
        {
            bool alreadyUnlocked = gameRunData.unlockedMerchantItems.Contains(soldItemData.saveID);
            int requiredSellAmount = Mathf.Max(1, soldItemData.sellAmountToUnlock);
            int currentSellAmount = gameRunData.itemSellAmount[soldItemData.saveID];

            if (!alreadyUnlocked && currentSellAmount >= requiredSellAmount)
            {
                gameRunData.unlockedMerchantItems.Add(soldItemData.saveID);
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
        List<ItemDataSO> result = new List<ItemDataSO>();

        if (gameRunData == null || itemDataBase == null || itemDataBase.itemList == null)
        {
            return result;
        }

        Dictionary<string, int> unlockOrderMap = new Dictionary<string, int>();

        for (int i = 0; i < gameRunData.unlockedMerchantItems.Count; i++)
        {
            string saveID = gameRunData.unlockedMerchantItems[i];

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

        if (gameRunData == null || itemData == null || string.IsNullOrEmpty(itemData.saveID))
        {
            return false;
        }

        return gameRunData.unlockedMerchantItems.Contains(itemData.saveID);
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

}