using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 收集场景中的全部 DarknessRevealSource，
/// 并将它们的位置和半径发送给黑暗遮罩 Shader。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class DarknessMaskController : MonoBehaviour
{
    /*
     * 必须与 Shader 中的 MAX_REVEAL_SOURCES 保持一致。
     */
    private const int MaxRevealSources = 32;

    [Header("必要引用")]
    [Tooltip("负责渲染游戏世界的摄像机。")]
    private Camera worldCamera;

    [Tooltip("使用 UI/MultiRevealDarknessMask Shader 的材质。")]
    [SerializeField]
    private Material darknessMaterialTemplate;

    [Header("黑暗参数")]
    [Tooltip("黑暗遮罩的颜色和最大不透明度。")]
    [SerializeField]
    private Color darknessColor = Color.black;

    [Tooltip("场景开始时是否立即启用黑暗遮罩。")]
    [SerializeField]
    private bool startDarknessActive=false;

    private Image darknessImage;
    private Material runtimeMaterial;

    /*
     * 固定长度的 Shader 数据数组。
     *
     * Material.SetVectorArray 第一次设置的数组长度
     * 会决定该属性之后可使用的最大长度，
     * 因此这里始终发送完整的固定长度数组。
     */
    private readonly Vector4[] revealSourceData =
        new Vector4[MaxRevealSources];

    private bool manualDarknessActive;

    private bool hasShownSourceLimitWarning;

    private static readonly int DarknessColorPropertyId =
        Shader.PropertyToID("_DarknessColor");

    private static readonly int RevealCountPropertyId =
        Shader.PropertyToID("_RevealCount");

    private static readonly int RevealSourcesPropertyId =
        Shader.PropertyToID("_RevealSources");

    private void Awake()
    {
        darknessImage = GetComponent<Image>();
        // 黑色 UI 只负责显示，不应该阻挡鼠标和触摸输入
        darknessImage.raycastTarget = false;

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            Debug.LogError(
                "DarknessMaskController：没有找到游戏摄像机。",
                this
            );

            enabled = false;
            return;
        }

        if (darknessMaterialTemplate == null)
        {
            Debug.LogError(
                "DarknessMaskController：没有设置黑暗遮罩材质。",
                this
            );

            enabled = false;
            return;
        }

        /*
         * 创建运行时材质实例。
         *
         * 这样修改 Shader 参数时，不会直接修改
         * Project 窗口中的材质资源。
         */
        runtimeMaterial =
            new Material(darknessMaterialTemplate);

        darknessImage.material =
            runtimeMaterial;

        runtimeMaterial.SetColor(
            DarknessColorPropertyId,
            darknessColor
        );

        /*
         * 第一次就传递完整长度的数组。
         * 后续也始终使用相同长度。
         */
        runtimeMaterial.SetVectorArray(
            RevealSourcesPropertyId,
            revealSourceData
        );

        runtimeMaterial.SetInteger(
            RevealCountPropertyId,
            0
        );

        manualDarknessActive =
            startDarknessActive;

        RefreshMaskState();
    }

    private void LateUpdate()
    {
        if (runtimeMaterial == null ||
            worldCamera == null ||
            darknessImage == null ||
            !darknessImage.enabled)
        {
            return;
        }

        UpdateRevealSources();
    }

    /// <summary>
    /// 收集全部有效可视源，并发送给 Shader。
    /// </summary>
    private void UpdateRevealSources()
    {
        int revealCount = 0;

        float aspectRatio =
            Mathf.Max(
                (float)worldCamera.pixelWidth /
                Mathf.Max(worldCamera.pixelHeight, 1),
                0.0001f
            );

        foreach (
            DarknessRevealSource source
            in DarknessRevealSource.ActiveSources
        )
        {
            if (source == null ||
                !source.CanReveal)
            {
                continue;
            }

            Vector3 worldPosition =
                source.WorldPosition;

            Vector3 viewportPosition =
                worldCamera.WorldToViewportPoint(
                    worldPosition
                );

            /*
             * z 小于或等于 0 表示物体位于摄像机后方。
             */
            if (viewportPosition.z <= 0f)
            {
                continue;
            }

            /*
             * 将以世界单位表示的半径转换为视口空间半径。
             *
             * 使用摄像机自身的向上方向计算，
             * 因此不需要把 Radius 手动换算为屏幕百分比。
             */
            float viewportRadius =
                ConvertWorldDistanceToViewportDistance(
                    worldPosition,
                    source.Radius
                );

            float viewportSoftness =
                ConvertWorldDistanceToViewportDistance(
                    worldPosition,
                    source.EdgeSoftness
                );

            float totalExtent =
                viewportRadius + viewportSoftness;

            /*
             * 由于 Shader 对 X 轴进行了宽高比修正，
             * 视口空间中的横向范围需要除以宽高比。
             */
            float horizontalExtent =
                totalExtent / aspectRatio;

            /*
             * 完全位于屏幕外的可视源无需发送给 Shader。
             */
            if (viewportPosition.x <
                    -horizontalExtent ||
                viewportPosition.x >
                    1f + horizontalExtent ||
                viewportPosition.y <
                    -totalExtent ||
                viewportPosition.y >
                    1f + totalExtent)
            {
                continue;
            }

            if (revealCount >= MaxRevealSources)
            {
                if (!hasShownSourceLimitWarning)
                {
                    Debug.LogWarning(
                        "DarknessMaskController：" +
                        "当前屏幕中的可视源超过了 " +
                        MaxRevealSources +
                        " 个，多余的可视源将被忽略。",
                        this
                    );

                    hasShownSourceLimitWarning = true;
                }

                break;
            }

            revealSourceData[revealCount] =
                new Vector4(
                    viewportPosition.x,
                    viewportPosition.y,
                    viewportRadius,
                    viewportSoftness
                );

            revealCount++;
        }

        runtimeMaterial.SetInteger(
            RevealCountPropertyId,
            revealCount
        );

        /*
         * 始终传递相同长度的数组。
         * Shader 只会读取 RevealCount 以内的数据。
         */
        runtimeMaterial.SetVectorArray(
            RevealSourcesPropertyId,
            revealSourceData
        );
    }

    /// <summary>
    /// 将一个世界空间距离转换为视口空间的纵向距离。
    /// </summary>
    private float ConvertWorldDistanceToViewportDistance(
        Vector3 worldPosition,
        float worldDistance
    )
    {
        if (worldDistance <= 0f)
        {
            return 0f;
        }

        Vector3 viewportCenter =
            worldCamera.WorldToViewportPoint(
                worldPosition
            );

        Vector3 viewportEdge =
            worldCamera.WorldToViewportPoint(
                worldPosition +
                worldCamera.transform.up * worldDistance
            );

        return Mathf.Abs(
            viewportEdge.y - viewportCenter.y
        );
    }


    /// <summary>
    /// 手动启用或关闭黑暗效果。
    ///
    /// 房间触发器产生的黑暗状态不会被这个方法覆盖。
    /// 只要玩家仍处于任意黑暗房间，遮罩就会继续显示。
    /// </summary>
    public void SetDarknessActive(bool active)
    {
        manualDarknessActive = active;

        RefreshMaskState();
    }

    /// <summary>
    /// 根据手动状态和房间状态刷新遮罩。
    /// </summary>
    private void RefreshMaskState()
    {
        if (darknessImage == null)
        {
            return;
        }

        darknessImage.enabled =
            manualDarknessActive;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}