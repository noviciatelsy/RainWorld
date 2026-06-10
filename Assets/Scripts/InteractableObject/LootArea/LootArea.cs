using System.Collections.Generic;
using UnityEngine;

public class LootArea : PlayerSensorTarget
{
    [Header("Item Database")]
    [SerializeField] private ItemListDataSO itemDataBase;

    [Header("无法搜刮被搜刮出的特殊物品")]
    [SerializeField] private ItemListDataSO specialItems;

    [Header("Generate Count")]
    [SerializeField] private int minGenerateItemCount = 2;
    [SerializeField] private int maxGenerateItemCount = 4;

    [Header("Rarity Weight")]
    [SerializeField] private int commonWeight = 50;
    [SerializeField] private int rareWeight = 30;
    [SerializeField] private int epicWeight = 15;
    [SerializeField] private int legendaryWeight = 5;

    [Header("Generate Settings")]
    [SerializeField] private bool generateOnAwake = true;

    // 防止之后手动调用生成时重复生成
    private bool hasGeneratedLoot = false;

    protected InventoryBase inventory;

    protected override void Awake()
    {
        base.Awake();

        inventory = GetComponent<InventoryBase>();

        if (generateOnAwake)
        {
            GenerateLoot();
        }
    }

    public override void Interact()
    {
        base.Interact();

        if (InGameUI.Instance != null)
        {
            InGameUI.Instance.ToggleLootUI(inventory);
        }
    }

    public virtual void GenerateLoot()
    {
        if (hasGeneratedLoot)
        {
            return;
        }

        if (inventory == null)
        {
            inventory = GetComponent<InventoryBase>();
        }

        if (inventory == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} 生成可搜刽物品失败：没有 InventoryBase。");

            return;
        }

        if (itemDataBase == null ||
            itemDataBase.itemList == null ||
            itemDataBase.itemList.Length == 0)
        {
            Debug.LogWarning(
                $"{gameObject.name} 生成可搜刽物品失败：itemDataBase 为空或没有物品。");

            return;
        }

        minGenerateItemCount = Mathf.Max(0, minGenerateItemCount);
        maxGenerateItemCount = Mathf.Max(
            minGenerateItemCount,
            maxGenerateItemCount);

        // Unity 的 Random.Range(int, int) 上限不包含，所以要 +1
        int generateCount = Random.Range(
            minGenerateItemCount,
            maxGenerateItemCount + 1);

        for (int i = 0; i < generateCount; i++)
        {
            bool success = TryGenerateOneItem();

            if (!success)
            {
                Debug.Log(
                    $"{gameObject.name} 第 {i + 1} 个物品生成失败，可能是没有可用物品或背包空间不足。");
            }
        }

        hasGeneratedLoot = true;
    }

    private bool TryGenerateOneItem()
    {
        const int maxTryCount = 20;

        for (int i = 0; i < maxTryCount; i++)
        {
            ItemRarity targetRarity = GetRandomRarityByWeight();

            ItemDataSO itemData = GetRandomItemByRarity(targetRarity);

            if (itemData == null)
            {
                continue;
            }

            bool added = inventory.AddItem(itemData);

            if (added)
            {
                return true;
            }
        }

        return false;
    }

    private ItemRarity GetRandomRarityByWeight()
    {
        int safeCommonWeight = Mathf.Max(0, commonWeight);
        int safeRareWeight = Mathf.Max(0, rareWeight);
        int safeEpicWeight = Mathf.Max(0, epicWeight);
        int safeLegendaryWeight = Mathf.Max(0, legendaryWeight);

        int totalWeight =
            safeCommonWeight +
            safeRareWeight +
            safeEpicWeight +
            safeLegendaryWeight;

        if (totalWeight <= 0)
        {
            return ItemRarity.Common;
        }

        int randomValue = Random.Range(0, totalWeight);

        if (randomValue < safeCommonWeight)
        {
            return ItemRarity.Common;
        }

        randomValue -= safeCommonWeight;

        if (randomValue < safeRareWeight)
        {
            return ItemRarity.Rare;
        }

        randomValue -= safeRareWeight;

        if (randomValue < safeEpicWeight)
        {
            return ItemRarity.Epic;
        }

        return ItemRarity.Legendary;
    }

    private ItemDataSO GetRandomItemByRarity(ItemRarity rarity)
    {
        List<ItemDataSO> candidates = new List<ItemDataSO>();

        for (int i = 0; i < itemDataBase.itemList.Length; i++)
        {
            ItemDataSO itemData = itemDataBase.itemList[i];

            if (itemData == null)
            {
                continue;
            }

            // 没有背包形状的数据，AddItem 肯定放不进去，所以提前跳过
            if (itemData.backpackItemData == null)
            {
                continue;
            }

            if (itemData.rarity != rarity)
            {
                continue;
            }

            candidates.Add(itemData);
        }

        /*
         * 持续随机选择候选物品。
         *
         * 如果选中了特殊物品，这次随机结果不作数，
         * 将其从本轮候选列表中移除，然后重新随机。
         */
        while (candidates.Count > 0)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            ItemDataSO selectedItem = candidates[randomIndex];

            if (IsSpecialItem(selectedItem))
            {
                candidates.RemoveAt(randomIndex);
                continue;
            }

            return selectedItem;
        }

        /*
         * 当前稀有度下没有普通物品，
         * 或者该稀有度下的物品全部都是特殊物品。
         */
        return null;
    }

    /// <summary>
    /// 判断指定物品是否存在于特殊物品数据库中。
    /// </summary>
    private bool IsSpecialItem(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            return false;
        }

        if (specialItems == null ||
            specialItems.itemList == null ||
            specialItems.itemList.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < specialItems.itemList.Length; i++)
        {
            ItemDataSO specialItem = specialItems.itemList[i];

            /*
             * ItemDataSO 是 ScriptableObject。
             * 这里比较的是两个字段是否引用同一个物品资源。
             */
            if (specialItem == itemData)
            {
                return true;
            }
        }

        return false;
    }
}