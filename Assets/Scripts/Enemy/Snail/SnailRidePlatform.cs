using UnityEngine;

/// <summary>
/// 蜗牛壳站立面：向下移动时若世界坐标下方检测区内有玩家则暂停蜗牛，玩家离开后继续。
/// </summary>
[DisallowMultipleComponent]
public class SnailRidePlatform : MovingGroundPlatform
{
    [SerializeField] private float movingEpsilon = 0.0001f;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float turnAngleThreshold = 0.5f;
    [SerializeField] private float turnSuspendDuration = 0.2f;

    [Header("Downward Player Gate (World Space)")]
    [SerializeField] private float downwardDetectDepth = 1.5f;
    [SerializeField] private float downwardDetectPaddingX = 0.35f;
    [SerializeField] private float minStompDownSpeed = 0.5f;

    private Snail2D snail;
    private PlayerControl cachedPlayer;
    private Vector2 lastWorldPosition;
    private float lastVisualAngle;
    private Vector3 lastVisualLocalScale;
    private float suspendTimer;
    private bool downwardPauseActive;
    private bool platformIntelUnlocked;

    protected override void Awake()
    {
        base.Awake();
        snail = GetComponentInParent<Snail2D>();
        lastWorldPosition = rb.position;
        CacheVisualBaseline();
    }

