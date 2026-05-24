using UnityEngine;
using UnityEngine.EventSystems;

public class SellArea : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform sellBoard;

    [Header("Board Animation")]
    [SerializeField] private float influenceRadius = 180f; // 鼠标进入这个半径后，挡板开始逐渐抬起
    [SerializeField] private float fullOpenRadius = 45f; // 鼠标距离中心小于这个距离时，视为完全抬起
    [SerializeField] private float openOffsetY = 120f; // 挡板完全打开时向上移动多少
    [SerializeField] private float boardSmoothSpeed = 18f; // 挡板跟随速度

    private GoodsShelfUI goodsShelfUI;
    private DraggedItemUI draggedItemUI;
    private RectTransform selfRect;
    private Canvas rootCanvas;

    private Vector2 closedAnchoredPosition;

    private bool isPointerInsideSellArea;

    private void Awake()
    {
        goodsShelfUI = GetComponentInParent<GoodsShelfUI>();

        InGameUI inGameUI = GetComponentInParent<InGameUI>();

        if (inGameUI != null)
        {
            draggedItemUI = inGameUI.draggedItemUI;
        }

        selfRect = transform as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();

        if (sellBoard != null)
        {
            closedAnchoredPosition = sellBoard.anchoredPosition;
        }
    }

    private void Update()
    {
        UpdateBoardVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInsideSellArea = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInsideSellArea = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (goodsShelfUI == null)
        {
            return;
        }

        goodsShelfUI.TrySellItem();
    }

    private void UpdateBoardVisual()
    {
        if (sellBoard == null)
        {
            return;
        }

        float openProgress = CalculateOpenProgress();

        Vector2 openedPosition = closedAnchoredPosition + Vector2.up * openOffsetY;
        Vector2 targetPosition = Vector2.Lerp(closedAnchoredPosition, openedPosition, openProgress);

        sellBoard.anchoredPosition = Vector2.Lerp(
            sellBoard.anchoredPosition,
            targetPosition,
            Time.unscaledDeltaTime * boardSmoothSpeed
        );
    }

    private float CalculateOpenProgress()
    {
        if (draggedItemUI == null || !draggedItemUI.IsDragging)
        {
            return 0f;
        }

        // 关键改动：
        // 鼠标不在 SellArea 里面时，不再计算距离中心，也不会抬起挡板。
        if (!isPointerInsideSellArea)
        {
            return 0f;
        }

        if (selfRect == null || rootCanvas == null)
        {
            return 0f;
        }

        Camera eventCamera = null;

        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = rootCanvas.worldCamera;
        }

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            selfRect,
            Input.mousePosition,
            eventCamera,
            out Vector2 localMousePosition
        );

        if (!converted)
        {
            return 0f;
        }

        float distanceToCenter = Vector2.Distance(localMousePosition, selfRect.rect.center);

        // 距离中心足够近时，直接视为完全打开。
        if (distanceToCenter <= fullOpenRadius)
        {
            return 1f;
        }

        // 超出影响半径时，完全关闭。
        if (distanceToCenter >= influenceRadius)
        {
            return 0f;
        }

        // 在 fullOpenRadius 和 influenceRadius 之间时，按距离渐变打开。
        float progress = Mathf.InverseLerp(influenceRadius, fullOpenRadius, distanceToCenter);

        return Mathf.Clamp01(progress);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (influenceRadius < 1f)
        {
            influenceRadius = 1f;
        }

        if (fullOpenRadius < 0f)
        {
            fullOpenRadius = 0f;
        }

        if (fullOpenRadius > influenceRadius)
        {
            fullOpenRadius = influenceRadius;
        }
    }
#endif
}