using UnityEngine;

public class LootArea_WithCertainItems : LootArea
{
    [Header("Certain Items")]
    [SerializeField] private ItemDataSO[] certainItems;

    /// <summary>
    /// 固定物品优先生成。
    /// 
    /// 这个子类不要求一开始就必须有 itemDataBase。
    /// 因为如果理论生成数量全部被固定物品占满，
    /// 那就根本不会用到随机物品数据库。
    /// </summary>
    protected override bool NeedRandomItemDataBaseBeforeGenerate
    {
        get
        {
            return false;
        }
    }

    protected override void GenerateLootItems(
        int generateCount,
        int bonusLuck)
    {
        int currentGeneratedCount = 0;

        GenerateCertainItemsFirst(
            generateCount,
            ref currentGeneratedCount);

        int remainingGenerateCount = generateCount - currentGeneratedCount;

        if (remainingGenerateCount <= 0)
        {
            return;
        }

        GenerateRandomItems(
            remainingGenerateCount,
            bonusLuck);
    }

    private void GenerateCertainItemsFirst(
        int generateCount,
        ref int currentGeneratedCount)
    {
        if (certainItems == null || certainItems.Length == 0)
        {
            return;
        }

        for (int i = 0; i < certainItems.Length; i++)
        {
            if (currentGeneratedCount >= generateCount)
            {
                return;
            }

            ItemDataSO certainItem = certainItems[i];

            if (certainItem == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name} 第 {i + 1} 个固定物品为空，已跳过。");

                continue;
            }

            // 没有背包形状的数据，AddItem 肯定放不进去，所以提前跳过
            if (certainItem.backpackItemData == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name} 固定物品 {certainItem.name} 没有 backpackItemData，已跳过。");

                continue;
            }

            bool added = inventory.AddItem(certainItem);

            if (added)
            {
                currentGeneratedCount++;
            }
            else
            {
                Debug.Log(
                    $"{gameObject.name} 固定物品 {certainItem.name} 生成失败，可能是背包空间不足。");
            }
        }
    }

    private void GenerateRandomItems(
        int randomGenerateCount,
        int bonusLuck)
    {
        if (!HasValidRandomItemDatabase())
        {
            LogInvalidRandomItemDatabaseWarning();
            return;
        }

        for (int i = 0; i < randomGenerateCount; i++)
        {
            bool success = TryGenerateOneItem(bonusLuck);

            if (!success)
            {
                Debug.Log(
                    $"{gameObject.name} 第 {i + 1} 个随机补充物品生成失败，可能是没有可用物品或背包空间不足。");
            }
        }
    }
}