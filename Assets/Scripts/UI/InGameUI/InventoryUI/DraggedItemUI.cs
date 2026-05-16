using System;
using UnityEngine;
using UnityEngine.UI;

public class DraggedItemUI : MonoBehaviour
{
    [Header("Refs")]
    private Canvas rootCanvas; // 主Canvas
    private Image draggedIconImage; // 跟随鼠标的图标
    [SerializeField] private int slotSize = 65;
    [SerializeField] private int spaceSize = 5;

    [Header("Drag Inertia")]
    [SerializeField] private float inertiaAngleStrength = 0.02f; // 鼠标速度转旋转角度的强度
    [SerializeField] private float maxInertiaAngle = 18f; // 最大惯性旋转角度
    [SerializeField] private float inertiaSmoothTime = 0.08f; // 惯性角度平滑时间，越小越灵敏
    [SerializeField] private float velocitySmoothTime = 0.04f; // 鼠标速度平滑时间，越小越跟手
    [SerializeField] private float maxVelocityForInertia = 2000f; // 参与惯性计算的最大鼠标速度
    [SerializeField] private bool useUnscaledTime = true; // 是否无视Time.timeScale

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

    private Vector2 previousMousePosition; // 上一帧鼠标在Canvas本地坐标中的位置
    private Vector2 smoothedMouseVelocity; // 平滑后的鼠标速度
    private Vector2 smoothedMouseVelocityRef; // Vector2.SmoothDamp内部使用的速度引用
    private bool hasPreviousMousePosition; // 是否已经记录过上一帧鼠标位置

    private float baseRotationAngle; // 物品自身旋转角度
    private float inertiaAngle; // 拖拽时额外叠加的惯性角度
    private float inertiaAngleVelocity; // Mathf.SmoothDampAngle内部使用的速度引用

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

        if (rootCanvas == null || selfRt == null)
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

        UpdateDragInertia(mousePosition);

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

        ResetDragInertia();
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

        ResetDragInertia();

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

        selfRt.sizeDelta = new Vector2(
            itemSize.x * slotSize + (itemSize.x - 1) * spaceSize,
            itemSize.y * slotSize + (itemSize.y - 1) * spaceSize
        );

        int clockwiseDegrees = BackpackItemShapeUtility.GetClockwiseDegrees(draggedItem.rotateState);

        // UI 正方向是逆时针，所以顺时针旋转用负角度
        baseRotationAngle = -clockwiseDegrees;

        ApplyVisualRotation();
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

    private void UpdateDragInertia(Vector2 mousePosition)
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // SmoothDampAngle在deltaTime为0时可能产生异常结果
        if (deltaTime <= 0f)
        {
            return;
        }

        if (!hasPreviousMousePosition)
        {
            previousMousePosition = mousePosition;
            hasPreviousMousePosition = true;
            ApplyVisualRotation();
            return;
        }

        Vector2 rawMouseVelocity = (mousePosition - previousMousePosition) / deltaTime;
        previousMousePosition = mousePosition;

        rawMouseVelocity = Vector2.ClampMagnitude(rawMouseVelocity, maxVelocityForInertia);

        smoothedMouseVelocity = Vector2.SmoothDamp(
            smoothedMouseVelocity,
            rawMouseVelocity,
            ref smoothedMouseVelocityRef,
            Mathf.Max(0.0001f, velocitySmoothTime),
            Mathf.Infinity,
            deltaTime
        );

        // 鼠标向右移动时，图片会产生顺时针惯性倾斜
        float targetInertiaAngle = Mathf.Clamp(
            -smoothedMouseVelocity.x * inertiaAngleStrength,
            -maxInertiaAngle,
            maxInertiaAngle
        );

        inertiaAngle = Mathf.SmoothDampAngle(
            inertiaAngle,
            targetInertiaAngle,
            ref inertiaAngleVelocity,
            Mathf.Max(0.0001f, inertiaSmoothTime),
            Mathf.Infinity,
            deltaTime
        );

        ApplyVisualRotation();
    }

    private void ApplyVisualRotation()
    {
        if (selfRt == null)
        {
            return;
        }

        selfRt.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle + inertiaAngle);
    }

    private void ResetDragInertia()
    {
        previousMousePosition = Vector2.zero;
        smoothedMouseVelocity = Vector2.zero;
        smoothedMouseVelocityRef = Vector2.zero;
        hasPreviousMousePosition = false;

        inertiaAngle = 0f;
        inertiaAngleVelocity = 0f;
    }
}