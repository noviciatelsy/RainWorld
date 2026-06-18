using UnityEngine;

public class Robot2D : MonsterBase, IContactWithLiquid, IAttractedByMilk
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float chargeSpeed = 7f;
    [Tooltip("冲刺固定水平距离")]
    public float chargeDistance = 6f;
    public float arriveThreshold = 0.08f;
    [Tooltip("脚底相对格子中心的 Y 偏移（鼹鼠路点约 -0.45；机器人 pivot 不同默认 -0.15，约高 0.3")]
    public float feetYOffset = -0.15f;

    [Header("Areas")]
    [Tooltip("Idle 巡逻范围（世界坐标 Center/Size，固定区域，需在 Inspector 手动配置）")]
    public Bounds idleBounds;
    [Tooltip("感知/冲刺触发范围（世界坐标 Center/Size，固定区域，需在 Inspector 手动配置）")]
    public Bounds activeBounds;

    [Header("Perception")]
    public LayerMask playerLayer;
    [Tooltip("冲刺撞停时检测可破坏墙的 Layer（默认 Ground + Platform）")]
    public LayerMask destructibleWallLayer;

    [Header("Combat")]
    public float attackRange = 0.9f;
    public int attackDamage = 12;
    [Tooltip("冲刺结束后原地停止时间")]
    public float recoverDuration = 1f;
    [Tooltip("单次冲刺最长持续时间，防止一直追")]
    public float chargeMaxDuration = 3f;
    [Tooltip("冲刺撞停时，朝冲刺方向探测可破坏墙的距离")]
    public float chargeDestructibleWallProbeDistance = 0.85f;
    [Tooltip("探测盒半高（世界单位）")]
    public float chargeDestructibleWallProbeHalfHeight = 0.55f;

    [Header("Visual")]
    public Transform bodyVisual;

    [Header("Drink Attract")]
    [SerializeField] private RobotDrinkCollector drinkCollector;

    public RobotDrinkCollector DrinkCollector => drinkCollector;

    [Header("Debug")]
    public bool drawDebugGizmos = true;

    public RobotBehavior CurrentBehavior { get; set; } = RobotBehavior.Idle;

    public bool IsDrinkFrozen { get; private set; }

    public bool DebugHasPlayer { get; private set; }
    public Vector2 DebugPlayerPosition { get; private set; }

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];
    private float baseVisualScaleX = 1f;

    protected override void Init()
    {
        ai = new RobotUtilityAI(this);
        motor = new RobotMotor(this);

        Arrived = true;

        Transform visual = bodyVisual != null ? bodyVisual : transform;
        baseVisualScaleX = Mathf.Abs(visual.localScale.x);

        if (baseVisualScaleX < 0.001f)
        {
            baseVisualScaleX = 1f;
        }

        EnsureDefaultAreas();
        ResolvePlayerLayerMask();
        ResolveDestructibleWallLayerMask();
        EnsureEnemyLayer();
        EnsureDrinkCollector();
        SnapFeetToGround();

        Arrived = true;

        EnemyStompReceiver.Ensure(
            this,
            bodyVisual != null ? bodyVisual : transform,
            new Vector2(0.7f, 0.12f));
    }

    protected override void FixedUpdate()
    {
        if (IsDrinkFrozen || IsStompPaused || ai == null || motor == null)
        {
            return;
        }

        IIntent intent = ai.Evaluate(this);
        motor.Execute(this, intent);
    }

    public void EnterDrinkFrozenState()
    {
        if (IsDrinkFrozen)
        {
            return;
        }

        IsDrinkFrozen = true;
        Arrived = true;
        CurrentBehavior = RobotBehavior.Recover;
        DebugPath = null;
        DebugTarget = Position;
        CurrentTarget = Position;
    }

    private void OnValidate()
    {
        EnsureDefaultAreas();
        ResolvePlayerLayerMask();
        ResolveDestructibleWallLayerMask();
    }

    public void EnsureDefaultAreas()
    {
        if (idleBounds.size.sqrMagnitude < 0.01f)
        {
            idleBounds = new Bounds(transform.position, new Vector3(12f, 4f, 0.1f));
        }

        if (activeBounds.size.sqrMagnitude < 0.01f)
        {
            activeBounds = new Bounds(transform.position, new Vector3(8f, 4f, 0.1f));
        }
    }

    public void SnapFeetToGround()
    {
        transform.position = RobotGroundPath.SnapToFlatGround(Position, feetYOffset);
    }

    public void ContactWithLiquid()
    {
        if (IsDrinkFrozen)
        {
            return;
        }

        drinkCollector?.OnLiquidContact();
    }

    public void AttractedByMilk(Vector2 milkPosition)
    {
        if (IsDrinkFrozen)
        {
            return;
        }

        drinkCollector?.NotifyMilkDropped(milkPosition);
    }

    private void EnsureEnemyLayer()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (enemyLayer < 0 || gameObject.layer == enemyLayer)
        {
            return;
        }

        gameObject.layer = enemyLayer;
    }

    private void EnsureDrinkCollector()
    {
        if (drinkCollector == null)
        {
            drinkCollector = GetComponent<RobotDrinkCollector>();
        }

        if (drinkCollector == null)
        {
            drinkCollector = gameObject.AddComponent<RobotDrinkCollector>();
        }
    }

    public void ResolvePlayerLayerMask()
    {
        if (playerLayer.value != 0)
        {
            return;
        }

        int playerLayerIndex = LayerMask.NameToLayer("Player");

        if (playerLayerIndex >= 0)
        {
            playerLayer = 1 << playerLayerIndex;
        }
    }

    public void ResolveDestructibleWallLayerMask()
    {
        if (destructibleWallLayer.value != 0)
        {
            return;
        }

        int mask = 0;
        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        int platformLayerIndex = LayerMask.NameToLayer("Platform");

        if (groundLayerIndex >= 0)
        {
            mask |= 1 << groundLayerIndex;
        }

        if (platformLayerIndex >= 0)
        {
            mask |= 1 << platformLayerIndex;
        }

        destructibleWallLayer = mask;
    }

    /// <summary>
    /// OverlapBox 的 size 为全宽/全高（非 halfExtents）。
    /// </summary>
    public Vector2 GetActiveBoxSize()
    {
        return new Vector2(
            Mathf.Max(activeBounds.size.x, 0.5f),
            Mathf.Max(activeBounds.size.y, 0.5f)
        );
    }

    public float GetActiveSenseRadius()
    {
        Vector2 size = GetActiveBoxSize();
        return Mathf.Max(size.x, size.y) * 0.5f;
    }

    /// <summary>
    /// 世界坐标下的固定 active 区域（感知/冲刺触发）。
    /// </summary>
    public Bounds GetActiveBoundsWorld()
    {
        return activeBounds;
    }

    public bool IsInsideIdleBounds(Vector2 point)
    {
        return RobotGroundPath.IsInsideBoundsXY(idleBounds, point);
    }

    public bool IsInsideActiveBounds(Vector2 point)
    {
        return RobotGroundPath.IsInsideBoundsXY(activeBounds, point);
    }

    public int OverlapPlayerNonAlloc(out Collider2D[] buffer)
    {
        buffer = overlapBuffer;
        Vector2 boxSize = GetActiveBoxSize();
        float radius = GetActiveSenseRadius();
        int count = 0;

        Vector2 senseCenter = activeBounds.center;

        if (playerLayer.value != 0)
        {
            count = Physics2D.OverlapBoxNonAlloc(
                senseCenter,
                boxSize,
                0f,
                overlapBuffer,
                playerLayer
            );
        }
        else
        {
            count = Physics2D.OverlapBoxNonAlloc(senseCenter, boxSize, 0f, overlapBuffer);
        }

        if (count > 0)
        {
            return count;
        }

        if (playerLayer.value != 0)
        {
            return Physics2D.OverlapCircleNonAlloc(
                senseCenter,
                radius,
                overlapBuffer,
                playerLayer
            );
        }

        return Physics2D.OverlapCircleNonAlloc(senseCenter, radius, overlapBuffer);
    }

    public bool IsOnPlatformSurface()
    {
        return RobotGroundPath.TryResolveSurfaceSupport(Position, feetYOffset, out RobotGroundPath.RobotSurfaceSupport support)
            && support.IsPlatform;
    }

    public bool IsPlayerCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.GetComponentInParent<Player>() != null)
        {
            return true;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        return playerLayer.value != 0
            && (playerLayer.value & (1 << collider.gameObject.layer)) != 0;
    }

    public Transform FindClosestPlayerTransform()
    {
        DebugHasPlayer = false;

        int hitCount = OverlapPlayerNonAlloc(out Collider2D[] hits);
        Transform closest = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || !IsPlayerCollider(hit))
            {
                continue;
            }

            Player player = hit.GetComponentInParent<Player>();

            if (player == null || !PlayerInvisibilityPerception.IsPlayerDetectable(player))
            {
                continue;
            }

            Vector2 playerPos = player.transform.position;

            if (!IsInsideActiveBounds(playerPos))
            {
                continue;
            }

            float distSqr = (playerPos - Position).sqrMagnitude;

            if (distSqr >= bestDistSqr)
            {
                continue;
            }

            bestDistSqr = distSqr;
            closest = player.transform;
        }

        if (closest == null && PlayerManager.Instance != null)
        {
            Player scenePlayer = PlayerManager.Instance.TryGetCurrentPlayer();

            if (scenePlayer != null && PlayerInvisibilityPerception.IsPlayerDetectable(scenePlayer))
            {
                Vector2 playerPos = scenePlayer.transform.position;

                if (IsInsideActiveBounds(playerPos))
                {
                    closest = scenePlayer.transform;
                }
            }
        }

        if (closest != null)
        {
            DebugHasPlayer = true;
            DebugPlayerPosition = closest.position;
        }

        return closest;
    }

    public bool TryDamagePlayer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return false;
        }

        if (((Vector2)playerTransform.position - Position).sqrMagnitude > attackRange * attackRange)
        {
            return false;
        }

        Player player = playerTransform.GetComponentInParent<Player>();

        if (player == null)
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();

        if (vitals == null || vitals.IsDead)
        {
            return false;
        }

        if (GameStateManager.Instance != null
            && GameStateManager.Instance.currentGameState != GameState.Game)
        {
            return false;
        }

        vitals.ReduceHealth(attackDamage);
        return true;
    }

    public void UpdateFacingToward(Vector2 worldPoint)
    {
        float deltaX = worldPoint.x - Position.x;

        if (Mathf.Abs(deltaX) < 0.01f)
        {
            return;
        }

        Transform visual = bodyVisual != null ? bodyVisual : transform;
        Vector3 scale = visual.localScale;
        float absX = Mathf.Max(Mathf.Abs(scale.x), baseVisualScaleX * 0.5f);
        scale.x = absX * (deltaX >= 0f ? 1f : -1f);
        visual.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebugGizmos(1f);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        DrawDebugGizmos(Application.isPlaying ? 0.85f : 0.55f);
    }

    private void DrawDebugGizmos(float alpha)
    {
        EnsureDefaultAreas();

        Gizmos.color = new Color(0.2f, 1f, 0.35f, alpha);
        Gizmos.DrawWireCube(idleBounds.center, idleBounds.size);

        Gizmos.color = new Color(1f, 0.55f, 0.15f, alpha * 0.85f);
        Gizmos.DrawWireCube(activeBounds.center, activeBounds.size);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, alpha * 0.6f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (DebugPath != null && DebugPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            Vector2 from = transform.position;

            for (int i = 0; i < DebugPath.Count; i++)
            {
                Gizmos.DrawLine(from, DebugPath[i]);
                Gizmos.DrawWireSphere(DebugPath[i], 0.12f);
                from = DebugPath[i];
            }
        }

        if (DebugTarget.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, DebugTarget);
            Gizmos.DrawWireSphere(DebugTarget, 0.1f);
        }

        if (DebugHasPlayer)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, DebugPlayerPosition);
            Gizmos.DrawWireSphere(DebugPlayerPosition, 0.25f);
        }

        if (drinkCollector != null)
        {
            drinkCollector.DrawDetectBoundsGizmo(alpha);
        }
    }
}
