using UnityEngine;

public enum SurfaceCrawlerVisualStyle
{
    /// <summary>蜗牛：贴图默认朝上，旋转到边法线。</summary>
    Snail,
    /// <summary>识别行者：Inspector 设 travelClockwise；只改 rotation.z 与 scale.x。</summary>
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
            ApplySurfaceWalker(
                bodyVisual,
                0,
                currentEdge,
                baseScale,
                false,
                0f,
                Vector2.zero
            );
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
    /// 只改 localEulerAngles.z 与 localScale.x。
    /// 旋转仅由当前边几何 + travelClockwise 决定，同一条边上保持不变。
    /// </summary>
    public static void ComputeSurfaceWalkerVisual(
        int edgeIndex,
        Edge currentEdge,
        Vector3 baseScale,
        bool travelClockwise,
        float rotationOffset,
        Vector2 worldPosition,
        out float zAngle,
        out float scaleX)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;
        Vector2 tangent = ResolveEdgeTravelTangent(mgr, edgeIndex, currentEdge, travelClockwise, worldPosition);
        zAngle = Vector2.SignedAngle(Vector2.left, tangent) + rotationOffset;
        scaleX = travelClockwise ? -baseScale.x : baseScale.x;
    }

    public static void ApplySurfaceWalker(
        Transform target,
        int edgeIndex,
        Edge currentEdge,
        Vector3 baseScale,
        bool travelClockwise,
        float rotationOffset,
        Vector2 worldPosition)
    {
        if (target == null)
        {
            return;
        }

        ComputeSurfaceWalkerVisual(
            edgeIndex,
            currentEdge,
            baseScale,
            travelClockwise,
            rotationOffset,
            worldPosition,
            out float z,
            out float scaleX
        );

        Vector3 euler = target.localEulerAngles;
        target.localEulerAngles = new Vector3(euler.x, euler.y, z);
        target.localScale = new Vector3(scaleX, baseScale.y, baseScale.z);
    }

    /// <summary>
    /// 沿 loop 的前进切线；不读移动目标，避免每帧抖动/上下翻转。
    /// </summary>
    private static Vector2 ResolveEdgeTravelTangent(
        TileMapGuideManager mgr,
        int edgeIndex,
        Edge edge,
        bool travelClockwise,
        Vector2 worldPosition)
    {
        if (mgr != null)
        {
            Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(worldPosition, edge.a, edge.b);
            Vector2 corner = SurfaceEdgePath.GetForwardCorner(mgr, edgeIndex, onEdge, travelClockwise);
            Vector2 along = corner - onEdge;

            if (along.sqrMagnitude >= MinTravelDeltaSqr)
            {
                return along.normalized;
            }
        }

        Vector2 edgeDir = (edge.b - edge.a).normalized;
        return travelClockwise ? -edgeDir : edgeDir;
    }

    /// <summary>
    /// 当前移动是否沿 loop 顺时针（用于识别行者 scale 翻转）。
    /// </summary>
    public static bool ComputeTravelClockwise(
        TileMapGuideManager mgr,
        int edgeIndex,
        Vector2 fromPosition,
        Vector2 targetPosition,
        bool fallbackClockwise)
    {
        if (mgr == null)
        {
            return fallbackClockwise;
        }

        Vector2 delta = targetPosition - fromPosition;

        if (delta.sqrMagnitude < MinTravelDeltaSqr)
        {
            return fallbackClockwise;
        }

        Edge edge = mgr.GetEdge(edgeIndex);
        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(fromPosition, edge.a, edge.b);
        Vector2 cwCorner = SurfaceEdgePath.GetForwardCorner(mgr, edgeIndex, onEdge, true);
        Vector2 ccwCorner = SurfaceEdgePath.GetForwardCorner(mgr, edgeIndex, onEdge, false);

        Vector2 moveDir = delta.normalized;
        float dotCw = Vector2.Dot(moveDir, (cwCorner - onEdge).normalized);
        float dotCcw = Vector2.Dot(moveDir, (ccwCorner - onEdge).normalized);

        if (dotCw >= TravelDotThreshold && dotCw > dotCcw)
        {
            return true;
        }

        if (dotCcw >= TravelDotThreshold && dotCcw > dotCw)
        {
            return false;
        }

        return fallbackClockwise;
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
