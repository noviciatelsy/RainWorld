using System.Collections.Generic;
using UnityEngine;

public class BatUtilityAI : IMonsterAI
{
    private readonly Bat2D owner;

    private BatIntent lastIssuedIntent;
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
    private BatBehavior lastDebugBehavior = BatBehavior.Idle;

    private const float MoveTargetLockThresholdSqr = 0.12f * 0.12f;
    private const float PreyGoalChangeThresholdSqr = 1.5f * 1.5f;
    private const int MaxPathNodeChecks = 8;

    public BatUtilityAI(Bat2D owner)
    {
        this.owner = owner;
        idleTimer = owner.idleMoveInterval;
        lastIssuedIntent = CreateIdleIntent(owner, owner.Position);
    }

    public IIntent Evaluate(MonsterBase ownerBase)
    {
        Bat2D bat = ownerBase as Bat2D;

        if (bat == null)
        {
            return lastIssuedIntent;
        }

        UpdatePerception(bat);

        if (bat.drawDebugGizmos)
        {
            UpdateDebugState(bat);
        }

        if (postAttackRecoveryTimer > 0f)
        {
            postAttackRecoveryTimer -= Time.fixedDeltaTime;
        }

        if (bat.IsInAttackSequence)
        {
            if (!hasIssuedIntent || lastIssuedIntent.behaviorState != BatBehavior.Attack)
            {
                lastIssuedIntent = BuildAttackIntent(bat);
                hasIssuedIntent = true;
            }

            bat.CurrentBehavior = BatBehavior.Attack;
            return lastIssuedIntent;
        }

        if (bat.IsCoolingDown || postAttackRecoveryTimer > 0f)
        {
            lastIssuedIntent = CreateIdleIntent(bat, bat.Position);
            bat.CurrentBehavior = BatBehavior.Idle;
            hasIssuedIntent = true;
            return lastIssuedIntent;
        }

        BatBehavior behavior = DecideBehavior(bat);
        bat.CurrentBehavior = behavior;

        switch (behavior)
        {
            case BatBehavior.Attack:
                lastIssuedIntent = BuildAttackIntent(bat);
                break;

            case BatBehavior.Hunt:
                lastIssuedIntent = BuildHuntIntent(bat);
                break;

            default:
                lastIssuedIntent = BuildIdleIntent(bat);
                break;
        }

        hasIssuedIntent = true;
        LogBehaviorChange(bat, behavior);
        return lastIssuedIntent;
    }

    public void NotifyAttackPerformed()
    {
        postAttackRecoveryTimer = owner.attackStiffDuration;
        idleTimer = owner.idleMoveInterval;
        pathPickTimer = 0f;
    }

    private void UpdatePerception(Bat2D bat)
    {
        perceptionTimer -= Time.fixedDeltaTime;
        float detectRadiusSqr = bat.detectRadius * bat.detectRadius;

        if (currentPrey != null)
        {
            if (currentPrey.gameObject.activeInHierarchy)
            {
                lastKnownPreyPosition = currentPrey.position;

                if (((Vector2)currentPrey.position - bat.Position).sqrMagnitude <= detectRadiusSqr)
                {
                    hasLastKnownPreyPosition = true;
                    aggroMemoryTimer = bat.aggroMemoryDuration;

                    if (perceptionTimer > 0f)
                    {
                        return;
                    }
                }
            }
            else
            {
                currentPrey = null;
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
            }

            return;
        }

        perceptionTimer = bat.perceptionInterval;

        int hitCount = bat.OverlapPreyNonAlloc(out Collider2D[] hits);
        Transform bestFly = null;
        Transform bestPlayer = null;
        Transform bestOther = null;
        float bestFlyDistSqr = float.MaxValue;
        float bestPlayerDistSqr = float.MaxValue;
        float bestOtherDistSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            float distSqr = ((Vector2)hit.transform.position - bat.Position).sqrMagnitude;

            if (distSqr > detectRadiusSqr)
            {
                continue;
            }

            if (bat.IsFlyCollider(hit))
            {
                if (distSqr < bestFlyDistSqr)
                {
                    bestFlyDistSqr = distSqr;
                    bestFly = hit.transform;
                }

                continue;
            }

            if (bat.IsPlayerCollider(hit))
            {
                if (distSqr < bestPlayerDistSqr)
                {
                    bestPlayerDistSqr = distSqr;
                    bestPlayer = hit.transform;
                }

                continue;
            }

            if (bat.IsOtherPreyCollider(hit))
            {
                if (distSqr < bestOtherDistSqr)
                {
                    bestOtherDistSqr = distSqr;
                    bestOther = hit.transform;
                }
            }
        }

        Fly2D closestFly = FlyRegistry.FindClosest(bat.Position, bat.detectRadius, out float flyDistSqr);

