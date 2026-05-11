using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    private Camera mainCamera;
    private Camera mapCamera;

    [Header("默认视野")]
    [SerializeField] private float defaultOrthographicSize = 18f;
    [SerializeField] private float minOrthographicSize = 4.5f;
    [SerializeField] private float maxOrthographicSize = 54f;

    [Tooltip("滚轮缩放速度")]
    [SerializeField] private float scrollZoomSpeed = 2f;

    [Tooltip("勾选后：滚轮向上，地图视野变大；取消勾选后：滚轮向上，地图视野变小")]
    [SerializeField] private bool scrollUpMakesViewBigger = true;

    [Header("关闭面板时跟随主相机")]
    [SerializeField] private Vector2 followOffset = Vector2.zero;

    [Tooltip("关闭地图面板时，是否禁用地图相机渲染来节省性能")]
    [SerializeField] private bool renderOnlyWhenPanelOpen = true;

    [Header("可选：地图拖动边界")]
    [SerializeField] private bool clampToMapBounds = false;

    [Tooltip("地图允许显示区域的左下角世界坐标")]
    [SerializeField] private Vector2 mapBoundsMin;

    [Tooltip("地图允许显示区域的右上角世界坐标")]
    [SerializeField] private Vector2 mapBoundsMax;

    private bool isPanelOpen;
    private float fixedCameraZ;

    public bool IsPanelOpen
    {
        get
        {
            return isPanelOpen;
        }
    }

    private void Awake()
    {
        mapCamera = GetComponent<Camera>();
        mainCamera = Camera.main;

        fixedCameraZ = transform.position.z;

        ResetViewToMainCamera();
        RefreshCameraRenderingState();
    }

    private void LateUpdate()
    {
        // 地图面板关闭时，地图相机一直跟随主相机。
        // 放在 LateUpdate，是为了等主相机本帧移动完之后再同步位置。
        if (!isPanelOpen)
        {
            FollowMainCamera();
        }
    }

    public void EnterPanelMode()
    {
        isPanelOpen = true;

        RefreshCameraRenderingState();

        // 打开地图面板时，回到当前主相机位置 + 默认视野
        ResetViewToMainCamera();
    }

    public void ExitPanelMode()
    {
        // 退出地图面板时，也重置一次，避免下次打开残留上次拖动和缩放
        ResetViewToMainCamera();

        isPanelOpen = false;

        RefreshCameraRenderingState();
    }

    public void ResetViewToMainCamera()
    {
        if (mapCamera == null)
        {
            return;
        }

        mapCamera.orthographicSize = Mathf.Clamp(
            defaultOrthographicSize,
            minOrthographicSize,
            maxOrthographicSize
        );

        Vector3 targetPosition = transform.position;

        if (mainCamera != null)
        {
            targetPosition.x = mainCamera.transform.position.x + followOffset.x;
            targetPosition.y = mainCamera.transform.position.y + followOffset.y;
        }

        targetPosition.z = fixedCameraZ;

        SetCameraPosition(targetPosition);
    }

    public void Zoom(float scrollDelta)
    {
        if (!isPanelOpen || mapCamera == null)
        {
            return;
        }

        if (Mathf.Abs(scrollDelta) <= 0.01f)
        {
            return;
        }

        float direction = scrollUpMakesViewBigger ? 1f : -1f;

        float targetSize = mapCamera.orthographicSize + scrollDelta * scrollZoomSpeed * direction;

        mapCamera.orthographicSize = Mathf.Clamp(
            targetSize,
            minOrthographicSize,
            maxOrthographicSize
        );

        // 缩放后重新限制位置，避免视野变大之后露出地图边界外
        SetCameraPosition(transform.position);
    }

    public void PanByMapViewLocalDelta(Vector2 localDelta, RectTransform mapViewRect)
    {
        if (!isPanelOpen || mapCamera == null || mapViewRect == null)
        {
            return;
        }

        Rect rect = mapViewRect.rect;

        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        float viewHeight = mapCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * GetMapCameraAspect();

        float worldDeltaX = localDelta.x / rect.width * viewWidth;
        float worldDeltaY = localDelta.y / rect.height * viewHeight;

        Vector3 targetPosition = transform.position;

        targetPosition.x -= worldDeltaX;
        targetPosition.y -= worldDeltaY;
        targetPosition.z = fixedCameraZ;

        SetCameraPosition(targetPosition);
    }

    private void FollowMainCamera()
    {
        if (mainCamera == null || mapCamera == null)
        {
            return;
        }

        mapCamera.orthographicSize = Mathf.Clamp(
            defaultOrthographicSize,
            minOrthographicSize,
            maxOrthographicSize
        );

        Vector3 targetPosition = transform.position;

        targetPosition.x = mainCamera.transform.position.x + followOffset.x;
        targetPosition.y = mainCamera.transform.position.y + followOffset.y;
        targetPosition.z = fixedCameraZ;

        SetCameraPosition(targetPosition);
    }

    private void SetCameraPosition(Vector3 targetPosition)
    {
        targetPosition.z = fixedCameraZ;

        if (clampToMapBounds)
        {
            targetPosition = ClampPositionToMapBounds(targetPosition);
        }

        transform.position = targetPosition;
    }

    private Vector3 ClampPositionToMapBounds(Vector3 targetPosition)
    {
        float boundsMinX = Mathf.Min(mapBoundsMin.x, mapBoundsMax.x);
        float boundsMaxX = Mathf.Max(mapBoundsMin.x, mapBoundsMax.x);
        float boundsMinY = Mathf.Min(mapBoundsMin.y, mapBoundsMax.y);
        float boundsMaxY = Mathf.Max(mapBoundsMin.y, mapBoundsMax.y);

        float halfHeight = mapCamera.orthographicSize;
        float halfWidth = halfHeight * GetMapCameraAspect();

        float minCameraX = boundsMinX + halfWidth;
        float maxCameraX = boundsMaxX - halfWidth;
        float minCameraY = boundsMinY + halfHeight;
        float maxCameraY = boundsMaxY - halfHeight;

        if (minCameraX <= maxCameraX)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minCameraX, maxCameraX);
        }
        else
        {
            targetPosition.x = (boundsMinX + boundsMaxX) * 0.5f;
        }

        if (minCameraY <= maxCameraY)
        {
            targetPosition.y = Mathf.Clamp(targetPosition.y, minCameraY, maxCameraY);
        }
        else
        {
            targetPosition.y = (boundsMinY + boundsMaxY) * 0.5f;
        }

        return targetPosition;
    }

    private float GetMapCameraAspect()
    {
        if (mapCamera != null && mapCamera.targetTexture != null && mapCamera.targetTexture.height > 0)
        {
            return (float)mapCamera.targetTexture.width / mapCamera.targetTexture.height;
        }

        if (mapCamera != null)
        {
            return mapCamera.aspect;
        }

        return 1f;
    }

    private void RefreshCameraRenderingState()
    {
        if (mapCamera == null)
        {
            return;
        }

        if (renderOnlyWhenPanelOpen)
        {
            mapCamera.enabled = isPanelOpen;
        }
        else
        {
            mapCamera.enabled = true;
        }
    }

    private void OnValidate()
    {
        minOrthographicSize = Mathf.Max(0.01f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
        defaultOrthographicSize = Mathf.Clamp(defaultOrthographicSize, minOrthographicSize, maxOrthographicSize);
        scrollZoomSpeed = Mathf.Max(0.01f, scrollZoomSpeed);
    }
}