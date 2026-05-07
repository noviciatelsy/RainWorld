using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryBase : MonoBehaviour
{
    public event Action onInventoryChange; // 物品改变事件

    [Header("尺寸")]
    [Min(1)] public int columnCount = 7; // 每行多少列
    [Min(1)] public int maxInventorySize = 56; // 容量

    [Header("自动塞入设置")]
    [SerializeField] private bool preferTighterPlacement = true; // 是否优先选择更紧密的位置

    public List<InventoryItemSlot> itemSlotList = new List<InventoryItemSlot>(); // 物品槽位列表
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    public ItemListDataSO itemDataBase; // 全物品SO

    public int ColumnCount
    {
        get
        {
            return Mathf.Max(1, columnCount);
        }
    }

    public int RowCount
    {
        get
        {
            return Mathf.CeilToInt(maxInventorySize / (float)ColumnCount);
        }
    }

    protected virtual void Awake()
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();
    }


    protected virtual void EnsureSlotListSize()
    {
        if (maxInventorySize < 1)
        {
            maxInventorySize = 1;
        }

        if (columnCount < 1)
        {
            columnCount = 1;
        }

        if (itemSlotList == null)
        {
            itemSlotList = new List<InventoryItemSlot>();
        }

        while (itemSlotList.Count < maxInventorySize)
        {
            itemSlotList.Add(new InventoryItemSlot()); // 补空槽
        }

        if (itemSlotList.Count > maxInventorySize)
        {
            itemSlotList.RemoveRange(maxInventorySize, itemSlotList.Count - maxInventorySize);
        }


    }

    public bool AddItem(ItemDataSO itemData)
    {
        EnsureSlotListSize();
        SanitizeEmptyItemShells();
        if (itemData == null)
        {
            Debug.LogWarning("AddItem 失败：itemData 为空。");
            return false;
        }

        if (itemData.backpackItemData == null)
        {
            Debug.LogWarning($"AddItem 失败：{itemData.name} 没有配置 backpackItemData。");
            return false;
        }

        BackpackItemDataSO backpackItemData = itemData.backpackItemData;


        InventoryItem newItem = new InventoryItem(itemData);

        if (!TryFindBestPlacement(newItem, out Vector2Int bestTopLeft, out ItemRotateState bestRotateState))
        {
            Debug.Log($"背包没有足够空间放入：{itemData.itemDisplayName}");
            return false;
        }

        return PlaceItem(newItem, bestTopLeft, bestRotateState);
    }


    public InventoryItem GetItemAtSlot(int slotIndex)
    {
        if (!IsValidIndex(slotIndex))
        {
            return null;
        }

        InventoryItemSlot slot = itemSlotList[slotIndex];

        if (slot == null)
        {
            return null;
        }

        if (!slot.HasItem())
        {
            // 清掉 Unity 序列化或测试遗留出来的空壳 InventoryItem
            slot.itemInSlot = null;
            return null;
        }

        return slot.itemInSlot;
    }

    public bool PlaceItem(InventoryItem item, Vector2Int topLeft, ItemRotateState rotateState)
    {
        if (item == null || item.ItemData == null || item.ItemData.backpackItemData == null)
        {
            return false;
        }

        if (!CanPlaceItem(item, topLeft, rotateState, out List<int> targetIndices, out HashSet<InventoryItem> conflictItems))
        {
            return false;
        }

        // 如果这个物品之前已经在当前背包里，先清掉旧占用，避免重复占格
        ClearSlotsContaining(item);

        item.rotateState = rotateState;

        for (int i = 0; i < targetIndices.Count; i++)
        {
            itemSlotList[targetIndices[i]].itemInSlot = item;
        }

        if (!inventoryItems.Contains(item))
        {
            inventoryItems.Add(item);
            OnItemPlaced(item);
        }

        onInventoryChange?.Invoke();
        return true;
    }

    public bool TryPlaceWithSingleReplacement(
        InventoryItem item,
        Vector2Int topLeft,
        ItemRotateState rotateState,
        out InventoryItem replacedItem
    )
    {
        replacedItem = null;

        if (item == null)
        {
            return false;
        }

        if (!TryGetPlacementInfo(item, topLeft, rotateState, out List<int> targetIndices, out HashSet<InventoryItem> conflictItems))
        {
            return false;
        }

        if (conflictItems.Count > 1)
        {
            return false;
        }

        if (conflictItems.Count == 0)
        {
            return PlaceItem(item, topLeft, rotateState);
        }

        foreach (InventoryItem conflictItem in conflictItems)
        {
            replacedItem = conflictItem;
            break;
        }

        if (replacedItem == null)
        {
            return false;
        }

        ItemRotateState oldRotateState = replacedItem.rotateState;
        Vector2Int oldTopLeft = Vector2Int.zero;
        bool hasOldTopLeft = TryGetTopLeftOfItem(replacedItem, out oldTopLeft);

        RemoveItem(replacedItem);

        bool placed = PlaceItem(item, topLeft, rotateState);

        if (!placed)
        {
            // 理论上不会发生，但防御一下，免得物品凭空消失
            if (hasOldTopLeft)
            {
                PlaceItem(replacedItem, oldTopLeft, oldRotateState);
            }

            replacedItem = null;
            return false;
        }

        return true;
    }

    public bool RemoveItem(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        bool clearedAnySlot = ClearSlotsContaining(item);
        bool removedFromList = inventoryItems.Remove(item);

        if (clearedAnySlot || removedFromList)
        {
            OnItemRemoved(item);
            onInventoryChange?.Invoke();
            return true;
        }

        return false;
    }

    public bool TryGetPlacementInfo(
       InventoryItem item,
       Vector2Int topLeft,
       ItemRotateState rotateState,
       out List<int> targetIndices,
       out HashSet<InventoryItem> conflictItems
   )
    {
        targetIndices = new List<int>();
        conflictItems = new HashSet<InventoryItem>();

        if (item == null || item.ItemData == null || item.ItemData.backpackItemData == null)
        {
            return false;
        }

        SanitizeEmptyItemShells();

        if (!TryGetTargetIndices(item.ItemData.backpackItemData, topLeft, rotateState, out targetIndices))
        {
            return false;
        }

        for (int i = 0; i < targetIndices.Count; i++)
        {
            int targetIndex = targetIndices[i];

            if (!IsValidIndex(targetIndex))
            {
                return false;
            }

            InventoryItemSlot slot = itemSlotList[targetIndex];

            if (slot == null)
            {
                itemSlotList[targetIndex] = new InventoryItemSlot();
                continue;
            }

            // 只有 HasItem 为 true 的格子才是真的被占用。
            // itemInSlot != null 但 ItemData == null 的空壳物品，直接清掉。
            if (!slot.HasItem())
            {
                slot.itemInSlot = null;
                continue;
            }

            InventoryItem existingItem = slot.itemInSlot;

            if (existingItem != item)
            {
                conflictItems.Add(existingItem);
            }
        }

        return true;
    }

    public bool CanPlaceItem(
        InventoryItem item,
        Vector2Int topLeft,
        ItemRotateState rotateState,
        out List<int> targetIndices,
        out HashSet<InventoryItem> conflictItems
    )
    {
        if (!TryGetPlacementInfo(item, topLeft, rotateState, out targetIndices, out conflictItems))
        {
            return false;
        }

        return conflictItems.Count == 0;
    }

    public bool TryGetTopLeftOfItem(InventoryItem item, out Vector2Int topLeft)
    {
        topLeft = Vector2Int.zero;

        if (item == null)
        {
            return false;
        }

        bool found = false;
        int minColumn = int.MaxValue;
        int minRow = int.MaxValue;

        for (int i = 0; i < itemSlotList.Count; i++)
        {
            if (itemSlotList[i].itemInSlot != item)
            {
                continue;
            }

            Vector2Int coord = IndexToCoord(i);

            if (coord.x < minColumn)
            {
                minColumn = coord.x;
            }

            if (coord.y < minRow)
            {
                minRow = coord.y;
            }

            found = true;
        }

        if (!found)
        {
            return false;
        }

        topLeft = new Vector2Int(minColumn, minRow);
        return true;
    }

    public List<int> GetSlotIndicesOfItem(InventoryItem item)
    {
        List<int> result = new List<int>();

        if (item == null)
        {
            return result;
        }

        for (int i = 0; i < itemSlotList.Count; i++)
        {
            if (itemSlotList[i].itemInSlot == item)
            {
                result.Add(i);
            }
        }

        return result;
    }

    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < itemSlotList.Count;
    }

    public Vector2Int IndexToCoord(int index)
    {
        int column = index % ColumnCount;
        int row = index / ColumnCount;

        return new Vector2Int(column, row);
    }

    public int CoordToIndex(int column, int row)
    {
        return row * ColumnCount + column;
    }


    private bool TryFindBestPlacement(
       InventoryItem item,
       out Vector2Int bestTopLeft,
       out ItemRotateState bestRotateState
   )
    {
        bestTopLeft = Vector2Int.zero;
        bestRotateState = ItemRotateState.Rotate0;

        if (item == null || item.ItemData == null || item.ItemData.backpackItemData == null)
        {
            return false;
        }

        SanitizeEmptyItemShells();

        bool found = false;
        int bestScore = int.MinValue;
        int bestRow = int.MaxValue;
        int bestColumn = int.MaxValue;
        int bestRotationOrder = int.MaxValue;

        ItemRotateState[] rotateStates =
        {
        ItemRotateState.Rotate0,
        ItemRotateState.Rotate90,
        ItemRotateState.Rotate180,
        ItemRotateState.Rotate270
    };

        for (int rotationIndex = 0; rotationIndex < rotateStates.Length; rotationIndex++)
        {
            ItemRotateState rotateState = rotateStates[rotationIndex];
            Vector2Int rotatedSize = BackpackItemShapeUtility.GetRotatedImageSize(item.ItemData.backpackItemData, rotateState);

            if (rotatedSize.x > ColumnCount || rotatedSize.y > RowCount)
            {
                continue;
            }

            for (int row = 0; row <= RowCount - rotatedSize.y; row++)
            {
                for (int column = 0; column <= ColumnCount - rotatedSize.x; column++)
                {
                    Vector2Int topLeft = new Vector2Int(column, row);

                    if (!CanPlaceItem(item, topLeft, rotateState, out List<int> targetIndices, out HashSet<InventoryItem> conflictItems))
                    {
                        continue;
                    }

                    if (!preferTighterPlacement)
                    {
                        bestTopLeft = topLeft;
                        bestRotateState = rotateState;
                        return true;
                    }

                    int score = CalculateTightnessScore(targetIndices);

                    bool better =
                        !found ||
                        score > bestScore ||
                        score == bestScore && row < bestRow ||
                        score == bestScore && row == bestRow && column < bestColumn ||
                        score == bestScore && row == bestRow && column == bestColumn && rotationIndex < bestRotationOrder;

                    if (better)
                    {
                        found = true;
                        bestScore = score;
                        bestRow = row;
                        bestColumn = column;
                        bestRotationOrder = rotationIndex;
                        bestTopLeft = topLeft;
                        bestRotateState = rotateState;
                    }
                }
            }
        }

        return found;
    }

    private int CalculateTightnessScore(List<int> targetIndices)
    {
        int score = 0;
        HashSet<int> placingIndices = new HashSet<int>(targetIndices);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        for (int i = 0; i < targetIndices.Count; i++)
        {
            Vector2Int coord = IndexToCoord(targetIndices[i]);

            for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                Vector2Int neighbor = coord + directions[directionIndex];

                bool outside =
                    neighbor.x < 0 ||
                    neighbor.x >= ColumnCount ||
                    neighbor.y < 0 ||
                    neighbor.y >= RowCount;

                if (outside)
                {
                    // 靠边也算更紧凑一点
                    score += 1;
                    continue;
                }

                int neighborIndex = CoordToIndex(neighbor.x, neighbor.y);

                if (placingIndices.Contains(neighborIndex))
                {
                    continue;
                }

                InventoryItemSlot neighborSlot = itemSlotList[neighborIndex];

                if (neighborSlot != null && neighborSlot.HasItem())
                {
                    // 靠已有物品更紧凑
                    score += 3;
                }
                else if (neighborSlot != null)
                {
                    neighborSlot.itemInSlot = null;
                }
            }
        }

        return score;
    }

    private bool TryGetTargetIndices(
    BackpackItemDataSO backpackItemData,
    Vector2Int topLeft,
    ItemRotateState rotateState,
    out List<int> targetIndices
)
    {
        targetIndices = new List<int>();

        if (backpackItemData == null)
        {
            Debug.LogWarning("TryGetTargetIndices 失败：backpackItemData 为空。");
            return false;
        }

        Vector2Int rotatedSize = BackpackItemShapeUtility.GetRotatedImageSize(backpackItemData, rotateState);

        if (rotatedSize.x <= 0 || rotatedSize.y <= 0)
        {
            Debug.LogWarning($"TryGetTargetIndices 失败：rotatedSize 不合法：{rotatedSize}");
            return false;
        }

        if (topLeft.x < 0 || topLeft.y < 0)
        {
            return false;
        }

        if (topLeft.x + rotatedSize.x > ColumnCount)
        {
            return false;
        }

        if (topLeft.y + rotatedSize.y > RowCount)
        {
            return false;
        }

        List<Vector2Int> rotatedOccupationArea = BackpackItemShapeUtility.GetRotatedOccupationArea(backpackItemData, rotateState);

        if (rotatedOccupationArea == null || rotatedOccupationArea.Count == 0)
        {
            Debug.LogWarning(
                $"TryGetTargetIndices 失败：旋转后的占用格为空。\n" +
                $"BackpackItemData：{backpackItemData.name}\n" +
                $"rotateState：{rotateState}\n" +
                $"原 occupationArea.Length：{(backpackItemData.occupationArea == null ? 0 : backpackItemData.occupationArea.Length)}"
            );

            return false;
        }

        for (int i = 0; i < rotatedOccupationArea.Count; i++)
        {
            Vector2Int localCell = rotatedOccupationArea[i];

            int column = topLeft.x + localCell.x;

            // occupationArea 是左下角坐标系，UI 背包是从上往下排，所以这里要翻转 y
            int row = topLeft.y + (rotatedSize.y - 1 - localCell.y);

            if (column < 0 || column >= ColumnCount || row < 0 || row >= RowCount)
            {
                return false;
            }

            int index = CoordToIndex(column, row);

            if (index < 0 || index >= itemSlotList.Count)
            {
                Debug.LogWarning(
                    $"TryGetTargetIndices 失败：计算出来的 index 超出 itemSlotList。\n" +
                    $"column：{column}\n" +
                    $"row：{row}\n" +
                    $"index：{index}\n" +
                    $"itemSlotList.Count：{itemSlotList.Count}\n" +
                    $"maxInventorySize：{maxInventorySize}"
                );

                return false;
            }

            if (!targetIndices.Contains(index))
            {
                targetIndices.Add(index);
            }
        }

        return targetIndices.Count > 0;
    }

    private bool ClearSlotsContaining(InventoryItem item)
    {
        bool changed = false;

        for (int i = 0; i < itemSlotList.Count; i++)
        {
            if (itemSlotList[i].itemInSlot == item)
            {
                itemSlotList[i].itemInSlot = null;
                changed = true;
            }
        }

        return changed;
    }

    protected virtual void SanitizeEmptyItemShells()
    {
        if (itemSlotList == null)
        {
            return;
        }

        for (int i = 0; i < itemSlotList.Count; i++)
        {
            if (itemSlotList[i] == null)
            {
                itemSlotList[i] = new InventoryItemSlot();
                continue;
            }

            itemSlotList[i].ClearIfInvalid();
        }

        if (inventoryItems == null)
        {
            inventoryItems = new List<InventoryItem>();
            return;
        }

        for (int i = inventoryItems.Count - 1; i >= 0; i--)
        {
            if (inventoryItems[i] == null || inventoryItems[i].ItemData == null)
            {
                inventoryItems.RemoveAt(i);
            }
        }
    }

    protected virtual void OnItemPlaced(InventoryItem item)
    {

    }

    protected virtual void OnItemRemoved(InventoryItem item)
    {

    }
}