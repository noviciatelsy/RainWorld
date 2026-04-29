using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryGridUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image itemImagePrefab;
    [SerializeField] private RectTransform itemImageRoot;
    [SerializeField] private RectTransform slotsRoot;
    private GridLayoutGroup slotsGridLayout;
    private DraggedItemUI draggedItemUI;

    private ItemSlotUI[] itemSlots;
    private InventoryBase inventory;

    private readonly List<Image> itemImageInstances = new List<Image>();
    private PlacementPreview currentPreview;

    public InventoryBase Inventory
    {
        get
        {
            return inventory;
        }
    }

    public bool IsDraggingItem
    {
        get
        {
            return draggedItemUI != null && draggedItemUI.IsDragging;
        }
    }

    private void Awake()
    {
        CacheRefs();
        BindSlots();
    }

    private void OnEnable()
    {
        UpdateItemImages();
    }

    private void OnDisable()
    {
        ClearSlotStates();
        currentPreview = null;
    }

    private void Update()
    {
        if (!IsDraggingItem)
        {
            ClearSlotStates();
            currentPreview = null;
            return;
        }

        RefreshPlacementPreview();
    }

    public void SetInventory(InventoryBase newInventory)
    {
        if (inventory == newInventory)
        {
            return;
        }

        if (inventory != null)
        {
            inventory.onInventoryChange -= UpdateItemImages;
        }

        inventory = newInventory;


        if (inventory != null)
        {
            inventory.onInventoryChange += UpdateItemImages;
        }

        UpdateItemImages();
    }

    public void ClearInventoryBinding()
    {
        SetInventory(null);
    }

    public void OnSlotPointerDown(int slotIndex, PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (inventory == null)
        {
            return;
        }

        if (IsDraggingItem)
        {
            TryPlaceDraggedItemByCurrentPreview();
            return;
        }

        TryBeginDragFromSlot(slotIndex);
    }

    private void TryBeginDragFromSlot(int slotIndex)
    {
        if (draggedItemUI == null)
        {
            Debug.LogWarning("拖拽失败：当前 InventoryGridUI 没有配置 DraggedItemUI。");
            return;
        }

        InventoryItem item = inventory.GetItemAtSlot(slotIndex);

        if (item == null)
        {
            return;
        }

        ItemRotateState oldRotateState = item.rotateState;
        Vector2Int oldTopLeft = Vector2Int.zero;
        bool hasOldTopLeft = inventory.TryGetTopLeftOfItem(item, out oldTopLeft);

        bool removed = inventory.RemoveItem(item);

        if (!removed)
        {
            return;
        }

        draggedItemUI.BeginDrag(item, inventory, hasOldTopLeft, oldTopLeft, oldRotateState);
    }

    private void TryPlaceDraggedItemByCurrentPreview()
    {
        if (currentPreview == null)
        {
            RefreshPlacementPreview();
        }

        if (currentPreview == null)
        {
            return;
        }

        if (draggedItemUI == null || draggedItemUI.draggedItem == null)
        {
            return;
        }

        if (currentPreview.conflictItems.Count > 1)
        {
            return;
        }

        InventoryItem draggingItem = draggedItemUI.draggedItem;

        bool success = inventory.TryPlaceWithSingleReplacement(
            draggingItem,
            currentPreview.topLeft,
            currentPreview.rotateState,
            out InventoryItem replacedItem
        );

        if (!success)
        {
            return;
        }

        if (replacedItem == null)
        {
            draggedItemUI.EndDrag();
        }
        else
        {
            // 单物品交换：
            // 当前拖拽物品放入这个 InventoryBase；
            // 被冲突的物品离开这个 InventoryBase，变成新的拖拽物品。
            draggedItemUI.BeginDrag(replacedItem, inventory);
        }

        ClearSlotStates();
        currentPreview = null;
    }

    private void RefreshPlacementPreview()
    {
        ClearSlotStates();
        currentPreview = null;

        if (inventory == null)
        {
            return;
        }

        if (draggedItemUI == null || !draggedItemUI.IsDragging || draggedItemUI.draggedItem == null)
        {
            return;
        }

        if (!IsMouseInsideThisGrid())
        {
            return;
        }

        if (!TryBuildPlacementPreview(draggedItemUI.draggedItem, out currentPreview))
        {
            return;
        }

        ApplyPlacementPreview(currentPreview);
    }

    private bool TryBuildPlacementPreview(InventoryItem draggingItem, out PlacementPreview preview)
    {
        preview = null;

        if (draggingItem == null || draggingItem.ItemData == null || draggingItem.ItemData.backpackItemData == null)
        {
            return false;
        }

        BackpackItemDataSO backpackItemData = draggingItem.ItemData.backpackItemData;
        Vector2Int rotatedSize = BackpackItemShapeUtility.GetRotatedImageSize(backpackItemData, draggingItem.rotateState);

        if (!TryGetNearestTopLeftByMouse(rotatedSize, out Vector2Int topLeft))
        {
            return false;
        }

        if (!inventory.TryGetPlacementInfo(
            draggingItem,
            topLeft,
            draggingItem.rotateState,
            out List<int> targetIndices,
            out HashSet<InventoryItem> conflictItems))
        {
            return false;
        }

        preview = new PlacementPreview();
        preview.topLeft = topLeft;
        preview.rotateState = draggingItem.rotateState;
        preview.targetIndices = targetIndices;
        preview.conflictItems = conflictItems;

        return true;
    }

    private void ApplyPlacementPreview(PlacementPreview preview)
    {
        if (preview == null)
        {
            return;
        }

        for (int i = 0; i < preview.targetIndices.Count; i++)
        {
            int index = preview.targetIndices[i];

            if (index < 0 || index >= itemSlots.Length)
            {
                continue;
            }

            InventoryItem existingItem = inventory.GetItemAtSlot(index);
            SlotState state = SlotState.EnablePlace;

            if (existingItem != null && existingItem != draggedItemUI.draggedItem)
            {
                state = preview.conflictItems.Count == 1
                    ? SlotState.EnableReplace
                    : SlotState.DisablePlace;
            }

            itemSlots[index].SetSlotState(state);
        }
    }

    private void UpdateItemImages()
    {
        ClearItemImages();
        if (itemSlots != null)
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                InventoryItem item = inventory != null ? inventory.GetItemAtSlot(i) : null;
                itemSlots[i].SetItemInSlot(item);
            }
        }

        if (inventory == null || itemImagePrefab == null || itemImageRoot == null)
        {
            return;
        }

        HashSet<InventoryItem> renderedItems = new HashSet<InventoryItem>();

        for (int i = 0; i < inventory.itemSlotList.Count; i++)
        {
            InventoryItem item = inventory.GetItemAtSlot(i);

            if (item == null)
            {
                continue;
            }

            if (!renderedItems.Add(item))
            {
                continue;
            }

            CreateItemImage(item);
        }
    }

    private void CreateItemImage(InventoryItem item)
    {
        if (item == null || item.ItemData == null || item.ItemData.backpackItemData == null)
        {
            return;
        }

        if (!inventory.TryGetTopLeftOfItem(item, out Vector2Int topLeft))
        {
            return;
        }

        Image itemImage = Instantiate(itemImagePrefab, itemImageRoot);
        itemImageInstances.Add(itemImage);

        itemImage.enabled = true;
        itemImage.raycastTarget = false;
        itemImage.sprite = item.ItemData.itemIcon;

        RectTransform rt = itemImage.transform as RectTransform;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        BackpackItemDataSO backpackItemData = item.ItemData.backpackItemData;

        Vector2Int originalSize = backpackItemData.imageSize;
        Vector2Int rotatedSize = BackpackItemShapeUtility.GetRotatedImageSize(backpackItemData, item.rotateState);

        rt.sizeDelta = GetVisualSize(originalSize);

        Vector2 localCenterInSlotsRoot = GetRectangleCenterLocal(topLeft, rotatedSize);
        Vector2 localCenterInImageRoot = ConvertLocalPoint(slotsRoot, itemImageRoot, localCenterInSlotsRoot);

        rt.anchoredPosition = localCenterInImageRoot;

        int clockwiseDegrees = BackpackItemShapeUtility.GetClockwiseDegrees(item.rotateState);
        rt.localEulerAngles = new Vector3(0, 0, -clockwiseDegrees);
    }

    private bool TryGetNearestTopLeftByMouse(Vector2Int rotatedSize, out Vector2Int topLeft)
    {
        topLeft = Vector2Int.zero;

        if (slotsRoot == null || inventory == null)
        {
            return false;
        }

        if (rotatedSize.x <= 0 || rotatedSize.y <= 0)
        {
            return false;
        }

        if (rotatedSize.x > inventory.ColumnCount || rotatedSize.y > inventory.RowCount)
        {
            return false;
        }

        Camera uiCamera = GetUICamera();

        if (!RectTransformUtility.RectangleContainsScreenPoint(slotsRoot, Input.mousePosition, uiCamera))
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            slotsRoot,
            Input.mousePosition,
            uiCamera,
            out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = slotsRoot.rect;

        Vector2 cellSize = GetCellSize();
        Vector2 spacing = GetSpacing();

        float pitchX = cellSize.x + spacing.x;
        float pitchY = cellSize.y + spacing.y;

        float left = rect.xMin + GetPaddingLeft();
        float top = rect.yMax - GetPaddingTop();

        Vector2 rectPixelSize = GetVisualSize(rotatedSize);

        int column = Mathf.RoundToInt((localPoint.x - left - rectPixelSize.x * 0.5f) / pitchX);
        int row = Mathf.RoundToInt((top - localPoint.y - rectPixelSize.y * 0.5f) / pitchY);

        column = Mathf.Clamp(column, 0, inventory.ColumnCount - rotatedSize.x);
        row = Mathf.Clamp(row, 0, inventory.RowCount - rotatedSize.y);

        topLeft = new Vector2Int(column, row);
        return true;
    }

    private bool IsMouseInsideThisGrid()
    {
        if (slotsRoot == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            slotsRoot,
            Input.mousePosition,
            GetUICamera()
        );
    }

    private Vector2 GetRectangleCenterLocal(Vector2Int topLeft, Vector2Int sizeInCells)
    {
        Rect rect = slotsRoot.rect;

        Vector2 cellSize = GetCellSize();
        Vector2 spacing = GetSpacing();

        float pitchX = cellSize.x + spacing.x;
        float pitchY = cellSize.y + spacing.y;

        float left = rect.xMin + GetPaddingLeft();
        float top = rect.yMax - GetPaddingTop();

        Vector2 visualSize = GetVisualSize(sizeInCells);

        float x = left + topLeft.x * pitchX + visualSize.x * 0.5f;
        float y = top - topLeft.y * pitchY - visualSize.y * 0.5f;

        return new Vector2(x, y);
    }

    private Vector2 GetVisualSize(Vector2Int sizeInCells)
    {
        Vector2 cellSize = GetCellSize();
        Vector2 spacing = GetSpacing();

        float width = sizeInCells.x * cellSize.x + Mathf.Max(0, sizeInCells.x - 1) * spacing.x;
        float height = sizeInCells.y * cellSize.y + Mathf.Max(0, sizeInCells.y - 1) * spacing.y;

        return new Vector2(width, height);
    }

    private Vector2 ConvertLocalPoint(RectTransform from, RectTransform to, Vector2 localPoint)
    {
        Vector3 worldPoint = from.TransformPoint(localPoint);
        Vector3 targetLocalPoint = to.InverseTransformPoint(worldPoint);

        return new Vector2(targetLocalPoint.x, targetLocalPoint.y);
    }

    private Vector2 GetCellSize()
    {
        if (slotsGridLayout != null)
        {
            return slotsGridLayout.cellSize;
        }

        return new Vector2(80, 80);
    }

    private Vector2 GetSpacing()
    {
        if (slotsGridLayout != null)
        {
            return slotsGridLayout.spacing;
        }

        return Vector2.zero;
    }

    private int GetPaddingLeft()
    {
        if (slotsGridLayout != null)
        {
            return slotsGridLayout.padding.left;
        }

        return 0;
    }

    private int GetPaddingTop()
    {
        if (slotsGridLayout != null)
        {
            return slotsGridLayout.padding.top;
        }

        return 0;
    }

    private Camera GetUICamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void ClearSlotStates()
    {
        if (itemSlots == null)
        {
            return;
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i].SetSlotState(SlotState.None);
        }
    }

    private void ClearItemImages()
    {
        for (int i = 0; i < itemImageInstances.Count; i++)
        {
            if (itemImageInstances[i] != null)
            {
                Destroy(itemImageInstances[i].gameObject);
            }
        }

        itemImageInstances.Clear();
    }

    private void CacheRefs()
    {
        if (slotsGridLayout == null)
        {
            slotsGridLayout = GetComponentInChildren<GridLayoutGroup>(true);
        }

        if (slotsRoot == null && slotsGridLayout != null)
        {
            slotsRoot = slotsGridLayout.transform as RectTransform;
        }

        if (slotsGridLayout == null && slotsRoot != null)
        {
            slotsGridLayout = slotsRoot.GetComponent<GridLayoutGroup>();
        }

        itemSlots = slotsRoot != null
    ? slotsRoot.GetComponentsInChildren<ItemSlotUI>(true)
    : GetComponentsInChildren<ItemSlotUI>(true);

        if (draggedItemUI == null)
        {
            InGameUI inGameUI = GetComponentInParent<InGameUI>();

            if (inGameUI != null)
            {
                draggedItemUI = inGameUI.draggedItemUI;
            }
        }
    }

    private void BindSlots()
    {

        Array.Sort(itemSlots, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i].BindItemIndex(i);
            itemSlots[i].SetOwnerGridUI(this);
        }
    }

    private class PlacementPreview
    {
        public Vector2Int topLeft;
        public ItemRotateState rotateState;
        public List<int> targetIndices;
        public HashSet<InventoryItem> conflictItems;
    }
}