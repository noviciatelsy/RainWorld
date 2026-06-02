using UnityEngine;

/// <summary>
/// 贴边爬行者（Snail / SurfaceWalker）共用：
/// 贴图默认朝上，旋转到边法线；scale.x 仅随明确行进方向翻转（避免在路点附近抖动）。
/// </summary>
public static class SurfaceCrawlerVisual
{
    private const float MinTravelDeltaSqr = 0.04f;
    private const float TravelDotThreshold = 0.35f;

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

    /// <param name="travelSignAlongEdge">
    /// 沿边行进方向：1 = 与 a→b 同向，-1 = 反向，0 = 保持上一帧 scale.x 符号。
    /// </param>
    public static void Apply(
        Transform root,
        Transform bodyVisual,
        Edge currentEdge,
        Vector3 baseScale,
        float normalOffset,
        int travelSignAlongEdge,
        ref float cachedSignX)
    {
        if (root != null)
        {
            root.rotation = Quaternion.identity;
        }

        if (bodyVisual == null)
        {
            return;
        }

        Vector2 dir = (currentEdge.b - currentEdge.a).normalized;
        Vector2 normal = GetOutwardNormal(dir);

        float angle = Vector2.SignedAngle(Vector2.up, normal);
        bodyVisual.localRotation = Quaternion.Euler(0f, 0f, angle);

        float signX = ResolveScaleSignX(travelSignAlongEdge, ref cachedSignX);
        bodyVisual.localScale = new Vector3(
            signX * Mathf.Abs(baseScale.x),
            -Mathf.Abs(baseScale.y),
            baseScale.z
        );
        bodyVisual.localPosition = new Vector3(normal.x, normal.y, 0f) * normalOffset;
    }

    /// <summary>
    /// 根据「当前位置 → 目标点」相对边方向计算稳定行进符号；距离过近时返回 fallback。
    /// </summary>
    public static int ComputeTravelSignAlongEdge(
        Edge edge,
        Vector2 fromPosition,
        Vector2 targetPosition,
        int fallbackSign = 0)
    {
        Vector2 delta = targetPosition - fromPosition;

        if (delta.sqrMagnitude < MinTravelDeltaSqr)
        {
            return fallbackSign;
        }

        Vector2 dir = (edge.b - edge.a).normalized;
        float along = Vector2.Dot(delta.normalized, dir);

        if (along >= TravelDotThreshold)
        {
            return 1;
        }

        if (along <= -TravelDotThreshold)
        {
            return -1;
        }

        return fallbackSign;
    }

    /// <summary>
    /// 与边 a→b 同向为 -1，反向为 +1（水平右移时贴图朝右）。
    /// </summary>
    private static float ResolveScaleSignX(int travelSignAlongEdge, ref float cachedSignX)
    {
        if (travelSignAlongEdge > 0)
        {
            cachedSignX = 1f;
        }
        else if (travelSignAlongEdge < 0)
        {
            cachedSignX = -1f;
        }
        else if (Mathf.Abs(cachedSignX) < 0.001f)
        {
            cachedSignX = 1f;
        }

        return cachedSignX;
    }

    /// <summary>
    /// 垂直于行走方向的外法线（相对边方向逆时针 90°）。
    /// </summary>
    public static Vector2 GetOutwardNormal(Vector2 edgeDirection)
    {
        return new Vector2(-edgeDirection.y, edgeDirection.x);
    }
}
