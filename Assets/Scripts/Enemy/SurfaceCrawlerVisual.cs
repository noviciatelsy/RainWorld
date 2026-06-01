using UnityEngine;

/// <summary>
/// 贴边爬行者（Snail / SurfaceWalker）共用：不旋转，bodyVisual 使用 scale(-1,-1) 与边法线偏移。
/// </summary>
public static class SurfaceCrawlerVisual
{
    public static void CacheBaseScale(Transform bodyVisual, ref Vector3 baseScale)
    {
        if (bodyVisual == null)
        {
            baseScale = Vector3.one;
            return;
        }

        baseScale = bodyVisual.localScale;

        if (Mathf.Abs(baseScale.x) < 0.001f)
        {
            baseScale.x = 1f;
        }

        if (Mathf.Abs(baseScale.y) < 0.001f)
        {
            baseScale.y = 1f;
        }
    }

    public static void Apply(
        Transform root,
        Transform bodyVisual,
        Edge currentEdge,
        Vector3 baseScale,
        float normalOffset)
    {
        if (root != null)
        {
            root.rotation = Quaternion.identity;
        }

        Transform visual = bodyVisual != null ? bodyVisual : root;

        if (visual == null)
        {
            return;
        }

        visual.localRotation = Quaternion.identity;
        visual.localScale = new Vector3(
            -Mathf.Abs(baseScale.x),
            -Mathf.Abs(baseScale.y),
            baseScale.z
        );

        if (bodyVisual == null)
        {
            return;
        }

        Vector2 dir = (currentEdge.b - currentEdge.a).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        bodyVisual.localPosition = new Vector3(normal.x, normal.y, 0f) * normalOffset;
    }
}
