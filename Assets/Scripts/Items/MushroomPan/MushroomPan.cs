using System.Collections.Generic;
using UnityEngine;

public class MushroomPan : MonoBehaviour
{
    [SerializeField] private ItemDataSO roastedMushroomItemData;

    private InventoryPlayer playerInventory;

    private void Awake()
    {
        playerInventory = GetComponentInParent<InventoryPlayer>();
    }

    public void RoastAllMushroom()
    {
        if (playerInventory == null)
        {
            Debug.LogWarning("蘑菇煎锅使用失败：找不到 InventoryPlayer。");
            return;
        }

        if (roastedMushroomItemData == null)
        {
            Debug.LogWarning("蘑菇煎锅使用失败：没有配置 roastedMushroomItemData。");
            return;
        }

        List<InventoryItem> mushroomsToRoast = GetAllMushroomsInPlayerInventory();

        if (mushroomsToRoast.Count <= 0)
        {
            Debug.Log("背包中没有可以烤的蘑菇。");
            return;
        }

        int removedMushroomCount = 0;

        for (int i = 0; i < mushroomsToRoast.Count; i++)
        {
            InventoryItem mushroomItem = mushroomsToRoast[i];

            if (mushroomItem == null)
            {
                continue;
            }

            // 如果这个蘑菇正被手持，先取消手持
            if (playerInventory.GetHoldingItem() == mushroomItem)
            {
                playerInventory.ClearHoldingItem();
            }

            // 如果这个蘑菇被放在快捷栏里，先清掉快捷栏引用
            playerInventory.ClearQuickItem(mushroomItem);

            bool removed = playerInventory.RemoveItem(mushroomItem);

            if (removed)
            {
                removedMushroomCount++;
            }
        }

        int addedRoastedMushroomCount = 0;

        for (int i = 0; i < removedMushroomCount; i++)
        {
            bool added = playerInventory.AddItem(roastedMushroomItemData);

            if (added)
            {
                addedRoastedMushroomCount++;
            }
            else
            {
                Debug.LogWarning("烤蘑菇添加失败：玩家背包空间不足。");
            }
        }

        playerInventory.ValidateQuickItems(null);
        playerInventory.ValidateHoldingItem(null);

        Debug.Log($"蘑菇煎锅使用完成：移除蘑菇 {removedMushroomCount} 个，添加烤蘑菇 {addedRoastedMushroomCount} 个。");
    }

    private List<InventoryItem> GetAllMushroomsInPlayerInventory()
    {
        List<InventoryItem> result = new List<InventoryItem>();

        if (playerInventory == null)
        {
            return result;
        }

        for (int i = 0; i < playerInventory.inventoryItems.Count; i++)
        {
            InventoryItem item = playerInventory.inventoryItems[i];

            if (!IsRoastableMushroom(item))
            {
                continue;
            }

            result.Add(item);
        }

        return result;
    }

    private bool IsRoastableMushroom(InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        // 防止烤蘑菇本身也被再次当成普通蘑菇烤掉
        if (item.ItemData == roastedMushroomItemData)
        {
            return false;
        }

        if (item.ItemData.itemType != ItemType.Active)
        {
            return false;
        }

        ActiveItemDataSO activeItemData = item.ItemData as ActiveItemDataSO;

        if (activeItemData == null)
        {
            return false;
        }

        return activeItemData.isMushroom;
    }
}