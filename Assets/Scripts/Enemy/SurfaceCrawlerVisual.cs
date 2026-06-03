using UnityEngine;

public enum SurfaceCrawlerVisualStyle
{
    /// <summary>蜗牛：贴图默认朝上，旋转到边法线。</summary>
    Snail,
    /// <summary>识别行者：贴图默认朝左，旋转到沿边行走方向（骨骼驱动）。</summary>
    SurfaceWalker
}

/// <summary>
/// 贴边爬行者（Snail / SurfaceWalker）视觉对齐。
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

    public static void Apply(
        SurfaceCrawlerVisualStyle style,
        Transform root,
        Transform bodyVisual,
        Edge currentEdge,
        Vector3 baseScale,
        float normalOffset,
        int travelSignAlongEdge,
        ref float cachedSignX)
    {
        if (style == SurfaceCrawlerVisualStyle.SurfaceWalker)
        {
            ApplySurfaceWalker(root, bodyVisual, currentEdge, baseScale, travelSignAlongEdge, ref cachedSignX);
            return;
        }

        ApplySnail(root, bodyVisual, currentEdge, baseScale, normalOffset, travelSignAlongEdge, ref cachedSignX);
    }

    public static void ApplySnail(
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
    /// 识别行者：贴图默认朝左，旋转到切线方向；不偏移 body 位置（避免破坏腿 restOffset）。
    /// </summary>
    public static void ApplySurfaceWalker(
        Transform root,
        Transform bodyVisual,
        Edge currentEdge,
        Vector3 baseScale,
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
        Vector2 tangent = GetTangentAlongTravel(dir, travelSignAlongEdge, ref cachedSignX);

        float angle = Vector2.SignedAngle(Vector2.left, tangent);
        bodyVisual.localRotation = Quaternion.Euler(0f, 0f, angle);

        float signX = ResolveScaleSignX(travelSignAlongEdge, ref cachedSignX);
        bodyVisual.localScale = new Vector3(
            signX * Mathf.Abs(baseScale.x),
            -Mathf.Abs(baseScale.y),
            baseScale.z
        );
        bodyVisual.localPosition = Vector3.zero;
    }

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

    private static Vector2 GetTangentAlongTravel(
        Vector2 edgeDirection,
        int travelSignAlongEdge,
        ref float cachedSignX)
    {
        if (travelSignAlongEdge > 0)
        {
            return edgeDirection;
        }

        if (travelSignAlongEdge < 0)
        {
            return -edgeDirection;
        }

        return cachedSignX > 0f ? edgeDirection : -edgeDirection;
    }

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
            cachedSignX = -1f;
        }

        return cachedSignX;
    }

    public static Vector2 GetOutwardNormal(Vector2 edgeDirection)
    {
        return new Vector2(-edgeDirection.y, edgeDirection.x);
    }
}
