using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhotographyOverlayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform overlayRoot;
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

    [Header("Shutter Pulse Settings")]
    [SerializeField] private float shutterCloseDuration = 0.055f;
    // 快门闭合时间
    // 越短越利落

    [SerializeField] private float shutterOpenDuration = 0.18f;
    // 快门回弹恢复时间
    // 稍微长一点会更 Q 弹

    [Range(0.01f, 0.5f)]
    [SerializeField] private float shutterClosedScale = 0.08f;
    // 快门闭合到多小
    // 0.08 表示缩到原本大小的 8%

    [SerializeField] private float shutterOvershootStrength = 1.45f;
    // 回弹力度
    // 越大越 Q 弹
    // 推荐 1.2 ~ 1.8

    [SerializeField] private bool useUnscaledTimeForShutter = true;
    // 快门动画是否使用不受 Time.timeScale 影响的时间
    // 摄影模式可能会慢动作，所以这里建议 true


    private bool isOpen;
    // 当前是否处于开启状态

    private Rect currentVisibleScreenRect;
    // 当前可视区域的屏幕 Rect

    private Coroutine shutterPulseRoutine;
    // 快门脉冲动画协程

    private bool isShutterPulsing;
    // 当前是否正在播放快门脉冲动画


    public bool IsOpen
    {
        get
        {
            return isOpen;
        }
    }

    public bool IsShutterPulsing
    {
        get
        {
            return isShutterPulsing;
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

    }


    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (isShutterPulsing)
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

        StopShutterPulse();

        UpdateMaskByMousePosition();
    }


    public void Close()
    {
        isOpen = false;

        StopShutterPulse();

        if (overlayRoot != null)
        {
            overlayRoot.gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// 播放快门脉冲动画。
    /// 
    /// 调用瞬间会记录鼠标位置，
    /// 然后可视矩形会朝这个位置快速闭合，再 Q 弹恢复。
    /// </summary>
    public void PlayShutterPulse()
    {
        if (!isOpen)
        {
            return;
        }

        Vector2 shutterCenterPosition =
            GetMouseScreenPosition();

        PlayShutterPulse(shutterCenterPosition);
    }


    /// <summary>
    /// 在指定屏幕位置播放快门脉冲动画。
    /// </summary>
    public void PlayShutterPulse(Vector2 myShutterCenterPosition)
    {
        if (!isOpen)
        {
            return;
        }

        StopShutterPulse();

        shutterPulseRoutine = StartCoroutine
        (
            ShutterPulseRoutine(myShutterCenterPosition)
        );
    }


    private void StopShutterPulse()
    {
        if (shutterPulseRoutine != null)
        {
            StopCoroutine(shutterPulseRoutine);
            shutterPulseRoutine = null;
        }

        isShutterPulsing = false;
    }


    private IEnumerator ShutterPulseRoutine(Vector2 myShutterCenterPosition)
    {
        isShutterPulsing = true;

        Vector2 originalSize =
            visibleRectSize;

        Vector2 closedSize =
            visibleRectSize * shutterClosedScale;

        // 第一段：快速闭合
        float closeElapsedTime = 0f;

        while (closeElapsedTime < shutterCloseDuration)
        {
            closeElapsedTime += GetShutterDeltaTime();

            float progress =
                Mathf.Clamp01(closeElapsedTime / shutterCloseDuration);

            float easedProgress =
                EaseInCubic(progress);

            Vector2 currentSize =
                Vector2.Lerp
                (
                    originalSize,
                    closedSize,
                    easedProgress
                );

            ApplyAnimatedMaskRect
            (
                myShutterCenterPosition,
                currentSize
            );

            yield return null;
        }

        // 第二段：Q 弹恢复
        float openElapsedTime = 0f;

        while (openElapsedTime < shutterOpenDuration)
        {
            openElapsedTime += GetShutterDeltaTime();

            float progress =
                Mathf.Clamp01(openElapsedTime / shutterOpenDuration);

            float easedProgress =
                EaseOutBack
                (
                    progress,
                    shutterOvershootStrength
                );

            Vector2 currentSize =
                Vector2.LerpUnclamped
                (
                    closedSize,
                    originalSize,
                    easedProgress
                );

            ApplyAnimatedMaskRect
            (
                myShutterCenterPosition,
                currentSize
            );

            yield return null;
        }

        ApplyAnimatedMaskRect
        (
            myShutterCenterPosition,
            originalSize
        );

        isShutterPulsing = false;
        shutterPulseRoutine = null;

        // 动画结束后，重新回到鼠标跟随状态
        UpdateMaskByMousePosition();
    }


    private void ApplyAnimatedMaskRect
    (
        Vector2 myCenterPosition,
        Vector2 myCurrentVisibleRectSize
    )
    {
        currentVisibleScreenRect =
            CalculateVisibleScreenRect
            (
                myCenterPosition,
                myCurrentVisibleRectSize
            );

        ApplyMaskRect(currentVisibleScreenRect);
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
        return CalculateVisibleScreenRect
        (
            myCenterPosition,
            visibleRectSize
        );
    }


    private Rect CalculateVisibleScreenRect
    (
        Vector2 myCenterPosition,
        Vector2 myVisibleRectSize
    )
    {
        float halfWidth =
            myVisibleRectSize.x * 0.5f;

        float halfHeight =
            myVisibleRectSize.y * 0.5f;

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


    private float GetShutterDeltaTime()
    {
        if (useUnscaledTimeForShutter)
        {
            return Time.unscaledDeltaTime;
        }

        return Time.deltaTime;
    }


    private float EaseInCubic(float myTime)
    {
        return myTime * myTime * myTime;
    }


    /// <summary>
    /// 回弹缓动。
    /// 会超过 1 一点点，再回到 1。
    /// </summary>
    private float EaseOutBack
    (
        float myTime,
        float myOvershoot
    )
    {
        float time =
            myTime - 1f;

        return 1f
            + time * time
            * ((myOvershoot + 1f) * time + myOvershoot);
    }
}