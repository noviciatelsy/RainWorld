using System.Collections.Generic;
using UnityEngine;

public class WolfSpider2D : MonsterBase, IMeatBaitAttractable, IToyCarAttractable, ITorchRepellable
{
    [Header("Jump")]
    public float moveSpeed = 8f;
    public float minJumpDist = 1.5f;
    public float maxJumpDist = 4f;
    public float arcHeight = 1.2f;

    [Header("Perception")]
    public float detectRadius = 8f;
    public float perceptionInterval = 0.2f;
    public float pathPickInterval = 0.35f;
    public float movementStuckTimeout = 1f;
    public LayerMask playerLayer;
    [Tooltip("Layer 名称 fly；若尚未在 TagManager 配置，可留空并依赖 Fly 组件检测")]
    public LayerMask flyLayer;
    public string flyTag = "Fly";

    [Header("Combat")]
    public float attackRange = 1.2f;
    [Tooltip("攻击动画播放时长（秒）")]
    public float attackAnimDuration = 1f;
    [Tooltip("攻击后再次攻击的最小间隔（秒），间隔内显示待机贴图")]
    public float attackInterval = 1.5f;
    public int attackDamage = 10;
    [Tooltip("踩踏平台碰撞盒大小 (宽, 高)，挂在 bodyVisual 头顶")]
    public Vector2 stompPlatformSize = new Vector2(0.75f, 0.12f);

    [Header("Aggro")]
    public float aggroMemoryDuration = 3f;

    [Header("Idle")]
    public Bounds activityBounds;
    public float idleJumpInterval = 2f;
    public float postLandJumpCooldown = 0.35f;

    [Header("Surface")]
    public float surfaceSnapMaxDistance = 0.85f;
    [Tooltip("选点/站立时的体型半径，避免贴角卡住")]
    public float bodyRadius = 0.34f;
    public float visualSurfaceOffset = 0.1f;
    [Tooltip("与 SurfaceWalker 相同：贴图朝向补偿")]
    public float visualRotationOffset = 0f;
    public Transform bodyVisual;

    [Header("Debug")]
    public bool enableDebugLog = false;
    public bool drawDebugGizmos = true;

