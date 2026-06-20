using System.Collections.Generic;
using UnityEngine;

public class LootArea : PlayerSensorTarget
{
    [Header("Item Database")]
    [SerializeField] protected ItemListDataSO itemDataBase;

    [Header("无法搜刮被搜刮出的特殊物品")]
    [SerializeField] protected ItemListDataSO specialItems;

    [Header("Generate Count")]
    [SerializeField] protected int minGenerateItemCount = 2;
    [SerializeField] protected int maxGenerateItemCount = 4;

    [Header("Rarity Weight")]
    [SerializeField] protected int commonWeight = 50;
    [SerializeField] protected int rareWeight = 30;
    [SerializeField] protected int epicWeight = 15;
    [SerializeField] protected int legendaryWeight = 5;

    [Header("Player Luck")]
    [Tooltip("每 1 点 bonusLuck 带来的稀有度权重成长率。0.03 表示每点幸运约提高 3% 的对应稀有度倍率。")]
    [SerializeField] protected float bonusLuckWeightGrowthPerPoint = 0.03f;

    [Tooltip("参与掉落计算的最大 bonusLuck，防止幸运值过高导致权重膨胀失控。")]
    [SerializeField] protected int maxBonusLuckAffect = 50;

    // 防止之后手动调用生成时重复生成
    protected bool hasGeneratedLoot = false;

    protected InventoryBase inventory;

    protected override void Awake()
    {
        base.Awake();

        inventory = GetComponent<InventoryBase>();
    }

    public override void Interact()
    {
        base.Interact();

        GenerateLoot();
        if (InGameUI.Instance != null)
        {
            InGameUI.Instance.ToggleLootUI(inventory);
        }
    }

    public virtual void GenerateLoot()
    {
        if (!CanStartGenerateLoot())
        {
            return;
        }

        if (NeedRandomItemDataBaseBeforeGenerate && !HasValidRandomItemDatabase())
        {
            LogInvalidRandomItemDatabaseWarning();
            return;
        }

        int bonusLuck;
        int generateCount = GetGenerateCountAndBonusLuck(out bonusLuck);

        GenerateLootItems(generateCount, bonusLuck);

        hasGeneratedLoot = true;
    }

    /// <summary>
    /// 是否在生成开始前就必须检查随机物品数据库。
    /// 
    /// 普通 LootArea 一定需要随机物品数据库。
    /// 但是某些子类可能会优先生成固定物品，
    /// 只有理论生成数量还有剩余时才需要随机物品数据库。
    /// </summary>
    protected virtual bool NeedRandomItemDataBaseBeforeGenerate
    {
        get
        {
            return true;
        }
    }

    protected bool CanStartGenerateLoot()
    {
        if (hasGeneratedLoot)
        {
            return false;
        }

        if (inventory == null)
        {
            inventory = GetComponent<InventoryBase>();
        }

        if (inventory == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} 生成可搜刽物品失败：没有 InventoryBase。");

            return false;
        }

