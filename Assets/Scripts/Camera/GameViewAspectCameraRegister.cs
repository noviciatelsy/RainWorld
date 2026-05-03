using UnityEngine;

[DefaultExecutionOrder(-9000)]
[RequireComponent(typeof(Camera))]
public class GameViewAspectCameraRegister : MonoBehaviour
{
    [Header("注册设置")]
    [SerializeField] private bool registerOnEnable = true;
    [SerializeField] private bool unregisterOnDisable = true;

    [Header("特殊相机")]
    [SerializeField] private bool ignoreWhenRenderingToTexture = true;

    [Header("调试")]
    [SerializeField] private bool resetRectWhenDisabled = false;

    private Camera cachedCamera;
    private bool isRegistered;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (registerOnEnable)
        {
            Register();
        }
    }

    private void Start()
    {
        // 兜底。
        // 如果某些场景初始化顺序比较奇怪，OnEnable 时管理器还没准备好，
        // Start 再尝试注册一次。
        if (registerOnEnable && !isRegistered)
        {
            Register();
        }
    }

    private void OnDisable()
    {
        if (unregisterOnDisable)
        {
            Unregister();
        }

        if (resetRectWhenDisabled && cachedCamera != null)
        {
            cachedCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Register()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        if (cachedCamera == null)
        {
            return;
        }

        // 如果这个相机是渲染到 RenderTexture 的，例如小地图贴图、角色预览贴图，
        // 通常不应该被屏幕纵宽比系统影响。
        if (ignoreWhenRenderingToTexture && cachedCamera.targetTexture != null)
        {
            return;
        }

        if (GameViewAspectController.Instance == null)
        {
            Debug.LogWarning(
                "没有找到 GameViewAspectController。" +
                "请确认全局场景或 GlobalGameRoot 中存在 GameViewAspectController。"
            );

            return;
        }

        GameViewAspectController.Instance.RegisterCamera(cachedCamera);
        isRegistered = true;
    }

    private void Unregister()
    {
        if (!isRegistered)
        {
            return;
        }

        if (GameViewAspectController.Instance != null)
        {
            GameViewAspectController.Instance.UnregisterCamera(cachedCamera);
        }

        isRegistered = false;
    }
}