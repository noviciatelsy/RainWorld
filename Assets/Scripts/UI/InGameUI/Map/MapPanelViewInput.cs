using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapPanelViewInput : MonoBehaviour,
    IScrollHandler,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("引用")]
    [SerializeField] private MapCameraController mapCameraController;

    private RectTransform mapViewRect;

    private bool isDragging;
    private Vector2 lastLocalPointerPosition;


    private void Awake()
    {
        if (mapViewRect == null)
        {
            mapViewRect = transform as RectTransform;
        }

    }

    private void OnEnable()
    {
        isDragging = false;

        if (mapCameraController != null)
        {
            mapCameraController.EnterPanelMode();
        }
    }

    private void OnDisable()
    {
        isDragging = false;

        if (mapCameraController != null)
        {
            mapCameraController.ExitPanelMode();
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (mapCameraController == null)
        {
            return;
        }

        // eventData.scrollDelta 表示本次滚轮滚动量。
        // 具体缩放方向交给 MapCameraController 里的 scrollUpMakesViewBigger 控制。
        mapCameraController.Zoom(eventData.scrollDelta.y);

        eventData.Use();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mapCameraController == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            mapCameraController.ResetViewToMainCamera();
            eventData.Use();
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = TryGetLocalPointerPosition(eventData, out lastLocalPointerPosition);
            eventData.Use();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isDragging = TryGetLocalPointerPosition(eventData, out lastLocalPointerPosition);

        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (mapCameraController == null)
        {
            return;
        }

        if (TryGetLocalPointerPosition(eventData, out Vector2 currentLocalPointerPosition))
        {
            Vector2 localDelta = currentLocalPointerPosition - lastLocalPointerPosition;

            mapCameraController.PanByMapViewLocalDelta(localDelta, mapViewRect);

            lastLocalPointerPosition = currentLocalPointerPosition;
        }

        eventData.Use();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isDragging = false;

        eventData.Use();
    }

    private bool TryGetLocalPointerPosition(PointerEventData eventData, out Vector2 localPointerPosition)
    {
        localPointerPosition = Vector2.zero;

        if (mapViewRect == null)
        {
            return false;
        }

        Camera eventCamera = null;

        if (eventCamera == null)
        {
            eventCamera = eventData.pressEventCamera;

            if (eventCamera == null)
            {
                eventCamera = eventData.enterEventCamera;
            }
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapViewRect,
            eventData.position,
            eventCamera,
            out localPointerPosition
        );
    }
}