        return true;
    }

    protected int GetGenerateCountAndBonusLuck(out int bonusLuck)
    {
        PlayerLuck playerLuck = GetPlayerLuck();

        int bonusItemLootAmount = 0;
        bonusLuck = 0;

        if (playerLuck != null)
        {
            bonusItemLootAmount = Mathf.Max(0, playerLuck.bonusItemLootAmount);
            bonusLuck = Mathf.Clamp(
                playerLuck.bonusLuck,
                0,
                Mathf.Max(0, maxBonusLuckAffect));
        }

        int safeMinGenerateItemCount = Mathf.Max(0, minGenerateItemCount);
        int safeMaxGenerateItemCount = Mathf.Max(
            safeMinGenerateItemCount,
            maxGenerateItemCount);

        // Unity 的 Random.Range(int, int) 上限不包含，所以要 +1
        int generateCount = Random.Range(
            safeMinGenerateItemCount,
            safeMaxGenerateItemCount + 1);

        // PlayerLuck 中的 bonusItemLootAmount 每 1 点额外增加 1 个实际生成物品
        generateCount += bonusItemLootAmount;

        return Mathf.Max(0, generateCount);
    }

    protected virtual void GenerateLootItems(
        int generateCount,
        int bonusLuck)
    {
        for (int i = 0; i < generateCount; i++)
        {
            bool success = TryGenerateOneItem(bonusLuck);

            if (!success)
            {
                Debug.Log(
                    $"{gameObject.name} 第 {i + 1} 个物品生成失败，可能是没有可用物品或背包空间不足。");
            }
        }
    }

    protected bool HasValidRandomItemDatabase()
    {
        if (itemDataBase == null ||
            itemDataBase.itemList == null ||
            itemDataBase.itemList.Length == 0)
        {
            return false;
        }

        return true;
    }

    protected void LogInvalidRandomItemDatabaseWarning()
    {
        Debug.LogWarning(
            $"{gameObject.name} 生成可搜刽物品失败：itemDataBase 为空或没有物品。");
    }

    protected bool TryGenerateOneItem(int bonusLuck)
    {
        if (!HasValidRandomItemDatabase())
        {
            LogInvalidRandomItemDatabaseWarning();
            return false;
        }

        const int maxTryCount = 20;

        for (int i = 0; i < maxTryCount; i++)
        {
            ItemRarity targetRarity = GetRandomRarityByWeight(bonusLuck);

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

    private ItemRarity GetRandomRarityByWeight(int bonusLuck)
    {
        int safeCommonWeight = Mathf.Max(0, commonWeight);
        int safeRareWeight = Mathf.Max(0, rareWeight);
        int safeEpicWeight = Mathf.Max(0, epicWeight);
        int safeLegendaryWeight = Mathf.Max(0, legendaryWeight);

        float adjustedCommonWeight = safeCommonWeight;
        float adjustedRareWeight = safeRareWeight * GetBonusLuckWeightMultiplier(
            bonusLuck,
            1);

        float adjustedEpicWeight = safeEpicWeight * GetBonusLuckWeightMultiplier(
            bonusLuck,
            2);

        float adjustedLegendaryWeight = safeLegendaryWeight * GetBonusLuckWeightMultiplier(
            bonusLuck,
            3);

        float totalWeight =
            adjustedCommonWeight +
            adjustedRareWeight +
            adjustedEpicWeight +
            adjustedLegendaryWeight;

        if (totalWeight <= 0f)
        {
            return ItemRarity.Common;
        }

        float randomValue = Random.Range(0f, totalWeight);

        if (randomValue < adjustedCommonWeight)
        {
            return ItemRarity.Common;
        }

        randomValue -= adjustedCommonWeight;

        if (randomValue < adjustedRareWeight)
        {
            return ItemRarity.Rare;
        }

        randomValue -= adjustedRareWeight;

        if (randomValue < adjustedEpicWeight)
        {
            return ItemRarity.Epic;
        }

        return ItemRarity.Legendary;
    }

    /// <summary>
    /// 根据 bonusLuck 计算稀有度权重倍率。
    /// 
    /// 稀有度等级：
    /// Common = 0，不受 bonusLuck 影响
    /// Rare = 1
    /// Epic = 2
    /// Legendary = 3
    /// 
    /// 公式：
    /// 实际权重 = 基础权重 * Pow(1 + 每点幸运成长率, bonusLuck * 稀有度等级)
    /// </summary>
    private float GetBonusLuckWeightMultiplier(
        int bonusLuck,
        int rarityLevel)
    {
        if (bonusLuck <= 0 || rarityLevel <= 0)
        {
            return 1f;
        }

        float safeGrowthPerPoint = Mathf.Max(
            0f,
            bonusLuckWeightGrowthPerPoint);

        return Mathf.Pow(
            1f + safeGrowthPerPoint,
            bonusLuck * rarityLevel);
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

    private PlayerLuck GetPlayerLuck()
    {
        Player player = PlayerManager.Instance.TryGetCurrentPlayer();

        if (player != null)
        {
            return player.GetComponentInChildren<PlayerLuck>();
        }

        return null;
    }
}