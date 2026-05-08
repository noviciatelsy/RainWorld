using System;
using UnityEngine;
using UnityEngine.UI;

public class DraggedItemUI : MonoBehaviour
{
    [Header("Refs")]
    private Canvas rootCanvas; // 主Canvas
    private Image draggedIconImage; // 跟随鼠标的图标

    public bool IsDragging { get; private set; } // bool锁，是否正在拖拽物品
    public InventoryItem draggedItem { get; private set; } // 拖拽时暂存的物品

    public InventoryBase SourceInventory { get; private set; } // 原本来自哪个InventoryBase
    public Vector2Int SourceTopLeft { get; private set; } // 原本所在矩形左上角
    public ItemRotateState SourceRotateState { get; private set; } // 原本旋转状态
    public bool HasSourcePlacement { get; private set; } // 是否记录了原位置

    public event Action<InventoryItem> OnBeginDraggingItem;
    public event Action<InventoryItem> OnEndDraggingItem;
    public event Action OnDraggedItemRotated;

    private RectTransform selfRt; // 自身的Rect

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        draggedIconImage = GetComponent<Image>();
        selfRt = transform as RectTransform;


        HideItem(); // 隐藏拖拽时的物品图标
    }

    private void Update()
    {
        if (!IsDragging)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            Input.mousePosition,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out Vector2 mousePosition
        );

        selfRt.anchoredPosition = mousePosition;

        if (Input.GetMouseButtonDown(1)) // 右键按下
        {
            RotateDraggedItem(); // 旋转物品
        }
    }

    public void BeginDrag(InventoryItem item, InventoryBase sourceInventory = null)
    {
        BeginDrag(item, sourceInventory, false, Vector2Int.zero, item != null ? item.rotateState : ItemRotateState.Rotate0);
    }

    public void BeginDrag(
            InventoryItem item,
            InventoryBase sourceInventory,
            bool hasSourcePlacement,
            Vector2Int sourceTopLeft,
            ItemRotateState sourceRotateState
        )
    {
        if (item == null)
        {
            return;
        }

        draggedItem = item;
        SourceInventory = sourceInventory;
        HasSourcePlacement = hasSourcePlacement;
        SourceTopLeft = sourceTopLeft;
        SourceRotateState = sourceRotateState;

        IsDragging = true;

        ShowItem();

        OnBeginDraggingItem?.Invoke(draggedItem);
    }


    public void EndDrag()
    {
        InventoryItem endedItem = draggedItem;

        HideItem();

        draggedItem = null;
        SourceInventory = null;
        SourceTopLeft = Vector2Int.zero;
        SourceRotateState = ItemRotateState.Rotate0;
        HasSourcePlacement = false;

        IsDragging = false;

        OnEndDraggingItem?.Invoke(endedItem);
    }


    public bool TryReturnToSource()
    {
        if (!IsDragging || draggedItem == null || SourceInventory == null || !HasSourcePlacement)
        {
            return false;
        }

        draggedItem.rotateState = SourceRotateState;

        bool success = SourceInventory.PlaceItem(
            draggedItem,
            SourceTopLeft,
            SourceRotateState
        );

        if (success)
        {
            EndDrag();
        }

        return success;
    }

    private void HideItem()
    {
        if (draggedIconImage != null)
        {
            draggedIconImage.enabled = false; // 禁用图标（使其不可见）
        }

        if (selfRt != null)
        {
            selfRt.sizeDelta = Vector2.zero;
            selfRt.localEulerAngles = Vector3.zero;
        }
    }

    private void ShowItem()
    {
        if (draggedItem == null || draggedItem.ItemData == null || draggedItem.ItemData.backpackItemData == null)
        {
            HideItem();
            return;
        }

        draggedIconImage.enabled = true;
        draggedIconImage.sprite = draggedItem.ItemData.itemIcon;

        BackpackItemDataSO backpackItemData = draggedItem.ItemData.backpackItemData;

        Vector2 itemSize = new Vector2(
            backpackItemData.imageSize.x,
            backpackItemData.imageSize.y
        );

        selfRt.sizeDelta = itemSize * backpackItemData.pixelAmount;

        int clockwiseDegrees = BackpackItemShapeUtility.GetClockwiseDegrees(draggedItem.rotateState);

        // UI 正方向是逆时针，所以顺时针旋转用负角度
        selfRt.localEulerAngles = new Vector3(0, 0, -clockwiseDegrees);
    }

    private void RotateDraggedItem()
    {
        if (draggedItem == null)
        {
            return;
        }

        draggedItem.rotateState = BackpackItemShapeUtility.GetNextClockwise(draggedItem.rotateState);
        ShowItem();
        OnDraggedItemRotated?.Invoke();
    }
}