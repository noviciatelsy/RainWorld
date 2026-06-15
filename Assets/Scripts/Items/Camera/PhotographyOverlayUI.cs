using UnityEngine;
using UnityEngine.InputSystem;

public class PhotographyOverlayUI : MonoBehaviour
{
    [Header("References")]
    private RectTransform overlayRoot;
    // 覆盖整屏的根节点

    [SerializeField] private RectTransform leftBlackImage;
    // 左侧黑色遮罩

    [SerializeField] private RectTransform rightBlackImage;
    // 右侧黑色遮罩

    [SerializeField] private RectTransform topBlackImage;
    // 上侧黑色遮罩

    [SerializeField] private RectTransform bottomBlackImage;
    // 下侧黑色遮罩


    [Header("View Rect Settings")]
    [SerializeField] private Vector2 visibleRectSize = new Vector2(360f, 220f);
    // 中间可视矩形大小，单位是屏幕像素

    [SerializeField] private bool clampVisibleRectInsideScreen = true;
    // 是否把可视矩形限制在屏幕内部

    [SerializeField] private bool showOnAwake = false;
    // Awake 时是否显示

    private bool isOpen;
    // 当前是否处于开启状态

    private Rect currentVisibleScreenRect;
    // 当前可视区域的屏幕 Rect


    public bool IsOpen
    {
        get
        {
            return isOpen;
        }
    }

    public Rect CurrentVisibleScreenRect
    {
        get
        {
            return currentVisibleScreenRect;
        }
    }


    private void Awake()
    {
        overlayRoot = GetComponent<RectTransform>();

        if (showOnAwake)
        {
            Open();
        }
        else
        {
            Close();
        }
    }


    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        UpdateMaskByMousePosition();
    }


    public void Open()
    {
        isOpen = true;

        if (overlayRoot != null)
        {
            overlayRoot.gameObject.SetActive(true);
        }

        UpdateMaskByMousePosition();
    }


    public void Close()
    {
        isOpen = false;

        if (overlayRoot != null)
        {
            overlayRoot.gameObject.SetActive(false);
        }
    }


    private void UpdateMaskByMousePosition()
    {
        Vector2 mouseScreenPosition =
            GetMouseScreenPosition();

        currentVisibleScreenRect =
            CalculateVisibleScreenRect(mouseScreenPosition);

        ApplyMaskRect(currentVisibleScreenRect);
    }


    private Vector2 GetMouseScreenPosition()
    {
        if (Mouse.current == null)
        {
            return new Vector2
            (
                Screen.width * 0.5f,
                Screen.height * 0.5f
            );
        }

        return Mouse.current.position.ReadValue();
    }


    private Rect CalculateVisibleScreenRect(Vector2 myCenterPosition)
    {
        float halfWidth =
            visibleRectSize.x * 0.5f;

        float halfHeight =
            visibleRectSize.y * 0.5f;

        float left =
            myCenterPosition.x - halfWidth;

        float right =
            myCenterPosition.x + halfWidth;

        float bottom =
            myCenterPosition.y - halfHeight;

        float top =
            myCenterPosition.y + halfHeight;

        if (clampVisibleRectInsideScreen)
        {
            if (left < 0f)
            {
                right -= left;
                left = 0f;
            }

            if (right > Screen.width)
            {
                left -= right - Screen.width;
                right = Screen.width;
            }

            if (bottom < 0f)
            {
                top -= bottom;
                bottom = 0f;
            }

            if (top > Screen.height)
            {
                bottom -= top - Screen.height;
                top = Screen.height;
            }

            left =
                Mathf.Clamp(left, 0f, Screen.width);

            right =
                Mathf.Clamp(right, 0f, Screen.width);

            bottom =
                Mathf.Clamp(bottom, 0f, Screen.height);

            top =
                Mathf.Clamp(top, 0f, Screen.height);
        }

        return Rect.MinMaxRect
        (
            left,
            bottom,
            right,
            top
        );
    }


    private void ApplyMaskRect(Rect myVisibleRect)
    {
        float screenWidth =
            Mathf.Max(1f, Screen.width);

        float screenHeight =
            Mathf.Max(1f, Screen.height);

        float left01 =
            myVisibleRect.xMin / screenWidth;

        float right01 =
            myVisibleRect.xMax / screenWidth;

        float bottom01 =
            myVisibleRect.yMin / screenHeight;

        float top01 =
            myVisibleRect.yMax / screenHeight;

        SetStretchRect
        (
            leftBlackImage,
            new Vector2(0f, 0f),
            new Vector2(left01, 1f)
        );

        SetStretchRect
        (
            rightBlackImage,
            new Vector2(right01, 0f),
            new Vector2(1f, 1f)
        );

        SetStretchRect
        (
            bottomBlackImage,
            new Vector2(left01, 0f),
            new Vector2(right01, bottom01)
        );

        SetStretchRect
        (
            topBlackImage,
            new Vector2(left01, top01),
            new Vector2(right01, 1f)
        );
    }


    private void SetStretchRect
    (
        RectTransform myRectTransform,
        Vector2 myAnchorMin,
        Vector2 myAnchorMax
    )
    {
        if (myRectTransform == null)
        {
            return;
        }

        myRectTransform.anchorMin =
            myAnchorMin;

        myRectTransform.anchorMax =
            myAnchorMax;

        myRectTransform.offsetMin =
            Vector2.zero;

        myRectTransform.offsetMax =
            Vector2.zero;
    }
}