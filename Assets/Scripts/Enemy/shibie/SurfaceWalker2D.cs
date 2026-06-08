using UnityEngine;

public class SurfaceWalker2D : MonsterBase
{
    public float moveSpeed = 3f;
    public float fallSpeed = 6f;

    [Header("Movement")]
    [Tooltip("勾选 = 顺时针沿 loop；不勾选 = 逆时针")]
    public bool travelClockwise = false;

    public SurfaceWalkerLegSystem legSystem;

    [Header("Visual")]
    [Tooltip("直接改此物体的 localScale 与 localEulerAngles.z")]
    public Transform visualTransform;
    [Tooltip("Prefab 默认 scale.x=-1、朝左；若整体朝向反了可填 180")]
    public float visualRotationOffset = 0f;

    private Vector3 baseVisualScale = Vector3.one;
    private int lastVisualEdgeIndex = -1;
    private bool lastVisualClockwise;
    private float cachedVisualZ;
    private float cachedVisualScaleX;

    protected override void Init()
    {
        ai = new SurfaceWalkerUtilityAI(this);
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
        lastVisualEdgeIndex = -1;
        ApplyVisual(force: true);
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
                Position,
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
            baseVisualScale.y,
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
}
