using System.Collections.Generic;
using UnityEngine;

public class WolfSpiderUtilityAI : IMonsterAI
{
    private readonly WolfSpider2D owner;

    private WolfSpiderIntent lastIssuedIntent;
    private bool hasIssuedIntent;

    private Transform currentPrey;
    private Vector2 lastKnownPreyPosition;
    private bool hasLastKnownPreyPosition;
    private float aggroMemoryTimer;

    private float idleTimer;
    private float perceptionTimer;
    private float pathPickTimer;
    private Vector2 lastPathGoal;
    private float postAttackRecoveryTimer;

    private Transform lastDebugPrey;
    private WolfSpiderBehavior lastDebugBehavior = WolfSpiderBehavior.Idle;

    private EnemyAttractionSource currentPreySource = EnemyAttractionSource.None;

    private Vector2 toyCarChasePoint;
    private bool hasToyCarChasePoint;
    private readonly List<Vector2> rejectedJumpTargets = new List<Vector2>();
    private bool hasLastVisitPoint;
    private Vector2 lastVisitPoint;
    private bool forceContourRepick;
    private int pathPickSeed;
    private float movementStuckTimer;
    private Vector2 lastMovementCheckPosition;

    private const int MaxRejectedJumpTargets = 6;
    private const float PreyGoalChangeThresholdSqr = 1.5f * 1.5f;
    private const float ArrivedThresholdSqr = 0.1f * 0.1f;

