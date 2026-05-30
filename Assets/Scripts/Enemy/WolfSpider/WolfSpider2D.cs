using System.Collections.Generic;
using UnityEngine;

public class WolfSpider2D : MonsterBase
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
    public LayerMask playerLayer;
    [Tooltip("Layer 名称 fly；若尚未在 TagManager 配置，可留空并依赖 Fly 组件检测")]
    public LayerMask flyLayer;
    public string flyTag = "Fly";

    [Header("Combat")]
    public float attackRange = 1.2f;
    public float attackStiffDuration = 1f;
    public int attackDamage = 10;

    [Header("Aggro")]
    public float aggroMemoryDuration = 3f;

    [Header("Idle")]
    public Bounds activityBounds;
    public float idleJumpInterval = 2f;

    [Header("Surface")]
    public float surfaceSnapMaxDistance = 0.85f;
    public float visualSurfaceOffset = 0.1f;
    public Transform bodyVisual;

    [Header("Debug")]
    public bool enableDebugLog = false;
    public bool drawDebugGizmos = true;

    public int PerceptionMask { get; private set; }

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];

    public bool IsJumping { get; set; }
    public bool IsCoolingDown { get; set; }
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

    private WolfSpiderUtilityAI spiderAI;

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

        ResolveFlyLayerMask();
        RebuildPerceptionMask();
        SnapToNearestSurface();
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
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr != null && mgr.TryGetFloorTop(Position, out Vector2 floorPoint, visualSurfaceOffset))
        {
            transform.position = floorPoint;
            ApplySurfaceOrientation(Vector2.up);
            return;
        }

        SurfaceSnapResult snap = WolfSpiderSurfaceProbe.SnapToFloorSurface(
            Position,
            surfaceSnapMaxDistance,
            visualSurfaceOffset
        );

        if (!snap.success)
        {
            snap = WolfSpiderSurfaceProbe.SnapToSurface(
                Position,
                surfaceSnapMaxDistance,
                visualSurfaceOffset,
                Position
            );
        }

        if (!snap.success)
        {
            return;
        }

        transform.position = snap.point;
        ApplySurfaceOrientation(snap.normal);
    }

    public void NotifyAttackPerformed()
    {
        if (spiderAI != null)
        {
            spiderAI.NotifyAttackPerformed();
        }
    }

    public void NotifyJumpTargetRejected()
    {
        JumpTargetRejected = true;
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
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = Vector2.up;
        }

        CurrentSurfaceNormal = normal.normalized;

        if (bodyVisual != null)
        {
            Vector3 scale = bodyVisual.localScale;
            scale.y = Mathf.Abs(scale.y);
            scale.x = Mathf.Abs(scale.x);
            bodyVisual.localScale = scale;
        }

        if (CurrentSurfaceNormal.y > 0.55f)
        {
            transform.rotation = Quaternion.identity;

            if (bodyVisual != null)
            {
                bodyVisual.localRotation = Quaternion.identity;
                bodyVisual.localPosition = new Vector3(0f, visualSurfaceOffset, 0f);
            }

            return;
        }

        if (bodyVisual != null)
        {
            bodyVisual.localPosition = (Vector3)(CurrentSurfaceNormal * visualSurfaceOffset);
        }

        float angle = Mathf.Atan2(CurrentSurfaceNormal.y, CurrentSurfaceNormal.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void FaceToward(Vector2 worldPoint)
    {
        Vector2 dir = worldPoint - Position;

        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
        if (focusTarget != null)
        {
            FaceToward(focusTarget.position);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(Position, attackRange, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || !IsPlayerCollider(hit))
            {
                continue;
            }

            PlayerVitals vitals = hit.GetComponent<PlayerVitals>();

            if (vitals != null && !vitals.IsDead)
            {
                vitals.ReduceHealth(attackDamage);
            }
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
}
