using System.Collections.Generic;
using UnityEngine;

public class MoleCave : MonoBehaviour
{
    [Header("活动区域配置")]
    [Tooltip("该洞穴管辖的鼹鼠 Idle 随机游走长方形范围")]
    public Bounds activityBounds;

    [Header("图结构：连通的洞穴")]
    [Tooltip("与当前洞穴互通的其他洞穴列表（无向图的边）")]
    public List<MoleCave> connectedCaves = new List<MoleCave>();

    public Vector2 Position => transform.position;

    private void Awake()
    {
        // 自动将自己注册到管理器中
        MoleCaveManager.Instance?.RegisterCave(this);
    }

    private void OnDestroy()
    {
        // 销毁时安全注销
        MoleCaveManager.Instance?.UnregisterCave(this);
    }

    /// <summary>
    /// 在 Inspector 中手动建立双向连接的辅助方法
    /// </summary>
    public void AddConnection(MoleCave other)
    {
        if (other == null || other == this) return;

        if (!connectedCaves.Contains(other))
            connectedCaves.Add(other);

        if (!other.connectedCaves.Contains(this))
            other.connectedCaves.Add(this);
    }

    // ==========================================
    // 编辑器可视化 (Gizmos)
    // ==========================================
    private void OnDrawGizmos()
    {
        // 1. 绘制活动范围长方形 (黄色)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(activityBounds.center, activityBounds.size);

        // 2. 绘制洞穴核心点 (紫色球)
        Gizmos.color = new Color(0.6f, 0.2f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.3f);

        // 3. 绘制无向图的连通线 (青色)
        if (connectedCaves == null) return;
        Gizmos.color = Color.cyan;
        foreach (var neighbor in connectedCaves)
        {
            if (neighbor != null)
            {
                // 仅在 ID 小于对方时绘制，避免双向线重复绘制导致颜色叠加
                if (this.GetInstanceID() < neighbor.GetInstanceID())
                {
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
                }
            }
        }
    }
}