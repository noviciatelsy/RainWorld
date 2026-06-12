using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 表示一个可以在黑暗遮罩中创建圆形可视区域的物体。
///
/// 玩家、火把、灯笼和荧光植物等对象都可以挂载此组件。
/// </summary>
public sealed class DarknessRevealSource : MonoBehaviour
{
    /*
     * 当前处于启用状态的全部可视源。
     *
     * 管理器直接读取这个集合，因此不需要手动把每一盏灯
     * 拖到 DarknessMaskController 的列表中。
     */
    private static readonly HashSet<DarknessRevealSource>
        ActiveSourceSet =
            new HashSet<DarknessRevealSource>();

    /// <summary>
    /// 当前所有启用中的可视源。
    /// </summary>
    public static IEnumerable<DarknessRevealSource> ActiveSources
    {
        get
        {
            return ActiveSourceSet;
        }
    }

    [Header("可视范围")]

    [Tooltip("可视区域的半径，单位为 Unity 世界单位。")]
    [SerializeField]
    [Min(0f)]
    private float radius = 3f;

    [Tooltip("可视区域边缘的柔和宽度，单位为 Unity 世界单位。")]
    [SerializeField]
    [Min(0f)]
    private float edgeSoftness = 0.25f;

    [Tooltip("可视区域圆心相对于当前物体的本地坐标偏移。")]
    [SerializeField]
    private Vector2 centerOffset;

    [Tooltip("是否允许这个物体产生可视区域。")]
    [SerializeField]
    private bool revealEnabled = true;

    /// <summary>
    /// 可视半径，单位为世界单位。
    /// </summary>
    public float Radius
    {
        get
        {
            return radius;
        }
    }

    /// <summary>
    /// 边缘柔和宽度，单位为世界单位。
    /// </summary>
    public float EdgeSoftness
    {
        get
        {
            return edgeSoftness;
        }
    }



    /// <summary>
    /// 当前可视区域的世界坐标圆心。
    /// </summary>
    public Vector3 WorldPosition
    {
        get
        {
            Vector3 localOffset = new Vector3(
                centerOffset.x,
                centerOffset.y,
                0f
            );

            return transform.TransformPoint(localOffset);
        }
    }

    /// <summary>
    /// 当前组件是否应当产生可视区域。
    /// </summary>
    public bool CanReveal
    {
        get
        {
            return isActiveAndEnabled &&
                   revealEnabled &&
                   radius > 0f;
        }
    }

    private void OnEnable()
    {
        ActiveSourceSet.Add(this);
    }

    private void OnDisable()
    {
        ActiveSourceSet.Remove(this);
    }

    private void OnDestroy()
    {
        // 避免对象销毁后集合中残留无效引用。
        ActiveSourceSet.Remove(this);
    }


    /// <summary>
    /// 在运行时启用或关闭这个可视源。
    /// </summary>
    public void SetRevealEnabled(bool enabled)
    {
        revealEnabled = enabled;
    }

    /// <summary>
    /// 在运行时修改可视半径。
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0f, newRadius);
    }


    public void AddRadius(float radiusToAdd)
    {
        radius += radiusToAdd;
    }

    public void ReduceRadius(float radiusToRemove)
    {
        radius -= radiusToRemove;
    }
    /// <summary>
    /// 在运行时修改边缘柔和宽度。
    /// </summary>
    public void SetEdgeSoftness(float newSoftness)
    {
        edgeSoftness = Mathf.Max(
            0f,
            newSoftness
        );
    }

    private void OnDrawGizmosSelected()
    {
        /*
         * 在 Scene 窗口中显示可视范围，
         * 方便直接调整 Radius 参数。
         */
        Gizmos.DrawWireSphere(
            WorldPosition,
            radius
        );

        if (edgeSoftness > 0f)
        {
            Gizmos.DrawWireSphere(
                WorldPosition,
                radius + edgeSoftness
            );
        }
    }
}