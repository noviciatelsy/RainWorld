using UnityEngine;
using UnityEngine.Serialization;

public class SurfaceWalker2D : MonsterBase, IMeatBaitAttractable, IToyCarAttractable
{
    public float moveSpeed = 3f;
    public float fallSpeed = 6f;

    [Header("Movement")]
    [Tooltip("勾选 = 顺时针沿 loop；不勾选 = 逆时针")]
    public bool travelClockwise = false;

    [Tooltip("整个 Prefab 根节点沿边法线离边的距离（移动逻辑仍在边线上）")]
    [FormerlySerializedAs("visualNormalOffset")]
    public float edgeNormalOffset = 0f;

    public SurfaceWalkerLegSystem legSystem;

    [Header("Visual")]
    [Tooltip("直接改此物体的 localScale 与 localEulerAngles.z")]
    public Transform visualTransform;
    [Tooltip("Prefab 默认 scale.x=-1、朝左；若整体朝向反了可填 180")]
    public float visualRotationOffset = 0f;

    [Header("Attraction")]
    public float detectRadius = 10f;
    public float perceptionInterval = 0.3f;

    private Vector3 baseVisualScale = Vector3.one;
    private int lastVisualEdgeIndex = -1;
    private bool lastVisualClockwise;
    private float cachedVisualZ;
    private float cachedVisualScaleX;

    private SurfaceWalkerUtilityAI walkerAI;

    protected override void Init()
    {
        walkerAI = new SurfaceWalkerUtilityAI(this);
        ai = walkerAI;
        motor = new SurfaceWalkerMotor();

        if (legSystem == null)
        {
            legSystem = GetComponent<SurfaceWalkerLegSystem>();
        }

        if (legSystem != null)
        {
            legSystem.sw = this;
        }

        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        baseVisualScale = visualTransform.localScale;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            HasEdge = false;
            return;
        }

        SurfaceEdgePath.TrySnapToNearestEdge(
            mgr,
            Position,
            out int edgeIndex,
            out Edge edge,
            out Vector2 snapped
        );

        EdgeIndex = edgeIndex;
        CurrentEdge = edge;
        transform.position = snapped;
        HasEdge = true;
        Arrived = true;

        SurfaceEdgePath.SyncEdgeStateFromPosition(this);
        ApplyRootNormalOffset();
        lastVisualEdgeIndex = -1;
        ApplyVisual(force: true);

        if (legSystem != null)
        {
            legSystem.InitializeFromWalker();
        }
    }

    /// <summary>边线上的逻辑位置（不含根节点法线 offset）。</summary>
    public Vector2 GetOnEdgeWorldPosition()
    {
        if (!HasEdge || Mathf.Approximately(edgeNormalOffset, 0f))
        {
            return Position;
        }

        Vector2 normal = GetEdgeOutwardNormal();
        return Position - normal * edgeNormalOffset;
    }

    public Vector2 GetEdgeOutwardNormal()
    {
        Vector2 edgeDir = (CurrentEdge.b - CurrentEdge.a).normalized;
        return SurfaceCrawlerVisual.GetOutwardNormal(edgeDir);
    }

    /// <summary>移动后：先落在边线上，再做法线 offset（不影响 SyncEdgeStateFromPosition）。</summary>
    public void SetOnEdgeThenApplyOffset(Vector2 onEdge)
    {
        transform.position = onEdge;
        ApplyRootNormalOffset();
    }

    public void ApplyRootNormalOffset()
    {
        if (!HasEdge || Mathf.Approximately(edgeNormalOffset, 0f))
        {
            return;
        }

        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(
            transform.position,
            CurrentEdge.a,
            CurrentEdge.b
        );
        transform.position = onEdge + GetEdgeOutwardNormal() * edgeNormalOffset;
    }

    public Vector2 StripNormalOffset(Vector2 worldPoint)
    {
        if (!HasEdge || Mathf.Approximately(edgeNormalOffset, 0f))
        {
            return worldPoint;
        }

        return worldPoint - GetEdgeOutwardNormal() * edgeNormalOffset;
    }

    /// <summary>供 LegSystem 读取 loop 锚点；不参与移动。</summary>
    public bool TryGetTravelLoopAnchor(
        out int loopId,
        out int bodyEdgeIndex,
        out Vector2 bodyOnEdge,
        out bool clockwise)
    {
        loopId = -1;
        bodyEdgeIndex = -1;
        bodyOnEdge = default;
        clockwise = travelClockwise;

        if (!HasEdge)
        {
            return false;
        }

        loopId = CurrentEdge.loopId;
        bodyEdgeIndex = EdgeIndex;
        bodyOnEdge = GetOnEdgeWorldPosition();
        return true;
    }

    public void ApplyVisual(bool force = false)
    {
        if (!HasEdge || visualTransform == null)
        {
            return;
        }

        bool needsRecalc = force
            || EdgeIndex != lastVisualEdgeIndex
            || travelClockwise != lastVisualClockwise;

        if (needsRecalc)
        {
            SurfaceCrawlerVisual.ComputeSurfaceWalkerVisual(
                EdgeIndex,
                CurrentEdge,
                baseVisualScale,
                travelClockwise,
                visualRotationOffset,
                GetOnEdgeWorldPosition(),
                out cachedVisualZ,
                out cachedVisualScaleX
            );

            lastVisualEdgeIndex = EdgeIndex;
            lastVisualClockwise = travelClockwise;
        }

        Vector3 euler = visualTransform.localEulerAngles;
        visualTransform.localEulerAngles = new Vector3(euler.x, euler.y, cachedVisualZ);
        visualTransform.localScale = new Vector3(
            cachedVisualScaleX,
            -Mathf.Abs(baseVisualScale.y),
            baseVisualScale.z
        );

        if (visualTransform != transform)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    private void LateUpdate()
    {
        ApplyVisual();

        if (legSystem != null && HasEdge)
        {
            legSystem.UpdateAfterBodyMoved();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        if (Application.isPlaying && HasEdge)
        {
            lastVisualEdgeIndex = -1;
            ApplyVisual(force: true);
        }
    }
#endif

    public void AttractToMeatBait(Vector2 myMeatBaitPosition)
    {
        walkerAI?.ForcePerceptionRefresh();
    }

    public void AttractToToyCar(Vector2 myToyCarPosition)
    {
        walkerAI?.ForcePerceptionRefresh();
    }
}