    public int PerceptionMask { get; private set; }

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];

    public bool IsJumping { get; set; }
    public bool IsCoolingDown { get; set; }
    public bool IsPerformingAttackAnim { get; private set; }
    public float PostLandJumpCooldownTimer { get; private set; }
    public bool IsPostLandJumpCooldown => PostLandJumpCooldownTimer > 0f;
    public bool JumpTargetRejected { get; private set; }
    public Vector2 CurrentSurfaceNormal { get; private set; } = Vector2.up;
    public WolfSpiderBehavior CurrentBehavior { get; set; } = WolfSpiderBehavior.Idle;

    public bool DebugHasPrey { get; set; }
    public bool DebugPreyIsFly { get; set; }
    public string DebugPreyName { get; set; } = "None";
    public Vector2 DebugPreyPosition { get; set; }
    public float DebugAggroTimer { get; set; }
    public string DebugPickReason { get; set; } = string.Empty;
    public int DebugColliderHitCount { get; set; }
    public int DebugFlyScanCount { get; set; }
    public readonly List<Vector2> DebugArcSamples = new List<Vector2>();
    public readonly List<Vector2> DebugCandidatePoints = new List<Vector2>();

    private WolfSpiderUtilityAI spiderAI;
    private WolfSpiderAni spiderAni;
    private Vector3 baseVisualScale = Vector3.one;
    private bool travelClockwise;
    private int lastVisualEdgeIndex = -1;
    private bool lastVisualClockwise;
    private float cachedVisualZ;
    private float cachedVisualScaleX;
    private Vector3 cachedVisualLocalPos;

    private bool jumpVisualActive;
    private Vector2? lastJumpProgressGoal;
    private float jumpVisualT;
    private float jumpVisualStartZ;
    private float jumpVisualEndZ;
    private float jumpVisualStartScaleX;
    private float jumpVisualEndScaleX;
    private Vector3 jumpVisualStartLocalPos;
    private Vector3 jumpVisualEndLocalPos;

    private struct EdgeVisualState
    {
        public float z;
        public float scaleX;
        public Vector3 localPos;
    }

    private const float JumpRotationEndFraction = 0.2f;

    private const float MinTravelDeltaSqr = 0.04f;

    protected override void Init()
    {
        spiderAI = new WolfSpiderUtilityAI(this);
        ai = spiderAI;
        motor = new WolfSpiderMotor(this);

        Arrived = true;
        IsJumping = false;
        IsCoolingDown = false;

        if (activityBounds.size.sqrMagnitude < 0.01f)
        {
            activityBounds = new Bounds(transform.position, new Vector3(12f, 8f, 1f));
        }

        EnsureActivityBoundsContainSpider();
        ResolveFlyLayerMask();
        RebuildPerceptionMask();
        CacheVisualBaseline();
        SnapToNearestSurface();
        spiderAni = GetComponent<WolfSpiderAni>();

        EnemyStompReceiver.Ensure(
            this,
            bodyVisual != null ? bodyVisual : transform,
            stompPlatformSize);
    }

    private void OnValidate()
    {
        ResolveFlyLayerMask();

        if (!Application.isPlaying)
        {
            EnsureActivityBoundsContainSpider();
        }

        if (Application.isPlaying)
        {
            RefreshStompPlatform();
        }
    }

    /// <summary>
    /// 狼蛛 world 坐标必须在 activityBounds 内；否则自动对齐 center（与 MoleCave 一致）。
    /// </summary>
    public void EnsureActivityBoundsContainSpider()
    {
        if (activityBounds.size.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 spiderWorldPos = transform.position;

        if (activityBounds.Contains(spiderWorldPos))
        {
            return;
        }

        Debug.LogWarning(
            $"WolfSpider「{name}」的 world 坐标 {spiderWorldPos} 不在 activityBounds 内，"
            + "已自动将 activityBounds.center 设为狼蛛位置。",
            this);

        activityBounds.center = spiderWorldPos;
    }

    public bool IsInsideActivityBounds(Vector2 point)
    {
        if (activityBounds.size.sqrMagnitude < 0.01f)
        {
            return true;
        }

        return activityBounds.Contains(point);
    }

    public void RefreshStompPlatform()
    {
        EnemyStompReceiver.Ensure(
            this,
            bodyVisual != null ? bodyVisual : transform,
            stompPlatformSize);
    }

    public void RebuildPerceptionMask()
    {
        PerceptionMask = playerLayer.value | flyLayer.value;
    }

    public int OverlapPreyNonAlloc(out Collider2D[] buffer)
    {
        buffer = overlapBuffer;
        float radius = detectRadius;

        if (PerceptionMask != 0)
        {
            return Physics2D.OverlapCircleNonAlloc(Position, radius, overlapBuffer, PerceptionMask);
        }

        return Physics2D.OverlapCircleNonAlloc(Position, radius, overlapBuffer);
    }

    public void ResolveFlyLayerMask()
    {
        int flyLayerIndex = LayerMask.NameToLayer("fly");

        if (flyLayerIndex >= 0)
        {
            flyLayer = 1 << flyLayerIndex;
        }

        RebuildPerceptionMask();
    }

    public void SnapToNearestSurface()
    {
        SurfaceSnapResult snap = WolfSpiderSurfaceProbe.SnapToContourSurface(
            Position,
            surfaceSnapMaxDistance,
            0f,
            Position);

        if (!snap.success)
        {
            snap = WolfSpiderSurfaceProbe.SnapToSurface(
                Position,
                surfaceSnapMaxDistance,
                0f,
                Position);
        }

        if (!snap.success)
        {
            return;
        }

        transform.position = snap.point;
        travelClockwise = false;
        ApplySurfaceOrientation(snap.normal);
    }

    public void SyncContourEdgeState()
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            HasEdge = false;
            return;
        }

        EdgeIndex = SurfaceEdgePath.FindEdgeIndexForStandPoint(mgr, Position);
        CurrentEdge = mgr.GetEdge(EdgeIndex);
        HasEdge = true;
    }

    public void NotifyAttackPerformed()
    {
        if (spiderAI != null)
        {
            spiderAI.NotifyAttackPerformed();
        }
    }

    internal void SetPerformingAttackAnim(bool active)
    {
        IsPerformingAttackAnim = active;
    }

    public void NotifyJumpStarted()
    {
        spiderAni?.NotifyJumpStarted();

        EnemyWolfSpiderAudioEmitter audioEmitter = GetComponent<EnemyWolfSpiderAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.PlayJump();
        }
    }

    public void NotifyJumpEnded()
    {
        spiderAni?.NotifyJumpEnded();
    }

    public void NotifyAttackStarted()
    {
        spiderAni?.NotifyAttackStarted();

        EnemyWolfSpiderAudioEmitter audioEmitter = GetComponent<EnemyWolfSpiderAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.PlayAttack();
        }
    }

    public void NotifyAttackAnimEnded()
    {
        spiderAni?.NotifyAttackAnimEnded();
    }

    public void ArmPostLandJumpCooldown()
    {
        PostLandJumpCooldownTimer = postLandJumpCooldown;
    }

    public void TickPostLandJumpCooldown(float deltaTime)
    {
        if (PostLandJumpCooldownTimer <= 0f)
        {
            return;
        }

        PostLandJumpCooldownTimer -= deltaTime;

        if (PostLandJumpCooldownTimer < 0f)
        {
            PostLandJumpCooldownTimer = 0f;
        }
    }

    public void NotifyJumpTargetRejected()
    {
        JumpTargetRejected = true;
    }

    public void NotifySuccessfulLanding(Vector2 landPoint, Vector2 jumpOrigin)
    {
        if (spiderAI != null)
        {
            spiderAI.NotifySuccessfulLanding(landPoint, jumpOrigin);
        }
    }

    public bool ConsumeJumpTargetRejected()
    {
        if (!JumpTargetRejected)
        {
            return false;
        }

        JumpTargetRejected = false;
        return true;
    }

    public void ApplySurfaceOrientation(Vector2 normal)
    {
        ApplySurfaceOrientation(normal, null, null, null);
    }

    public void ApplySurfaceOrientation(
        Vector2 normal,
        Vector2? jumpOrigin,
        Vector2? landPoint,
        Vector2? progressGoal = null)
    {
        CurrentSurfaceNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector2.up;
        transform.rotation = Quaternion.identity;
        SyncContourEdgeState();

        if (HasEdge && jumpOrigin.HasValue && landPoint.HasValue)
        {
            TileMapGuideManager mgr = TileMapGuideManager.Instance;

            if (mgr != null)
            {
                Vector2? facingGoal = progressGoal ?? lastJumpProgressGoal;

                travelClockwise = SurfaceCrawlerVisual.ComputeTravelClockwiseForLanding(
                    mgr,
                    EdgeIndex,
                    jumpOrigin.Value,
                    landPoint.Value,
                    travelClockwise,
                    facingGoal);
            }
        }

        ApplyEdgeVisual(force: true);
    }

    private EdgeVisualState ComputeEdgeVisualState(
        int edgeIndex,
        Edge edge,
        bool clockwise,
        Vector2 onEdgeWorldPoint)
    {
        SurfaceCrawlerVisual.ComputeSurfaceWalkerVisual(
            edgeIndex,
            edge,
            baseVisualScale,
            clockwise,
            visualRotationOffset,
            onEdgeWorldPoint,
            out float z,
            out float scaleX);

        TileMapGuideManager mgr = TileMapGuideManager.Instance;
        Vector2 normal = mgr != null
            ? mgr.GetEdgeAirNormal(edge)
            : CurrentSurfaceNormal;
        Vector3 localPos = (Vector3)(normal.normalized * visualSurfaceOffset);

        return new EdgeVisualState
        {
            z = z,
            scaleX = scaleX,
            localPos = localPos
        };
    }

    private void ApplyBodyVisualState(in EdgeVisualState state)
    {
        if (bodyVisual == null)
        {
            return;
        }

        transform.rotation = Quaternion.identity;
        Vector3 euler = bodyVisual.localEulerAngles;
        bodyVisual.localEulerAngles = new Vector3(euler.x, euler.y, state.z);
        bodyVisual.localScale = new Vector3(
            state.scaleX,
            -Mathf.Abs(baseVisualScale.y),
            baseVisualScale.z);
        bodyVisual.localPosition = state.localPos;
    }

    private bool TryResolveLandingVisual(
        Vector2 landingPoint,
        Vector2 jumpOrigin,
        Vector2? progressGoal,
        out SurfaceSnapResult snap,
        out bool landingClockwise,
        out EdgeVisualState visual)
    {
        snap = WolfSpiderSurfaceProbe.SnapToContourSurface(
            landingPoint,
            surfaceSnapMaxDistance,
            0f,
            jumpOrigin);

        if (!snap.success)
        {
            snap = WolfSpiderSurfaceProbe.SnapToSurface(
                landingPoint,
                surfaceSnapMaxDistance,
                0f,
                jumpOrigin);
        }

        landingClockwise = travelClockwise;
        visual = default;

        if (!snap.success)
        {
            return false;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return false;
        }

        int edgeIndex = SurfaceEdgePath.FindEdgeIndexForStandPoint(mgr, snap.point);

        if (edgeIndex < 0)
        {
            return false;
        }

        Edge edge = mgr.GetEdge(edgeIndex);
        landingClockwise = SurfaceCrawlerVisual.ComputeTravelClockwiseForLanding(
            mgr,
            edgeIndex,
            jumpOrigin,
            snap.point,
            travelClockwise,
            progressGoal);

        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(
            snap.point,
            edge.a,
            edge.b);
        visual = ComputeEdgeVisualState(edgeIndex, edge, landingClockwise, onEdge);
        return true;
    }

    /// <summary>
    /// 沿当前边切线更新行进方向（参考 SurfaceWalker 的 scale.x / rotation.z）。
    /// </summary>
    public void ApplyTravelFacing(Vector2 worldPoint)
    {
        if (!HasEdge)
        {
            return;
        }

        Vector2 delta = worldPoint - GetOnEdgeWorldPosition();

        if (delta.sqrMagnitude < MinTravelDeltaSqr)
        {
            return;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr != null)
        {
            travelClockwise = SurfaceCrawlerVisual.ComputeTravelClockwiseForLanding(
                mgr,
                EdgeIndex,
                GetOnEdgeWorldPosition(),
                worldPoint,
                travelClockwise);
        }

        ApplyEdgeVisual(force: true);
    }

    public Vector2 GetOnEdgeWorldPosition()
    {
        if (!HasEdge)
        {
            return Position;
        }

        return SurfaceEdgeTraversal.ClosestPointOnSegment(
            Position,
            CurrentEdge.a,
            CurrentEdge.b);
    }

    public void ApplyEdgeVisual(bool force = false)
    {
        if (bodyVisual == null)
        {
            return;
        }

        transform.rotation = Quaternion.identity;

        if (!HasEdge)
        {
            bodyVisual.localRotation = Quaternion.identity;
            bodyVisual.localPosition = new Vector3(0f, visualSurfaceOffset, 0f);
            bodyVisual.localScale = baseVisualScale;
            return;
        }

        bool needsRecalc = force
            || EdgeIndex != lastVisualEdgeIndex
            || travelClockwise != lastVisualClockwise;

        if (needsRecalc)
        {
            EdgeVisualState state = ComputeEdgeVisualState(
                EdgeIndex,
                CurrentEdge,
                travelClockwise,
                GetOnEdgeWorldPosition());

            cachedVisualZ = state.z;
            cachedVisualScaleX = state.scaleX;
            cachedVisualLocalPos = state.localPos;

            lastVisualEdgeIndex = EdgeIndex;
            lastVisualClockwise = travelClockwise;
        }

        ApplyBodyVisualState(new EdgeVisualState
        {
            z = cachedVisualZ,
            scaleX = cachedVisualScaleX,
            localPos = cachedVisualLocalPos
        });

        TileMapGuideManager mgr = TileMapGuideManager.Instance;
        Vector2 normal = mgr != null
            ? mgr.GetEdgeAirNormal(CurrentEdge)
            : CurrentSurfaceNormal;
        CurrentSurfaceNormal = normal.normalized;
    }

    /// <summary>
    /// 起跳时记录当前朝向，并预计算落点边的目标朝向。
    /// </summary>
    public void PrepareJumpVisual(Vector2 landingPoint, Vector2 jumpOrigin, Vector2? progressGoal = null)
    {
        if (bodyVisual == null)
        {
            jumpVisualActive = false;
            return;
        }

        lastJumpProgressGoal = progressGoal ?? landingPoint;

        if (!TryResolveLandingVisual(
                landingPoint,
                jumpOrigin,
                lastJumpProgressGoal,
                out _,
                out _,
                out EdgeVisualState endVisual))
        {
            jumpVisualActive = false;
            return;
        }

        jumpVisualStartZ = bodyVisual.localEulerAngles.z;
        jumpVisualStartScaleX = bodyVisual.localScale.x;
        jumpVisualStartLocalPos = bodyVisual.localPosition;
        jumpVisualEndZ = endVisual.z;
        jumpVisualEndScaleX = endVisual.scaleX;
        jumpVisualEndLocalPos = endVisual.localPos;

        jumpVisualT = 0f;
        jumpVisualActive = true;
    }

    public void SetJumpVisualProgress(float t)
    {
        jumpVisualT = Mathf.Clamp01(t);
    }

    public void ClearJumpVisual()
    {
        jumpVisualActive = false;
        lastJumpProgressGoal = null;
    }

    private void ApplyJumpVisual()
    {
        if (!jumpVisualActive || bodyVisual == null)
        {
            return;
        }

        float blendT = 1f;

        if (jumpVisualT < JumpRotationEndFraction)
        {
            blendT = jumpVisualT / JumpRotationEndFraction;
        }

        EdgeVisualState state = new EdgeVisualState
        {
            z = Mathf.LerpAngle(jumpVisualStartZ, jumpVisualEndZ, blendT),
            scaleX = Mathf.Lerp(jumpVisualStartScaleX, jumpVisualEndScaleX, blendT),
            localPos = Vector3.Lerp(jumpVisualStartLocalPos, jumpVisualEndLocalPos, blendT)
        };

        ApplyBodyVisualState(state);
    }

    /// <summary>
    /// 落地吸附与视觉对齐，与 PrepareJumpVisual 使用同一套解析逻辑。
    /// </summary>
    public bool TryCompleteJumpLanding(Vector2 landHint, Vector2 jumpOrigin)
    {
        if (!TryResolveLandingVisual(
                landHint,
                jumpOrigin,
                lastJumpProgressGoal,
                out SurfaceSnapResult snap,
                out bool landingClockwise,
                out EdgeVisualState visual))
        {
            ClearJumpVisual();
            return false;
        }

        transform.position = snap.point;
        CurrentSurfaceNormal = snap.normal.sqrMagnitude > 0.0001f
            ? snap.normal.normalized
            : Vector2.up;
        transform.rotation = Quaternion.identity;
        travelClockwise = landingClockwise;
        SyncContourEdgeState();

        cachedVisualZ = visual.z;
        cachedVisualScaleX = visual.scaleX;
        cachedVisualLocalPos = visual.localPos;
        lastVisualEdgeIndex = EdgeIndex;
        lastVisualClockwise = travelClockwise;

        ApplyBodyVisualState(visual);
        ClearJumpVisual();
        return true;
    }

    public bool IsFlyCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(flyTag) && collider.CompareTag(flyTag))
        {
            return true;
        }

        if (flyLayer.value != 0 && (flyLayer.value & (1 << collider.gameObject.layer)) != 0)
        {
            return true;
        }

        return false;
    }

    public bool IsPlayerCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        return playerLayer.value != 0 && (playerLayer.value & (1 << collider.gameObject.layer)) != 0;
    }

    public void PerformAttack(Transform focusTarget = null)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(Position, attackRange, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || !IsPlayerCollider(hit))
            {
                continue;
            }

            PlayerVitals vitals = MonsterPlayerDamage.ResolveVitals(hit);

            MonsterPlayerDamage.TryDealDamage(vitals, attackDamage);
        }
    }

    public void LogDebug(string message)
    {
        if (!enableDebugLog)
        {
            return;
        }

        Debug.Log($"[WolfSpider {name}] {message}", this);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || !Application.isPlaying)
        {
            return;
        }

        DrawDebugGizmosInternal(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebugGizmosInternal(true);
    }

    private void DrawDebugGizmosInternal(bool selectedOnlyExtra)
    {
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (activityBounds.size.sqrMagnitude > 0.01f)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(activityBounds.center, activityBounds.size);
        }

        DrawStompPlatformGizmo();

        if (DebugHasPrey)
        {
            Gizmos.color = DebugPreyIsFly ? Color.cyan : Color.magenta;
            Gizmos.DrawLine(transform.position, DebugPreyPosition);
            Gizmos.DrawWireSphere(DebugPreyPosition, 0.25f);
            Gizmos.DrawSphere(DebugPreyPosition, 0.12f);
        }

        Gizmos.color = CurrentBehavior switch
        {
            WolfSpiderBehavior.Hunt => Color.yellow,
            WolfSpiderBehavior.Attack => Color.red,
            _ => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, 0.22f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(DebugTarget, 0.18f);

        if (DebugCandidatePoints.Count > 0)
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);

            foreach (Vector2 candidate in DebugCandidatePoints)
            {
                Gizmos.DrawWireSphere(candidate, 0.1f);
                Gizmos.DrawLine(transform.position, candidate);
            }
        }

        if (DebugArcSamples.Count > 1)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);

            for (int i = 0; i < DebugArcSamples.Count - 1; i++)
            {
                Gizmos.DrawLine(DebugArcSamples[i], DebugArcSamples[i + 1]);
            }
        }

        if (DebugPath == null || DebugPath.Count < 2)
        {
            return;
        }

        Gizmos.color = Color.green;

        for (int i = 0; i < DebugPath.Count - 1; i++)
        {
            Gizmos.DrawLine(DebugPath[i], DebugPath[i + 1]);
        }

        Gizmos.color = Color.yellow;

        foreach (Vector2 pathPoint in DebugPath)
        {
            Gizmos.DrawSphere(pathPoint, 0.08f);
        }

        if (selectedOnlyExtra)
        {
            Gizmos.color = new Color(0f, 1f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, minJumpDist);
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, maxJumpDist);
        }
    }

    public void AttractToMeatBait(Vector2 myMeatBaitPosition)
    {
        spiderAI?.ForcePerceptionRefresh();
    }

    public void AttractToToyCar(Vector2 myToyCarPosition)
    {
        spiderAI?.ForcePerceptionRefresh();
    }

    public void FleeFromTorch(Vector2 torchPosition)
    {
        spiderAI?.NotifyRepelledByTorch(torchPosition);
    }

    private void DrawStompPlatformGizmo()
    {
        Transform anchor = bodyVisual != null ? bodyVisual : transform;
        Transform platform = anchor.Find("StompPlatform");

        if (platform == null)
        {
            return;
        }

        BoxCollider2D box = platform.GetComponent<BoxCollider2D>();

        if (box == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
        Vector3 worldCenter = platform.TransformPoint(box.offset);
        Vector3 worldSize = Vector3.Scale(box.size, platform.lossyScale);
        Gizmos.matrix = Matrix4x4.TRS(worldCenter, platform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, worldSize);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void LateUpdate()
    {
        if (IsStompPaused)
        {
            return;
        }

        if (IsJumping && jumpVisualActive)
        {
            ApplyJumpVisual();
            return;
        }

        ApplyEdgeVisual();
    }

    private void CacheVisualBaseline()
    {
        if (bodyVisual == null)
        {
            bodyVisual = transform;
        }

        SurfaceCrawlerVisual.CacheBaseScale(bodyVisual, ref baseVisualScale);
    }
}