    public WolfSpiderUtilityAI(WolfSpider2D owner)
    {
        this.owner = owner;
        idleTimer = 0f;
        perceptionTimer = 0f;
        pathPickTimer = 0f;
        lastIssuedIntent = new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Idle,
            jumpTarget = owner.Position,
            focusTarget = null
        };
    }

    public IIntent Evaluate(MonsterBase ownerBase)
    {
        WolfSpider2D spider = ownerBase as WolfSpider2D;

        if (spider == null)
        {
            return lastIssuedIntent;
        }

        UpdatePerception(spider);

        if (spider.drawDebugGizmos)
        {
            UpdateDebugState(spider);
        }

        if (TryBuildTorchFleeIntent(spider, out WolfSpiderIntent torchFleeIntent))
        {
            lastIssuedIntent = torchFleeIntent;
            return lastIssuedIntent;
        }

        if (postAttackRecoveryTimer > 0f)
        {
            postAttackRecoveryTimer -= Time.fixedDeltaTime;
        }

        if (spider.ConsumeJumpTargetRejected())
        {
            HandleJumpTargetRejected(spider);
        }

        UpdateMovementStuck(spider);

        if (spider.IsJumping)
        {
            return GetHeldIntent(spider);
        }

        if (spider.IsCoolingDown || postAttackRecoveryTimer > 0f)
        {
            lastIssuedIntent = CreateIdleIntent(spider, spider.Position);
            spider.CurrentBehavior = WolfSpiderBehavior.Idle;
            hasIssuedIntent = true;
            return lastIssuedIntent;
        }

        WolfSpiderBehavior behavior = DecideBehavior(spider);
        spider.CurrentBehavior = behavior;

        switch (behavior)
        {
            case WolfSpiderBehavior.Attack:
                lastIssuedIntent = BuildAttackIntent(spider);
                break;

            case WolfSpiderBehavior.Hunt:
                lastIssuedIntent = BuildHuntIntent(spider);
                break;

            default:
                lastIssuedIntent = BuildIdleIntent(spider);
                break;
        }

        hasIssuedIntent = true;
        LogBehaviorChange(spider, behavior);
        return lastIssuedIntent;
    }

    public void NotifyAttackPerformed()
    {
        postAttackRecoveryTimer = owner.attackInterval;
        idleTimer = owner.idleJumpInterval;
        pathPickTimer = 0f;
    }

    public void ForcePerceptionRefresh()
    {
        perceptionTimer = 0f;
        pathPickTimer = 0f;
        idleTimer = 0f;
    }

    public void NotifyRepelledByTorch(Vector2 torchPosition)
    {
        ForcePerceptionRefresh();
        hasIssuedIntent = false;
    }

    private bool TryBuildTorchFleeIntent(WolfSpider2D spider, out WolfSpiderIntent fleeIntent)
    {
        fleeIntent = default;

        if (!TorchAvoidance.IsInsideAnyActiveTorch(spider.Position))
        {
            return false;
        }

        Vector2 fleeTarget = TorchAvoidance.GetFleePointAwayFromAllTorches(spider.Position);
        fleeIntent = CreateIdleIntent(spider, fleeTarget);
        spider.CurrentBehavior = WolfSpiderBehavior.Idle;
        spider.DebugPickReason = "TorchFlee";
        hasIssuedIntent = true;
        return true;
    }

    private void HandleJumpTargetRejected(WolfSpider2D spider)
    {
        float arriveSqr = JumpArriveThreshold * JumpArriveThreshold;
        float distSqr = (lastIssuedIntent.jumpTarget - spider.Position).sqrMagnitude;

        if (distSqr > arriveSqr)
        {
            RegisterRejectedJumpTarget(lastIssuedIntent.jumpTarget);
        }
        else if (rejectedJumpTargets.Count >= 6)
        {
            rejectedJumpTargets.Clear();
        }

        pathPickTimer = 0f;
        forceContourRepick = true;
        hasIssuedIntent = false;
        movementStuckTimer = 0f;

        spider.LogDebug("落点无效，重新选择跳跃目标。");
    }

    private const float JumpArriveThreshold = 0.05f;

    private void RegisterRejectedJumpTarget(Vector2 target)
    {
        TryAddRejectedJumpTarget(target);
        movementStuckTimer = 0f;
        forceContourRepick = true;
    }

    private bool TryAddRejectedJumpTarget(Vector2 target)
    {
        for (int i = 0; i < rejectedJumpTargets.Count; i++)
        {
            if ((rejectedJumpTargets[i] - target).sqrMagnitude <= 0.08f * 0.08f)
            {
                return false;
            }
        }

        rejectedJumpTargets.Add(target);

        while (rejectedJumpTargets.Count > MaxRejectedJumpTargets)
        {
            rejectedJumpTargets.RemoveAt(0);
        }

        return true;
    }

    public void NotifySuccessfulLanding(Vector2 landPoint, Vector2 jumpOrigin)
    {
        lastVisitPoint = jumpOrigin;
        hasLastVisitPoint = true;
        rejectedJumpTargets.Clear();
        forceContourRepick = false;

        if (hasIssuedIntent)
        {
            float snapTolSqr = owner.surfaceSnapMaxDistance * owner.surfaceSnapMaxDistance;

            if ((lastIssuedIntent.jumpTarget - landPoint).sqrMagnitude <= snapTolSqr)
            {
                lastIssuedIntent.jumpTarget = landPoint;
            }
        }
    }

    private void UpdateMovementStuck(WolfSpider2D spider)
    {
        if (spider.IsJumping || spider.IsCoolingDown)
        {
            movementStuckTimer = 0f;
            lastMovementCheckPosition = spider.Position;
            return;
        }

        float minJump = spider.minJumpDist + 0.02f;
        float minJumpSqr = minJump * minJump;

        if (hasIssuedIntent)
        {
            float targetDistSqr = (lastIssuedIntent.jumpTarget - spider.Position).sqrMagnitude;
            float snapTolSqr = spider.surfaceSnapMaxDistance * spider.surfaceSnapMaxDistance;

            if (targetDistSqr <= snapTolSqr)
            {
                movementStuckTimer = 0f;
                lastMovementCheckPosition = spider.Position;
                return;
            }

            if (targetDistSqr < minJumpSqr)
            {
                forceContourRepick = true;
                pathPickTimer = 0f;
                movementStuckTimer = 0f;
                lastMovementCheckPosition = spider.Position;
                return;
            }
        }

        if ((spider.Position - lastMovementCheckPosition).sqrMagnitude > 0.08f * 0.08f)
        {
            movementStuckTimer = 0f;
            lastMovementCheckPosition = spider.Position;
            return;
        }

        movementStuckTimer += Time.fixedDeltaTime;

        if (movementStuckTimer < spider.movementStuckTimeout)
        {
            return;
        }

        movementStuckTimer = 0f;
        lastMovementCheckPosition = spider.Position;
        forceContourRepick = true;
        pathPickTimer = 0f;

        if (hasIssuedIntent && (lastIssuedIntent.jumpTarget - spider.Position).sqrMagnitude >= minJumpSqr)
        {
            RegisterRejectedJumpTarget(lastIssuedIntent.jumpTarget);
        }

        if (rejectedJumpTargets.Count >= 4)
        {
            rejectedJumpTargets.Clear();
        }
    }

    private IIntent GetHeldIntent(WolfSpider2D spider)
    {
        if (!hasIssuedIntent)
        {
            lastIssuedIntent = BuildIdleIntent(spider);
            hasIssuedIntent = true;
        }

        return lastIssuedIntent;
    }

    private void UpdatePerception(WolfSpider2D spider)
    {
        perceptionTimer -= Time.fixedDeltaTime;
        float detectRadiusSqr = spider.detectRadius * spider.detectRadius;

        if (currentPrey != null)
        {
            if (!PlayerInvisibilityPerception.IsPlayerDetectable(currentPrey)
                && currentPrey.GetComponentInParent<Player>() != null)
            {
                currentPrey = null;
                currentPreySource = EnemyAttractionSource.None;
                aggroMemoryTimer = 0f;
            }
            else if (currentPrey.gameObject.activeInHierarchy)
            {
                lastKnownPreyPosition = currentPrey.position;

                if (((Vector2)currentPrey.position - spider.Position).sqrMagnitude <= detectRadiusSqr)
                {
                    hasLastKnownPreyPosition = true;
                    aggroMemoryTimer = spider.aggroMemoryDuration;
                }
            }
            else
            {
                currentPrey = null;
                currentPreySource = EnemyAttractionSource.None;
            }
        }

        if (perceptionTimer > 0f)
        {
            if (aggroMemoryTimer > 0f)
            {
                aggroMemoryTimer -= Time.fixedDeltaTime;
            }

            if (aggroMemoryTimer <= 0f)
            {
                currentPrey = null;
                currentPreySource = EnemyAttractionSource.None;
            }

            return;
        }

        perceptionTimer = spider.perceptionInterval;

        EnemyAttractionCapabilities capabilities =
            EnemyAttractionCapabilities.MeatBait
            | EnemyAttractionCapabilities.ToyCar
            | EnemyAttractionCapabilities.Fly
            | EnemyAttractionCapabilities.Player;

        if (EnemyAttractionUtility.TryResolveTarget(
                spider.Position,
                spider.detectRadius,
                capabilities,
                null,
                out EnemyAttractionTarget attraction)
            && attraction.Transform != null)
        {
            ApplyDetectedPrey(spider, attraction.Transform, attraction.Source);
            return;
        }

        if (ShouldKeepToyCarChaseWithoutPerception(spider))
        {
            aggroMemoryTimer = spider.aggroMemoryDuration;
            return;
        }

        if (aggroMemoryTimer > 0f)
        {
            aggroMemoryTimer -= spider.perceptionInterval;
            return;
        }

        if (currentPrey != null)
        {
            spider.LogDebug("目标丢失，仇恨计时结束，回到 Idle。");
        }

        currentPrey = null;
        currentPreySource = EnemyAttractionSource.None;
    }

    private void ApplyDetectedPrey(WolfSpider2D spider, Transform detected, EnemyAttractionSource source)
    {
        if (detected != null
            && detected.GetComponentInParent<Player>() != null
            && !PlayerInvisibilityPerception.IsPlayerDetectable(detected))
        {
            return;
        }

        bool preyChanged = currentPrey != detected || currentPreySource != source;

        if (preyChanged)
        {
            spider.LogDebug(
                $"发现目标: {detected.name} ({GetPreyTypeLabel(source, detected)})"
            );
            pathPickTimer = 0f;
            idleTimer = 0f;

            if (source != EnemyAttractionSource.ToyCar)
            {
                hasToyCarChasePoint = false;
            }
        }

        currentPrey = detected;
        currentPreySource = source;
        lastKnownPreyPosition = detected.position;
        hasLastKnownPreyPosition = true;
        aggroMemoryTimer = spider.aggroMemoryDuration;
    }

    private static string GetPreyTypeLabel(EnemyAttractionSource source, Transform detected)
    {
        switch (source)
        {
            case EnemyAttractionSource.MeatBait:
                return "MeatBait";
            case EnemyAttractionSource.ToyCar:
                return "ToyCar";
            case EnemyAttractionSource.Fly:
                return "Fly";
            case EnemyAttractionSource.Player:
                return "Player";
            default:
                return detected != null && detected.GetComponentInParent<Fly2D>() != null
                    ? "Fly"
                    : "Player";
        }
    }

    private void UpdateDebugState(WolfSpider2D spider)
    {
        spider.DebugHasPrey = currentPrey != null || (aggroMemoryTimer > 0f && hasLastKnownPreyPosition);
        spider.DebugAggroTimer = aggroMemoryTimer;
        spider.DebugPreyIsFly = currentPrey != null && currentPrey.GetComponentInParent<Fly2D>() != null;

        if (currentPrey != null)
        {
            spider.DebugPreyName = currentPrey.name;
            spider.DebugPreyPosition = currentPrey.position;
            lastDebugPrey = currentPrey;
            return;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            spider.DebugPreyName = lastDebugPrey != null ? $"{lastDebugPrey.name}(记忆)" : "LastKnown";
            spider.DebugPreyPosition = lastKnownPreyPosition;
            return;
        }

        spider.DebugPreyName = "None";
        spider.DebugPreyPosition = spider.Position;
    }

    private void LogBehaviorChange(WolfSpider2D spider, WolfSpiderBehavior behavior)
    {
        if (!spider.enableDebugLog || behavior == lastDebugBehavior)
        {
            return;
        }

        lastDebugBehavior = behavior;

        switch (behavior)
        {
            case WolfSpiderBehavior.Hunt:
                spider.LogDebug(
                    $"进入 Hunt，目标 {spider.DebugPreyName}，落点 {lastIssuedIntent.jumpTarget}，原因: {spider.DebugPickReason}"
                );
                break;
            case WolfSpiderBehavior.Attack:
                spider.LogDebug($"进入 Attack，原地咬 {spider.DebugPreyName}");
                break;
            case WolfSpiderBehavior.Idle:
                spider.LogDebug("进入 Idle");
                break;
        }
    }

    private WolfSpiderBehavior DecideBehavior(WolfSpider2D spider)
    {
        if (postAttackRecoveryTimer > 0f)
        {
            return WolfSpiderBehavior.Idle;
        }

        if (currentPrey != null)
        {
            if (CanAttack(spider, currentPrey.position))
            {
                return WolfSpiderBehavior.Attack;
            }

            return WolfSpiderBehavior.Hunt;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            if (CanAttack(spider, lastKnownPreyPosition))
            {
                return WolfSpiderBehavior.Attack;
            }

            return WolfSpiderBehavior.Hunt;
        }

        return WolfSpiderBehavior.Idle;
    }

    private bool CanAttack(WolfSpider2D spider, Vector2 preyPosition)
    {
        if (IsHuntOnlyPrey())
        {
            return false;
        }

        float attackRangeSqr = spider.attackRange * spider.attackRange;

        if ((preyPosition - spider.Position).sqrMagnitude > attackRangeSqr)
        {
            return false;
        }

        return WolfSpiderSurfaceProbe.HasLineOfSight(spider.Position, preyPosition);
    }

    private WolfSpiderIntent BuildAttackIntent(WolfSpider2D spider)
    {
        return new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Attack,
            jumpTarget = spider.Position,
            focusTarget = currentPrey
        };
    }

    private WolfSpiderIntent BuildHuntIntent(WolfSpider2D spider)
    {
        if (!spider.Arrived)
        {
            return hasIssuedIntent && lastIssuedIntent.behaviorState == WolfSpiderBehavior.Hunt
                ? lastIssuedIntent
                : CreateHuntIntent(spider, lastIssuedIntent.jumpTarget);
        }

        Vector2 preyPosition = GetPreyPosition(spider);
        float snapTolSqr = spider.surfaceSnapMaxDistance * spider.surfaceSnapMaxDistance;
        bool isStayIntent = hasIssuedIntent
            && (lastIssuedIntent.jumpTarget - spider.Position).sqrMagnitude
                <= Mathf.Max(ArrivedThresholdSqr, snapTolSqr);

        if (isStayIntent && pathPickTimer > spider.pathPickInterval)
        {
            pathPickTimer = spider.pathPickInterval;
        }

        if (forceContourRepick || pathPickTimer <= 0f || HasPreyGoalChanged(preyPosition))
        {
            pathPickTimer = spider.pathPickInterval;
            lastPathGoal = preyPosition;
            forceContourRepick = false;

            Vector2 huntGoal = preyPosition;
            Vector2 jumpTarget;

            if (currentPreySource == EnemyAttractionSource.ToyCar)
            {
                if (ShouldPickNewToyCarChasePoint(spider, preyPosition))
                {
                    toyCarChasePoint = preyPosition;
                    hasToyCarChasePoint = true;
                }

                Vector2 chaseGoal = hasToyCarChasePoint ? toyCarChasePoint : preyPosition;
                huntGoal = chaseGoal;
                jumpTarget = PickJumpTarget(spider, huntGoal, WolfSpiderBehavior.Hunt, out _);
            }
            else
            {
                jumpTarget = PickJumpTarget(spider, huntGoal, WolfSpiderBehavior.Hunt, out _);
            }

            if ((jumpTarget - spider.Position).sqrMagnitude <= ArrivedThresholdSqr)
            {
                pathPickTimer = spider.pathPickInterval;
                forceContourRepick = false;
            }

            return CreateHuntIntent(spider, jumpTarget);
        }

        if (!isStayIntent
            && ShouldKeepCurrentJumpTarget(spider, WolfSpiderBehavior.Hunt))
        {
            pathPickTimer -= Time.fixedDeltaTime;
            return lastIssuedIntent;
        }

        pathPickTimer -= Time.fixedDeltaTime;
        return lastIssuedIntent;
    }

    private WolfSpiderIntent CreateHuntIntent(WolfSpider2D spider, Vector2 jumpTarget)
    {
        return new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Hunt,
            jumpTarget = jumpTarget,
            focusTarget = currentPrey
        };
    }

    private WolfSpiderIntent BuildIdleIntent(WolfSpider2D spider)
    {
        if (!spider.Arrived)
        {
            return hasIssuedIntent ? lastIssuedIntent : CreateIdleIntent(spider, spider.Position);
        }

        float snapTolSqr = spider.surfaceSnapMaxDistance * spider.surfaceSnapMaxDistance;
        bool isStayIntent = hasIssuedIntent
            && (lastIssuedIntent.jumpTarget - spider.Position).sqrMagnitude
                <= Mathf.Max(ArrivedThresholdSqr, snapTolSqr);

        if (forceContourRepick || idleTimer <= 0f)
        {
            Vector2 jumpTarget = PickJumpTarget(
                spider,
                GetRandomIdleGoal(spider),
                WolfSpiderBehavior.Idle,
                out bool pickSucceeded
            );

            forceContourRepick = false;
            idleTimer = spider.idleJumpInterval;

            if (!pickSucceeded
                || (jumpTarget - spider.Position).sqrMagnitude <= ArrivedThresholdSqr)
            {
                return CreateIdleIntent(spider, spider.Position);
            }

            return CreateIdleIntent(spider, jumpTarget);
        }

        if (!isStayIntent && ShouldKeepCurrentJumpTarget(spider, WolfSpiderBehavior.Idle))
        {
            idleTimer -= Time.fixedDeltaTime;
            return lastIssuedIntent;
        }

        idleTimer -= Time.fixedDeltaTime;
        return lastIssuedIntent;
    }

    private bool HasPreyGoalChanged(Vector2 preyPosition)
    {
        return (preyPosition - lastPathGoal).sqrMagnitude > PreyGoalChangeThresholdSqr;
    }

    private bool ShouldKeepCurrentJumpTarget(WolfSpider2D spider, WolfSpiderBehavior expectedBehavior)
    {
        if (!hasIssuedIntent || lastIssuedIntent.behaviorState != expectedBehavior)
        {
            return false;
        }

        float distSqr = (lastIssuedIntent.jumpTarget - spider.Position).sqrMagnitude;
        float snapTolSqr = spider.surfaceSnapMaxDistance * spider.surfaceSnapMaxDistance;

        if (distSqr <= snapTolSqr)
        {
            return false;
        }

        float minJump = spider.minJumpDist + 0.02f;
        float minJumpSqr = minJump * minJump;

        if (distSqr < minJumpSqr)
        {
            return false;
        }

        if (distSqr > spider.maxJumpDist * spider.maxJumpDist)
        {
            return false;
        }

        return true;
    }

    private WolfSpiderIntent CreateIdleIntent(WolfSpider2D spider, Vector2 jumpTarget)
    {
        return new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Idle,
            jumpTarget = jumpTarget,
            focusTarget = null
        };
    }

    private Vector2 GetPreyPosition(WolfSpider2D spider)
    {
        if (currentPrey != null)
        {
            return currentPrey.position;
        }

        if (hasLastKnownPreyPosition)
        {
            return lastKnownPreyPosition;
        }

        return spider.Position;
    }

    private Vector2 GetRandomIdleGoal(WolfSpider2D spider)
    {
        Bounds bounds = spider.activityBounds;

        if (bounds.size.sqrMagnitude < 0.01f)
        {
            return spider.Position;
        }

        if (!bounds.Contains(spider.Position))
        {
            return bounds.center;
        }

        Vector2 center = bounds.center;
        Vector2 extents = bounds.extents;

        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = new Vector2(
                center.x + Random.Range(-extents.x, extents.x),
                center.y + Random.Range(-extents.y, extents.y)
            );

            if (bounds.Contains(candidate))
            {
                return candidate;
            }
        }

        return center;
    }

    private Vector2 PickJumpTarget(
        WolfSpider2D spider,
        Vector2 goal,
        WolfSpiderBehavior behavior,
        out bool pickSucceeded)
    {
        pickSucceeded = false;
        List<Vector2> debugRoute = null;
        List<Vector2> debugCandidates = null;

        if (spider.drawDebugGizmos)
        {
            spider.DebugPath ??= new List<Vector2>();
            spider.DebugPath.Clear();
            debugRoute = spider.DebugPath;

            spider.DebugCandidatePoints.Clear();
            debugCandidates = spider.DebugCandidatePoints;
        }

        IReadOnlyList<Vector2> excludes = rejectedJumpTargets;
        bool isHunt = behavior == WolfSpiderBehavior.Hunt;
        bool picked = false;
        Vector2 pickedTarget = spider.Position;
        string pickedReason = "Stay";
        pathPickSeed = unchecked(pathPickSeed * 1664525 + 1013904223 + spider.GetInstanceID());

        if (isHunt)
        {
            picked = WolfSpiderJumpPlanner.TryPickHuntJumpTarget(
                spider.Position,
                goal,
                spider.CurrentSurfaceNormal,
                spider.HasEdge ? spider.EdgeIndex : -1,
                spider.minJumpDist,
                spider.maxJumpDist,
                spider.arcHeight,
                spider.surfaceSnapMaxDistance,
                0f,
                spider.bodyRadius,
                excludes,
                hasLastVisitPoint,
                lastVisitPoint,
                pathPickSeed,
                out pickedTarget,
                out pickedReason,
                debugCandidates,
                debugRoute);
        }
        else if (behavior == WolfSpiderBehavior.Idle)
        {
            picked = WolfSpiderJumpPlanner.TryPickIdleJumpTarget(
                spider.Position,
                goal,
                spider.CurrentSurfaceNormal,
                spider.HasEdge ? spider.EdgeIndex : -1,
                spider.minJumpDist,
                spider.maxJumpDist,
                spider.arcHeight,
                spider.surfaceSnapMaxDistance,
                0f,
                spider.bodyRadius,
                spider.activityBounds,
                excludes,
                hasLastVisitPoint,
                lastVisitPoint,
                pathPickSeed,
                out pickedTarget,
                out pickedReason,
                debugCandidates);
        }

        if (picked && IsValidJumpTargetForBehavior(spider, behavior, pickedTarget))
        {
            pickSucceeded = true;
            spider.DebugPickReason = pickedReason;
            spider.DebugTarget = pickedTarget;
            CacheArcDebug(spider, pickedTarget);

            if (spider.enableDebugLog)
            {
                spider.LogDebug(
                    $"选点 {pickedReason} → {pickedTarget}，候选 {debugCandidates?.Count ?? 0} 个");
            }

            return pickedTarget;
        }

        if (WolfSpiderJumpPlanner.TryPickRelaxedFromCandidates(
                spider.Position,
                spider.minJumpDist,
                spider.maxJumpDist,
                hasLastVisitPoint,
                lastVisitPoint,
                pathPickSeed,
                spider.activityBounds,
                restrictToActivityBounds: behavior == WolfSpiderBehavior.Idle,
                out Vector2 relaxedTarget,
                out string relaxedReason)
            && IsValidJumpTargetForBehavior(spider, behavior, relaxedTarget))
        {
            pickSucceeded = true;
            spider.DebugPickReason = relaxedReason;
            spider.DebugTarget = relaxedTarget;
            return relaxedTarget;
        }

        if (WolfSpiderJumpPlanner.TryPickRelaxedFromCandidates(
                spider.Position,
                spider.minJumpDist,
                spider.maxJumpDist,
                hasRecentVisit: false,
                recentVisitPoint: default,
                pathPickSeed,
                spider.activityBounds,
                restrictToActivityBounds: behavior == WolfSpiderBehavior.Idle,
                out Vector2 ignoreVisitTarget,
                out string ignoreVisitReason)
            && IsValidJumpTargetForBehavior(spider, behavior, ignoreVisitTarget))
        {
            pickSucceeded = true;
            spider.DebugPickReason = ignoreVisitReason;
            spider.DebugTarget = ignoreVisitTarget;
            return ignoreVisitTarget;
        }

        if (WolfSpiderJumpPlanner.TryPickDesperateJump(
                spider.Position,
                goal,
                spider.CurrentSurfaceNormal,
                spider.HasEdge ? spider.EdgeIndex : -1,
                spider.minJumpDist,
                spider.maxJumpDist,
                spider.arcHeight,
                0f,
                spider.bodyRadius,
                isHunt,
                spider.activityBounds,
                restrictToActivityBounds: behavior == WolfSpiderBehavior.Idle,
                out Vector2 desperateTarget,
                out string desperateReason,
                debugCandidates)
            && IsValidJumpTargetForBehavior(spider, behavior, desperateTarget))
        {
            rejectedJumpTargets.Clear();
            forceContourRepick = false;
            pickSucceeded = true;
            spider.DebugPickReason = desperateReason;
            spider.DebugTarget = desperateTarget;
            CacheArcDebug(spider, desperateTarget);
            return desperateTarget;
        }

        forceContourRepick = false;

        if (behavior == WolfSpiderBehavior.Hunt)
        {
            pathPickTimer = spider.pathPickInterval;
        }

        spider.DebugPickReason = "Stay";
        return spider.Position;
    }

    private static bool IsValidJumpTargetForBehavior(
        WolfSpider2D spider,
        WolfSpiderBehavior behavior,
        Vector2 jumpTarget)
    {
        if (behavior != WolfSpiderBehavior.Idle)
        {
            return true;
        }

        return spider.IsInsideActivityBounds(jumpTarget);
    }

    private void CacheArcDebug(WolfSpider2D spider, Vector2 jumpTarget)
    {
        spider.DebugArcSamples.Clear();
    }

    private bool IsHuntOnlyPrey()
    {
        return currentPreySource == EnemyAttractionSource.MeatBait
            || currentPreySource == EnemyAttractionSource.ToyCar;
    }

    private bool ShouldPickNewToyCarChasePoint(WolfSpider2D spider, Vector2 preyPosition)
    {
        if (!hasToyCarChasePoint)
        {
            return true;
        }

        if (spider.Arrived)
        {
            return true;
        }

        float detectRadiusSqr = spider.detectRadius * spider.detectRadius;

        if (((Vector2)preyPosition - spider.Position).sqrMagnitude <= detectRadiusSqr
            && HasPreyGoalChanged(preyPosition))
        {
            return true;
        }

        return false;
    }

    private bool ShouldKeepToyCarChaseWithoutPerception(WolfSpider2D spider)
    {
        if (currentPreySource != EnemyAttractionSource.ToyCar)
        {
            return false;
        }

        if (!ToyCarRegistry.HasActiveCar())
        {
            hasToyCarChasePoint = false;
            return false;
        }

        if (currentPrey != null && currentPrey.gameObject.activeInHierarchy)
        {
            return true;
        }

        return hasToyCarChasePoint && !spider.Arrived;
    }
}
