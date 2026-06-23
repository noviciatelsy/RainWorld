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

    [Header("UseItem")]
    [SerializeField] private float secondaryUseHoldThreshold = 0.35f;

    private bool isPressingLeftUse;
    private bool hasTriggeredLongPressDrop;
    private float leftUseStartTime;

    public bool IsDragging { get; private set; } // bool锁，是否正在拖拽物品

    private bool isInSlot; // 是否在物品槽位内 
    private bool isInMerchant;
    public InventoryItem draggedItem { get; private set; } // 拖拽时暂存的物品


    public event Action<InventoryItem> OnBeginDraggingItem;
    public event Action<InventoryItem> OnEndDraggingItem;
    public event Action OnDraggedItemRotated;

    private RectTransform selfRt; // 自身的Rect
    private InventoryPlayer playerInventory;
    private Player player;

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

    private void OnEnable()
    {
        GetPlayerInventory(PlayerManager.Instance.TryGetCurrentPlayer());
        PlayerManager.Instance.OnPlayerRegistered += GetPlayerInventory;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerRegistered -= GetPlayerInventory;
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

        HandleDraggedItemLeftUseInput();
    }


    public void BeginDrag(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        draggedItem = item;
        IsDragging = true;
        draggedItem.SubscribeToPlayer(player);

        ResetDragInertia();
        ResetLeftUseInputState();

        ShowItem();

        OnBeginDraggingItem?.Invoke(draggedItem);
    }

    public void EndDrag()
    {
        if (draggedItem == null)
        {
            HideItem();
            IsDragging = false;
            ResetLeftUseInputState();
            return;
        }

        InventoryItem endedItem = draggedItem;

        draggedItem.UnsubscribeToPlayer();

        HideItem();

        draggedItem = null;
        IsDragging = false;

        ResetLeftUseInputState();

        OnEndDraggingItem?.Invoke(endedItem);
    }

    public void TryDropItem()
    {
        if (!IsDragging || draggedItem == null)
        {
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("拖拽物品丢弃失败：playerInventory 为空。");
            return;
        }

        InventoryItem itemToDrop = draggedItem;

        if (itemToDrop.ItemData == null)
        {
            return;
        }

        // 如果这个物品还残留在快捷栏，清掉引用
        playerInventory.ClearQuickItem(itemToDrop);

        // 如果这个物品正被手持，取消手持
        if (playerInventory.GetHoldingItem() == itemToDrop)
        {
            playerInventory.ClearHoldingItem();
        }

        // 防御性移除。
        // 正常拖拽时，这个物品多半已经不在 Inventory 中。
        playerInventory.RemoveItem(itemToDrop);

        playerInventory.DropItem(itemToDrop.ItemData);

        EndDrag();

        playerInventory.ValidateQuickItems(null);
        playerInventory.ValidateHoldingItem(null);
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
        AudioManager.Instance.PlayUI("ItemRotateSFX");
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

    private void GetPlayerInventory(Player player)
    {
        if(player == null)
        {
            return;
        }
        this.player = player;
        playerInventory = player.GetComponent<InventoryPlayer>();
    }

    public void SetInSlot(bool isInSlot)
    {
        this.isInSlot = isInSlot;
    }

    private void HandleDraggedItemLeftUseInput()
    {
        if (!IsDragging || draggedItem == null)
        {
            ResetLeftUseInputState();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryStartLeftUseInput();
        }

        if (isPressingLeftUse)
        {
            UpdateLeftUseInput();
        }

        if (Input.GetMouseButtonUp(0))
        {
            TryFinishLeftUseInput();
        }
    }

    private void TryStartLeftUseInput()
    {
        // 鼠标在槽位内时，左键应该交给槽位处理放置，不触发使用/丢弃
        if (isInSlot||isInMerchant)
        {
            return;
        }

        isPressingLeftUse = true;
        hasTriggeredLongPressDrop = false;
        leftUseStartTime = GetCurrentUseTime();
    }

    private void UpdateLeftUseInput()
    {
        if (!isPressingLeftUse)
        {
            return;
        }

        // 如果按住过程中进入了槽位，就取消这次使用/丢弃判断
        if (isInSlot)
        {
            ResetLeftUseInputState();
            return;
        }

        if (hasTriggeredLongPressDrop)
        {
            return;
        }

        float holdDuration = GetCurrentUseTime() - leftUseStartTime;

        if (holdDuration >= secondaryUseHoldThreshold)
        {
            hasTriggeredLongPressDrop = true;

            // 长按达到阈值，直接丢弃
            TryDropItem();

            ResetLeftUseInputState();
        }
    }

    private void TryFinishLeftUseInput()
    {
        if (!isPressingLeftUse)
        {
            return;
        }

        bool shouldTryMainUse =
            !hasTriggeredLongPressDrop &&
            !isInSlot &&
            IsDragging &&
            draggedItem != null&&
           !isInMerchant ;

        ResetLeftUseInputState();

        if (shouldTryMainUse)
        {
            TryMainUseDraggedItem();
        }
    }

    private void ResetLeftUseInputState()
    {
        isPressingLeftUse = false;
        hasTriggeredLongPressDrop = false;
        leftUseStartTime = 0f;
    }

    private float GetCurrentUseTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    private void TryMainUseDraggedItem()
    {
        if (!IsDragging || draggedItem == null)
        {
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("拖拽物品使用失败：playerInventory 为空。");
            return;
        }

        if (draggedItem.ItemData == null)
        {
            return;
        }

        if (draggedItem.ItemData.itemType != ItemType.Active)
        {
            Debug.Log($"{draggedItem.ItemData.itemDisplayName} 不是主动道具，没有 MainUse。");
            return;
        }

        bool useSucceeded = draggedItem.MainUse(playerInventory);

        if (!useSucceeded)
        {
            return;
        }

        ActiveItemDataSO activeItemData = draggedItem.ItemData as ActiveItemDataSO;

        bool isConsumable =
            activeItemData != null &&
            activeItemData.isConsumable;

        if (isConsumable)
        {
            // 如果这个拖拽物品本身是消耗品，使用成功后结束拖拽。
            // 由于拖拽时它通常已经临时离开 Inventory，所以这里 RemoveItem 只是防御。
            playerInventory.ClearQuickItem(draggedItem);

            if (playerInventory.GetHoldingItem() == draggedItem)
            {
                playerInventory.ClearHoldingItem();
            }

            playerInventory.RemoveItem(draggedItem);

            EndDrag();

            playerInventory.ValidateQuickItems(null);
            playerInventory.ValidateHoldingItem(null);
        }
    }

    public void SetInMerchant(bool isInMerchant)
    {
        this.isInMerchant = isInMerchant;
    }
}