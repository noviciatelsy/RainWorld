using UnityEngine;

/// <summary>
/// 节肢动物式迈步：每条腿在自身 Prefab 休息位 localPosition 的 ±legMoveRange 内活动。
/// 落后时单独向前迈步，同一时刻仅一条腿在迈步。
/// </summary>
public class SurfaceWalkerLegSystem : MonoBehaviour
{
    [System.Serializable]
    public class Leg
    {
        public Transform target;

        public Vector3 restLocalPosition;
        public float alongOffset;
        public bool isStepping;
        public float stepStartOffset;
        public float stepGoalOffset;
        public float stepProgress;
    }

    public SurfaceWalker2D sw;
    [Tooltip("body IK 目标；其 parent 为 legSpace（与 foot 同级）")]
    public Transform body;

    public Leg[] legs = new Leg[6];

    [Tooltip("迈步时沿行进方向移动速度")]
    public float legMoveSpeed = 14f;

    [Tooltip("每条腿相对自身休息位，沿行进方向可活动 ± 此距离")]
    public float legMoveRange = 0.1f;

    [Tooltip("迈步中段抬脚高度（legSpace 局部 Y）")]
    public float stepArcHeight = 0.06f;

    private Transform legSpace;
    private bool initialized;
    private int lastBodyEdge = -1;
    private Vector2 lastBodyOnEdge;

    public void InitializeFromWalker()
    {
        ResolveLegSpace();

        if (!TryGetAnchor(out TileMapGuideManager mgr, out int loopId, out int bodyEdge, out Vector2 bodyOnEdge, out bool clockwise))
        {
            return;
        }

        lastBodyEdge = bodyEdge;
        lastBodyOnEdge = bodyOnEdge;

        Vector3 forwardLocal = GetForwardLocal(mgr, bodyEdge, bodyOnEdge, clockwise);

        for (int i = 0; i < legs.Length; i++)
        {
            Leg leg = legs[i];

            if (leg.target == null)
            {
                continue;
            }

            leg.restLocalPosition = leg.target.localPosition;
            leg.alongOffset = 0f;
            leg.isStepping = false;
            leg.stepProgress = 0f;
            ApplyFootLocal(ref leg, forwardLocal);
            legs[i] = leg;
        }

        initialized = true;
    }

