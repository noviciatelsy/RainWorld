using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class GameViewAspectController : MonoBehaviour
{
    public static GameViewAspectController Instance
    {
        get;
        private set;
    }

    [Header("目标画面比例")]
    [SerializeField] private float targetWidth = 16f;
    [SerializeField] private float targetHeight = 9f;

    [Header("黑边背景")]
    [SerializeField] private bool useBackgroundCamera = true;
    [SerializeField] private Color barColor = Color.black;
    [SerializeField] private float backgroundCameraDepthOffset = 1f;


    private readonly HashSet<Camera> registeredCameras = new HashSet<Camera>();

    private Camera backgroundCamera;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private Rect currentGameViewportRect = new Rect(0f, 0f, 1f, 1f);

    public Rect CurrentGameViewportRect
    {
        get
        {
            return currentGameViewportRect;
        }
    }

    public float TargetAspect
    {
        get
        {
            return targetWidth / targetHeight;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PrepareBackgroundCameraIfNeeded();
        ApplyAspectRatio();
    }

    private void Update()
    {
        // 桌面端窗口缩放、移动端旋转屏幕、外接显示器变化等情况，
        // 都可能让 Screen.width / Screen.height 改变。
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyAspectRatio();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        targetWidth = Mathf.Max(0.01f, targetWidth);
        targetHeight = Mathf.Max(0.01f, targetHeight);
        backgroundCameraDepthOffset = Mathf.Max(0.01f, backgroundCameraDepthOffset);

        if (Application.isPlaying && Instance == this)
        {
            ApplyAspectRatio();
        }
    }
#endif


    public void RegisterCamera(Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return;
        }

        if (targetCamera == backgroundCamera)
        {
            return;
        }

        registeredCameras.Add(targetCamera);

        RemoveInvalidCameras();
        ApplyAspectRatio();
    }

    public void UnregisterCamera(Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return;
        }

        registeredCameras.Remove(targetCamera);

        RemoveInvalidCameras();
        UpdateBackgroundCameraDepth();
    }

    private void ApplyAspectRatio()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (lastScreenWidth <= 0 || lastScreenHeight <= 0)
        {
            return;
        }

        currentGameViewportRect = CalculateViewportRect();

        RemoveInvalidCameras();

        foreach (Camera targetCamera in registeredCameras)
        {
            ApplyViewportToCamera(targetCamera, currentGameViewportRect);
        }

        PrepareBackgroundCameraIfNeeded();
        UpdateBackgroundCameraDepth();
    }

    private Rect CalculateViewportRect()
    {
        float targetAspect = targetWidth / targetHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        // 屏幕比目标画面更“窄”。
        // 例如 16:10 屏幕显示 16:9 画面时，上下留黑边。
        if (screenAspect < targetAspect)
        {
            float normalizedHeight = screenAspect / targetAspect;
            float yOffset = (1f - normalizedHeight) * 0.5f;

            return new Rect(0f, yOffset, 1f, normalizedHeight);
        }

        // 屏幕比目标画面更“宽”。
        // 例如 21:9 屏幕显示 16:9 画面时，左右留黑边。
        float normalizedWidth = targetAspect / screenAspect;
        float xOffset = (1f - normalizedWidth) * 0.5f;

        return new Rect(xOffset, 0f, normalizedWidth, 1f);
    }

    private void ApplyViewportToCamera(Camera targetCamera, Rect viewportRect)
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.rect = viewportRect;
    }

    private void PrepareBackgroundCameraIfNeeded()
    {
        if (!useBackgroundCamera)
        {
            if (backgroundCamera != null)
            {
                backgroundCamera.enabled = false;
            }

            return;
        }

        if (backgroundCamera == null)
        {
            GameObject cameraObject = new GameObject("BlackBarBackgroundCamera");
            cameraObject.transform.SetParent(transform);

            backgroundCamera = cameraObject.AddComponent<Camera>();
        }

        backgroundCamera.enabled = true;

        // 这个相机不渲染任何物体，只负责把整块屏幕先清成黑色。
        // 之后真正的游戏相机只渲染中间的 16:9 区域。
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = barColor;
        backgroundCamera.cullingMask = 0;
        backgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    private void UpdateBackgroundCameraDepth()
    {
        if (backgroundCamera == null)
        {
            return;
        }

        bool hasValidCamera = false;
        float lowestDepth = 0f;

        foreach (Camera targetCamera in registeredCameras)
        {
            if (targetCamera == null)
            {
                continue;
            }

            if (!hasValidCamera)
            {
                lowestDepth = targetCamera.depth;
                hasValidCamera = true;
            }
            else
            {
                lowestDepth = Mathf.Min(lowestDepth, targetCamera.depth);
            }
        }

        if (hasValidCamera)
        {
            backgroundCamera.depth = lowestDepth - backgroundCameraDepthOffset;
        }
        else
        {
            backgroundCamera.depth = -1000f;
        }
    }

    private void RemoveInvalidCameras()
    {
        registeredCameras.RemoveWhere(camera => camera == null);
    }

    public Rect GetGamePixelRect()
    {
        return new Rect(
            currentGameViewportRect.x * Screen.width,
            currentGameViewportRect.y * Screen.height,
            currentGameViewportRect.width * Screen.width,
            currentGameViewportRect.height * Screen.height
        );
    }

    public bool IsScreenPointInsideGameView(Vector2 screenPoint)
    {
        return GetGamePixelRect().Contains(screenPoint);
    }
}