        if (closestFly != null && (bestFly == null || flyDistSqr < bestFlyDistSqr))
        {
            bestFly = closestFly.transform;
        }

        Transform detected = bestFly != null ? bestFly : (bestPlayer != null ? bestPlayer : bestOther);

        if (detected != null)
        {
            if (currentPrey != detected)
            {
                string preyType = bestFly != null ? "Fly" : (bestPlayer != null ? "Player" : "Other");
                bat.LogDebug($"发现目标: {detected.name} ({preyType})");
            }

            currentPrey = detected;
            lastKnownPreyPosition = detected.position;
            hasLastKnownPreyPosition = true;
            aggroMemoryTimer = bat.aggroMemoryDuration;
            return;
        }

        if (aggroMemoryTimer > 0f)
        {
            aggroMemoryTimer -= bat.perceptionInterval;
            return;
        }

        currentPrey = null;
    }

    private void UpdateDebugState(Bat2D bat)
    {
        bat.DebugHasPrey = currentPrey != null || (aggroMemoryTimer > 0f && hasLastKnownPreyPosition);
        bat.DebugAggroTimer = aggroMemoryTimer;
        bat.DebugPreyIsFly = currentPrey != null && currentPrey.GetComponentInParent<Fly2D>() != null;

        if (currentPrey != null)
        {
            bat.DebugPreyName = currentPrey.name;
            bat.DebugPreyPosition = currentPrey.position;
            lastDebugPrey = currentPrey;
            return;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            bat.DebugPreyName = lastDebugPrey != null ? $"{lastDebugPrey.name}(记忆)" : "LastKnown";
            bat.DebugPreyPosition = lastKnownPreyPosition;
            return;
        }

        bat.DebugPreyName = "None";
        bat.DebugPreyPosition = bat.Position;
    }

    private void LogBehaviorChange(Bat2D bat, BatBehavior behavior)
    {
        if (!bat.enableDebugLog || behavior == lastDebugBehavior)
        {
            return;
        }

        lastDebugBehavior = behavior;

        switch (behavior)
        {
            case BatBehavior.Hunt:
                bat.LogDebug(
                    $"进入 Hunt，目标 {bat.DebugPreyName}，航点 {lastIssuedIntent.moveTarget}，原因: {bat.DebugPickReason}"
                );
                break;
            case BatBehavior.Attack:
                bat.LogDebug($"进入 Attack，撕咬 {bat.DebugPreyName}");
                break;
            case BatBehavior.Idle:
                bat.LogDebug("进入 Idle");
                break;
        }
    }

    private BatBehavior DecideBehavior(Bat2D bat)
    {
        if (postAttackRecoveryTimer > 0f)
        {
            return BatBehavior.Idle;
        }

        if (currentPrey != null)
        {
            if (CanAttack(bat, currentPrey.position))
            {
                return BatBehavior.Attack;
            }

            return BatBehavior.Hunt;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            if (CanAttack(bat, lastKnownPreyPosition))
            {
                return BatBehavior.Attack;
            }

            return BatBehavior.Hunt;
        }

        return BatBehavior.Idle;
    }

    private bool CanAttack(Bat2D bat, Vector2 preyPosition)
    {
        float attackRangeSqr = bat.attackRange * bat.attackRange;
        return (preyPosition - bat.Position).sqrMagnitude <= attackRangeSqr;
    }

    private BatIntent BuildAttackIntent(Bat2D bat)
    {
        return new BatIntent
        {
            behaviorState = BatBehavior.Attack,
            moveTarget = bat.Position,
            focusTarget = currentPrey
        };
    }

    private BatIntent BuildHuntIntent(Bat2D bat)
    {
        if (!bat.Arrived)
        {
            return hasIssuedIntent && lastIssuedIntent.behaviorState == BatBehavior.Hunt
                ? lastIssuedIntent
                : CreateHuntIntent(bat, lastIssuedIntent.moveTarget);
        }

        if (ShouldKeepCurrentMoveTarget(bat, BatBehavior.Hunt))
        {
            return lastIssuedIntent;
        }

        Vector2 preyPosition = GetPreyPosition(bat);

        if (pathPickTimer > 0f && !HasPreyGoalChanged(preyPosition))
        {
            pathPickTimer -= Time.fixedDeltaTime;
            return lastIssuedIntent;
        }

        pathPickTimer = bat.pathPickInterval;
        lastPathGoal = preyPosition;

        Vector2 moveTarget = PickPathMoveTarget(bat, preyPosition, BatBehavior.Hunt);
        return CreateHuntIntent(bat, moveTarget);
    }

    private BatIntent BuildIdleIntent(Bat2D bat)
    {
        if (!bat.Arrived)
        {
            return hasIssuedIntent ? lastIssuedIntent : CreateIdleIntent(bat, bat.Position);
        }

        if (ShouldKeepCurrentMoveTarget(bat, BatBehavior.Idle))
        {
            idleTimer -= Time.fixedDeltaTime;
            return lastIssuedIntent;
        }

        idleTimer -= Time.fixedDeltaTime;

        if (idleTimer > 0f)
        {
            return hasIssuedIntent && lastIssuedIntent.behaviorState == BatBehavior.Idle
                ? lastIssuedIntent
                : CreateIdleIntent(bat, bat.Position);
        }

        idleTimer = bat.idleMoveInterval;
        pathPickTimer = bat.pathPickInterval;

        Vector2 moveTarget = PickPathMoveTarget(
            bat,
            PickRandomIdleGoal(bat),
            BatBehavior.Idle
        );

        return CreateIdleIntent(bat, moveTarget);
    }

    private bool HasPreyGoalChanged(Vector2 preyPosition)
    {
        return (preyPosition - lastPathGoal).sqrMagnitude > PreyGoalChangeThresholdSqr;
    }

    private bool ShouldKeepCurrentMoveTarget(Bat2D bat, BatBehavior expectedBehavior)
    {
        if (!hasIssuedIntent || lastIssuedIntent.behaviorState != expectedBehavior)
        {
            return false;
        }

        return (lastIssuedIntent.moveTarget - bat.Position).sqrMagnitude > MoveTargetLockThresholdSqr;
    }

    private BatIntent CreateHuntIntent(Bat2D bat, Vector2 moveTarget)
    {
        return new BatIntent
        {
            behaviorState = BatBehavior.Hunt,
            moveTarget = moveTarget,
            focusTarget = currentPrey
        };
    }

    private BatIntent CreateIdleIntent(Bat2D bat, Vector2 moveTarget)
    {
        return new BatIntent
        {
            behaviorState = BatBehavior.Idle,
            moveTarget = moveTarget,
            focusTarget = null
        };
    }

    private Vector2 GetPreyPosition(Bat2D bat)
    {
        if (currentPrey != null)
        {
            return currentPrey.position;
        }

        if (hasLastKnownPreyPosition)
        {
            return lastKnownPreyPosition;
        }

        return bat.Position;
    }

    private Vector2 PickRandomIdleGoal(Bat2D bat)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;
        Bounds bounds = bat.activityBounds;
        Vector2 center = bounds.center;
        Vector2 extents = bounds.extents;

        for (int i = 0; i < 24; i++)
        {
            Vector2 offset = Random.insideUnitCircle
                * Random.Range(bat.idleWanderRadiusMin, bat.idleWanderRadiusMax);
            Vector2 candidate = bat.Position + offset;

            if (!bounds.Contains(candidate))
            {
                candidate = new Vector2(
                    Mathf.Clamp(candidate.x, center.x - extents.x, center.x + extents.x),
                    Mathf.Clamp(candidate.y, center.y - extents.y, center.y + extents.y)
                );
            }

            if (mgr == null)
            {
                return candidate;
            }

            List<Vector2> path = mgr.FindPath(bat.Position, candidate);

            if (path != null && path.Count > 1)
            {
                return candidate;
            }
        }

        return bat.Position + Random.insideUnitCircle * bat.idleWanderRadiusMin;
    }

    private Vector2 PickPathMoveTarget(Bat2D bat, Vector2 goal, BatBehavior behavior)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            bat.DebugPickReason = "NoMgr";
            return bat.Position;
        }

        List<Vector2> path = mgr.FindPath(bat.Position, goal);

        if (bat.drawDebugGizmos)
        {
            bat.DebugPath = path;
        }

        if (path == null || path.Count == 0)
        {
            bat.DebugPickReason = "NoPath";
            return bat.Position;
        }

        float maxStepSqr = bat.maxStepAlongPath * bat.maxStepAlongPath;
        Vector2 bestPoint = bat.Position;
        float bestScore = float.MinValue;
        int step = Mathf.Max(1, path.Count / MaxPathNodeChecks);

        for (int i = path.Count - 1; i >= 0; i -= step)
        {
            Vector2 node = path[i];
            float distSqr = (node - bat.Position).sqrMagnitude;

            if (distSqr < 0.01f)
            {
                continue;
            }

            if (behavior == BatBehavior.Hunt && distSqr > maxStepSqr)
            {
                continue;
            }

            if (behavior == BatBehavior.Idle && !bat.activityBounds.Contains(node))
            {
                continue;
            }

            float towardGoal = Vector2.Dot(
                (node - bat.Position).normalized,
                (goal - bat.Position).normalized
            );
            float score = distSqr + towardGoal * 2f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = node;
            }
        }

        if (bestScore <= float.MinValue)
        {
            bat.DebugPickReason = "Stay";
            return bat.Position;
        }

        bat.DebugPickReason = behavior == BatBehavior.Hunt ? "HuntPath" : "IdlePath";
        bat.DebugTarget = bestPoint;
        return bestPoint;
    }
}