    public void UpdateAfterBodyMoved()
    {
        if (!initialized && sw != null && sw.HasEdge)
        {
            InitializeFromWalker();
        }

        if (!TryGetAnchor(out TileMapGuideManager mgr, out int loopId, out int bodyEdge, out Vector2 bodyOnEdge, out bool clockwise))
        {
            return;
        }

        ResolveLegSpace();
        Vector3 forwardLocal = GetForwardLocal(mgr, bodyEdge, bodyOnEdge, clockwise);
        float travelDelta = ComputeTravelDelta(mgr, loopId, bodyEdge, bodyOnEdge, clockwise);

        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i].isStepping)
            {
                Leg leg = legs[i];
                AdvanceStep(ref leg, forwardLocal);
                legs[i] = leg;
            }
        }

        for (int i = 0; i < legs.Length; i++)
        {
            if (!legs[i].isStepping)
            {
                UpdatePlantedLeg(i, forwardLocal, travelDelta);
            }
        }
    }

    private void UpdatePlantedLeg(int legIndex, Vector3 forwardLocal, float travelDelta)
    {
        Leg leg = legs[legIndex];

        if (leg.target == null)
        {
            return;
        }

        leg.alongOffset -= travelDelta;
        leg.alongOffset = Mathf.Max(leg.alongOffset, -legMoveRange);

        if (leg.alongOffset > -legMoveRange)
        {
            ApplyFootLocal(ref leg, forwardLocal);
            legs[legIndex] = leg;
            return;
        }

        if (!CanStartStep(legIndex))
        {
            ApplyFootLocal(ref leg, forwardLocal);
            legs[legIndex] = leg;
            return;
        }

        leg.isStepping = true;
        leg.stepStartOffset = leg.alongOffset;
        leg.stepGoalOffset = legMoveRange;
        leg.stepProgress = 0f;
        AdvanceStep(ref leg, forwardLocal);
        legs[legIndex] = leg;
    }

    private void AdvanceStep(ref Leg leg, Vector3 forwardLocal)
    {
        leg.alongOffset = Mathf.MoveTowards(
            leg.alongOffset,
            leg.stepGoalOffset,
            legMoveSpeed * Time.deltaTime
        );

        float range = leg.stepGoalOffset - leg.stepStartOffset;
        leg.stepProgress = range > 0.001f
            ? Mathf.Clamp01((leg.alongOffset - leg.stepStartOffset) / range)
            : 1f;

        if (Mathf.Approximately(leg.alongOffset, leg.stepGoalOffset))
        {
            leg.isStepping = false;
            leg.alongOffset = 0f;
            leg.stepProgress = 1f;
        }

        ApplyFootLocal(ref leg, forwardLocal);
    }

    /// <summary>同一时刻仅一条腿迈步；多条落后时选拖拽最多的一条。</summary>
    private bool CanStartStep(int legIndex)
    {
        if (AnyLegStepping())
        {
            return false;
        }

        float myOffset = legs[legIndex].alongOffset;

        for (int i = 0; i < legs.Length; i++)
        {
            if (i == legIndex)
            {
                continue;
            }

            if (legs[i].alongOffset < myOffset - 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    private bool AnyLegStepping()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i].isStepping)
            {
                return true;
            }
        }

        return false;
    }

    private float ComputeTravelDelta(
        TileMapGuideManager mgr,
        int loopId,
        int bodyEdge,
        Vector2 bodyOnEdge,
        bool clockwise)
    {
        if (lastBodyEdge < 0)
        {
            lastBodyEdge = bodyEdge;
            lastBodyOnEdge = bodyOnEdge;
            return 0f;
        }

        float forward = SurfaceEdgeTraversal.DistanceAlongLoopForward(
            mgr,
            lastBodyEdge,
            lastBodyOnEdge,
            bodyEdge,
            bodyOnEdge,
            clockwise,
            loopId
        );

        float backward = SurfaceEdgeTraversal.DistanceAlongLoopForward(
            mgr,
            bodyEdge,
            bodyOnEdge,
            lastBodyEdge,
            lastBodyOnEdge,
            clockwise,
            loopId
        );

        float delta = forward <= backward ? forward : -backward;

        lastBodyEdge = bodyEdge;
        lastBodyOnEdge = bodyOnEdge;
        return delta;
    }

    private Vector3 GetForwardLocal(
        TileMapGuideManager mgr,
        int bodyEdge,
        Vector2 bodyOnEdge,
        bool clockwise)
    {
        if (legSpace == null || sw == null)
        {
            return Vector3.right;
        }

        Vector2 tangentWorld = SurfaceCrawlerVisual.GetEdgeTravelTangent(
            mgr,
            bodyEdge,
            sw.CurrentEdge,
            clockwise,
            bodyOnEdge);

        Vector3 world = new Vector3(tangentWorld.x, tangentWorld.y, 0f);
        Vector3 local = legSpace.InverseTransformDirection(world);

        if (local.sqrMagnitude < 0.0001f)
        {
            return Vector3.right;
        }

        return local.normalized;
    }

    private void ApplyFootLocal(ref Leg leg, Vector3 forwardLocal)
    {
        if (leg.target == null)
        {
            return;
        }

        float clampedOffset = Mathf.Clamp(leg.alongOffset, -legMoveRange, legMoveRange);
        Vector3 local = leg.restLocalPosition + forwardLocal * clampedOffset;

        if (leg.isStepping && stepArcHeight > 0f)
        {
            float lift = Mathf.Sin(leg.stepProgress * Mathf.PI) * stepArcHeight;
            local += Vector3.up * lift;
        }

        leg.target.localPosition = local;
    }

    private void ResolveLegSpace()
    {
        if (legSpace != null)
        {
            return;
        }

        if (body != null && body.parent != null)
        {
            legSpace = body.parent;
            return;
        }

        if (legs.Length > 0 && legs[0].target != null && legs[0].target.parent != null)
        {
            legSpace = legs[0].target.parent;
        }
    }

    private bool TryGetAnchor(
        out TileMapGuideManager mgr,
        out int loopId,
        out int bodyEdge,
        out Vector2 bodyOnEdge,
        out bool clockwise)
    {
        mgr = TileMapGuideManager.Instance;
        loopId = -1;
        bodyEdge = -1;
        bodyOnEdge = default;
        clockwise = false;

        if (mgr == null || sw == null || !sw.TryGetTravelLoopAnchor(out loopId, out bodyEdge, out bodyOnEdge, out clockwise))
        {
            return false;
        }

        return true;
    }
}