    private void OnEnable()
    {
        lastWorldPosition = transform.position;
        if (rb != null)
        {
            rb.position = lastWorldPosition;
        }

        suspendTimer = 0f;
        downwardPauseActive = false;
        cachedPlayer = null;
        CacheVisualBaseline();
        ApplyDownwardPause(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryUnlockPlatformIntelligence(collision);
    }

    private void TryUnlockPlatformIntelligence(Collision2D collision)
    {
        if (platformIntelUnlocked || collision == null)
        {
            return;
        }

        Collider2D other = collision.collider;

        if (other == null || other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        if (collision.relativeVelocity.y > -minStompDownSpeed)
        {
            return;
        }

        if (platformCollider != null
            && other.bounds.min.y < platformCollider.bounds.center.y)
        {
            return;
        }

        platformIntelUnlocked = true;
        EnemyIntelligenceUnlockUtility.TryUnlockByName(EnemyIntelligenceNames.SnailPlatform);
    }

    public void PrepareBeforeMotor(IIntent intent)
    {
        UpdateDownwardPauseGate(intent);
    }

    public void SyncAfterMotor()
    {
        if (DetectVisualTurning())
        {
            suspendTimer = turnSuspendDuration;
        }

        suspendTimer -= Time.fixedDeltaTime;
        bool platformActive = suspendTimer <= 0f;

        if (platformCollider != null)
        {
            platformCollider.enabled = platformActive;
        }

        Vector2 worldPos = transform.position;
        rb.position = worldPos;

        if (!platformActive)
        {
            lastWorldPosition = worldPos;
            ClearFrameMotion();
            return;
        }

        Vector2 platformDelta = worldPos - lastWorldPosition;
        lastWorldPosition = worldPos;

        if (downwardPauseActive)
        {
            SetHeldMovingState();
            return;
        }

        if (platformDelta.sqrMagnitude > movingEpsilon)
        {
            SetFrameMotion(platformDelta);
        }
        else
        {
            ClearFrameMotion();
        }
    }

    private void UpdateDownwardPauseGate(IIntent intent)
    {
        if (downwardPauseActive)
        {
            if (!IsPlayerInWorldDownwardZone())
            {
                ApplyDownwardPause(false);
            }

            return;
        }

        if (WillMoveWorldDownward(intent) && IsPlayerInWorldDownwardZone())
        {
            ApplyDownwardPause(true);
        }
    }

    private void ApplyDownwardPause(bool paused)
    {
        downwardPauseActive = paused;
        if (snail != null)
        {
            snail.SetDownwardMovementPaused(paused);
        }
    }

    private bool WillMoveWorldDownward(IIntent intent)
    {
        if (snail == null)
        {
            return false;
        }

        Vector2 current = snail.Position;
        Vector2 next = snail.PredictPositionAfterStep(intent);

        if (next.y < current.y - movingEpsilon)
        {
            return true;
        }

        if (!snail.HasEdge)
        {
            return next.y < current.y - movingEpsilon;
        }

        Edge edge = snail.CurrentEdge;
        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(current, edge.a, edge.b);
        if (onEdge.y < current.y - movingEpsilon)
        {
            return true;
        }

        return next.y < onEdge.y - movingEpsilon;
    }

    private bool IsPlayerInWorldDownwardZone()
    {
        if (!GetWorldDownwardDetectBounds(out Bounds zone))
        {
            return false;
        }

        PlayerControl player = GetPlayer();
        if (player == null)
        {
            return false;
        }

        Vector2 point = player.transform.position;
        if (IsPointInsideZone(point, zone))
        {
            return true;
        }

        Collider2D[] colliders = player.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col == platformCollider)
            {
                continue;
            }

            if (zone.Intersects(col.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private PlayerControl GetPlayer()
    {
        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        cachedPlayer = Object.FindObjectOfType<PlayerControl>();
        return cachedPlayer;
    }

    private static bool IsPointInsideZone(Vector2 point, Bounds zone)
    {
        return point.x >= zone.min.x && point.x <= zone.max.x
            && point.y >= zone.min.y && point.y <= zone.max.y;
    }

    private bool GetWorldDownwardDetectBounds(out Bounds zone)
    {
        zone = default;
        if (!TryGetPlatformWorldFootprint(out float minX, out float maxX, out float minY, out _))
        {
            return false;
        }

        float width = maxX - minX + downwardDetectPaddingX * 2f;
        float centerX = (minX + maxX) * 0.5f;
        float centerY = minY - downwardDetectDepth * 0.5f;

        zone = new Bounds(new Vector3(centerX, centerY, 0f), new Vector3(width, downwardDetectDepth, 0.01f));
        return width > 0f && downwardDetectDepth > 0f;
    }

    private bool TryGetPlatformWorldFootprint(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = maxX = minY = maxY = 0f;

        if (platformCollider == null)
        {
            return false;
        }

        if (platformCollider is not BoxCollider2D box)
        {
            Bounds aabb = platformCollider.bounds;
            minX = aabb.min.x;
            maxX = aabb.max.x;
            minY = aabb.min.y;
            maxY = aabb.max.y;
            return true;
        }

        Vector2 half = box.size * 0.5f;
        Vector2 offset = box.offset;
        Vector2[] localCorners =
        {
            offset + new Vector2(-half.x, -half.y),
            offset + new Vector2(-half.x, half.y),
            offset + new Vector2(half.x, -half.y),
            offset + new Vector2(half.x, half.y)
        };

        Vector3 world0 = box.transform.TransformPoint(localCorners[0]);
        minX = maxX = world0.x;
        minY = maxY = world0.y;

        for (int i = 1; i < localCorners.Length; i++)
        {
            Vector3 world = box.transform.TransformPoint(localCorners[i]);
            minX = Mathf.Min(minX, world.x);
            maxX = Mathf.Max(maxX, world.x);
            minY = Mathf.Min(minY, world.y);
            maxY = Mathf.Max(maxY, world.y);
        }

        return true;
    }

    private void CacheVisualBaseline()
    {
        if (visualTransform == null && snail != null)
        {
            visualTransform = snail.bodyVisual;
        }

        if (visualTransform == null)
        {
            return;
        }

        lastVisualAngle = visualTransform.eulerAngles.z;
        lastVisualLocalScale = visualTransform.localScale;
    }

    private bool DetectVisualTurning()
    {
        if (visualTransform == null)
        {
            return false;
        }

        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(lastVisualAngle, visualTransform.eulerAngles.z));
        lastVisualAngle = visualTransform.eulerAngles.z;

        Vector3 localScale = visualTransform.localScale;
        float localScaleDeltaSqr = (localScale - lastVisualLocalScale).sqrMagnitude;
        lastVisualLocalScale = localScale;

        return angleDelta > turnAngleThreshold || localScaleDeltaSqr > 0.0001f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        if (!GetWorldDownwardDetectBounds(out Bounds zone))
        {
            return;
        }

        Gizmos.color = downwardPauseActive ? Color.red : new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawCube(zone.center, zone.size);
    }
#endif
}
