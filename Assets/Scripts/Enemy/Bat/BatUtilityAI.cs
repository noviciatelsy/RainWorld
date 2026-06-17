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

    private Vector2 currentHuntPoint;
    private bool hasHuntPoint;

    private EnemyAttractionSource currentPreySource = EnemyAttractionSource.None;

    private Vector2 toyCarChasePoint;
    private bool hasToyCarChasePoint;

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

        if (TryBuildMosquitoCoilFleeIntent(bat, out BatIntent coilFleeIntent))
        {
            lastIssuedIntent = coilFleeIntent;
            return lastIssuedIntent;
        }

        if (postAttackRecoveryTimer > 0f)
        {
            postAttackRecoveryTimer -= Time.fixedDeltaTime;
        }

        if (bat.IsInAttackSequence)
        {
            if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(bat.Position))
            {
                lastIssuedIntent = CreateFleeIntent(
                    bat,
                    MosquitoCoilAvoidance.GetFleePointAwayFromAllCoils(bat.Position));
                hasIssuedIntent = true;
                bat.CurrentBehavior = BatBehavior.Idle;
                return lastIssuedIntent;
            }

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
        hasHuntPoint = false;
    }

    public void NotifyRepelledByMosquitoCoil(Vector2 coilPosition)
    {
        hasHuntPoint = false;
        pathPickTimer = 0f;
        idleTimer = 0f;
    }

    public void ForcePerceptionRefresh()
    {
        perceptionTimer = 0f;
        hasHuntPoint = false;
        pathPickTimer = 0f;
        idleTimer = 0f;
    }

    private bool TryBuildMosquitoCoilFleeIntent(Bat2D bat, out BatIntent fleeIntent)
    {
        fleeIntent = default;

        if (!MosquitoCoilAvoidance.HasActiveCoils())
        {
            return false;
        }

        if (!MosquitoCoilAvoidance.IsInsideAnyActiveCoil(bat.Position))
        {
            return false;
        }

        Vector2 fleeTarget = MosquitoCoilAvoidance.GetFleePointAwayFromAllCoils(bat.Position);
        fleeIntent = CreateFleeIntent(bat, fleeTarget);
        bat.CurrentBehavior = BatBehavior.Idle;
        hasIssuedIntent = true;
        return true;
    }

    private BatIntent CreateFleeIntent(Bat2D bat, Vector2 fleeTarget)
    {
        bat.DebugPickReason = "MosquitoCoilFlee";
        bat.DebugTarget = fleeTarget;

        return new BatIntent
        {
            behaviorState = BatBehavior.Idle,
            moveTarget = fleeTarget,
            focusTarget = null
        };
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

        perceptionTimer = bat.perceptionInterval;

        EnemyAttractionCapabilities capabilities =
            EnemyAttractionCapabilities.MeatBait
            | EnemyAttractionCapabilities.ToyCar
            | EnemyAttractionCapabilities.Fly
            | EnemyAttractionCapabilities.Player;

        if (EnemyAttractionUtility.TryResolveTarget(
                bat.Position,
                bat.detectRadius,
                capabilities,
                null,
                out EnemyAttractionTarget attraction)
            && attraction.Transform != null)
        {
            ApplyDetectedPrey(bat, attraction.Transform, attraction.Source);
            return;
        }

        int hitCount = bat.OverlapPreyNonAlloc(out Collider2D[] hits);
        Transform bestOther = null;
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

            if (bat.IsOtherPreyCollider(hit))
            {
                if (distSqr < bestOtherDistSqr)
                {
                    bestOtherDistSqr = distSqr;
                    bestOther = hit.transform;
                }
            }
        }

        if (bestOther != null)
        {
            ApplyDetectedPrey(bat, bestOther, EnemyAttractionSource.None);
            return;
        }

        if (ShouldKeepToyCarChaseWithoutPerception(bat))
        {
            aggroMemoryTimer = bat.aggroMemoryDuration;
            return;
        }

        if (aggroMemoryTimer > 0f)
        {
            aggroMemoryTimer -= bat.perceptionInterval;
            return;
        }

        currentPrey = null;
        currentPreySource = EnemyAttractionSource.None;
        hasHuntPoint = false;
    }

    private void ApplyDetectedPrey(Bat2D bat, Transform detected, EnemyAttractionSource source)
    {
        bool preyChanged = currentPrey != detected || currentPreySource != source;

        if (preyChanged)
        {
            string preyType = GetPreyTypeLabel(source, detected);
            bat.LogDebug($"发现目标: {detected.name} ({preyType})");
            pathPickTimer = 0f;
            idleTimer = 0f;
            hasHuntPoint = false;

            if (source != EnemyAttractionSource.ToyCar)
            {
                hasToyCarChasePoint = false;
            }
        }

        currentPrey = detected;
        currentPreySource = source;
        lastKnownPreyPosition = detected.position;
        hasLastKnownPreyPosition = true;
        aggroMemoryTimer = bat.aggroMemoryDuration;
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
                if (detected != null && detected.GetComponentInParent<Fly2D>() != null)
                {
                    return "Fly";
                }

                if (detected != null && detected.GetComponentInParent<Player>() != null)
                {
                    return "Player";
                }

                return "Other";
        }
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
        if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(bat.Position))
        {
            return BatBehavior.Idle;
        }

        if (postAttackRecoveryTimer > 0f)
        {
            return BatBehavior.Idle;
        }

        if (currentPrey != null)
        {
            Vector2 preyPos = GetRawPreyPosition(bat);
            if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(preyPos))
            {
                return BatBehavior.Idle;
            }

            if (CanAttack(bat, preyPos))
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
        if (IsHuntOnlyPrey())
        {
            return false;
        }

        if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(bat.Position)
            || MosquitoCoilAvoidance.IsInsideAnyActiveCoil(preyPosition))
        {
            return false;
        }

        if (ShouldUseSectorHunt())
        {
            return bat.CanAttackPosition(preyPosition);
        }

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
        Vector2 preyPosition = GetRawPreyPosition(bat);
        Vector2 moveTarget;

        if (currentPreySource == EnemyAttractionSource.ToyCar)
        {
            if (ShouldPickNewToyCarChasePoint(bat, preyPosition))
            {
                toyCarChasePoint = preyPosition;
                hasToyCarChasePoint = true;
                lastPathGoal = preyPosition;
            }

            moveTarget = hasToyCarChasePoint ? toyCarChasePoint : preyPosition;
            bat.DebugPickReason = "HuntToyCarCommitted";
        }
        else if (ShouldUseSectorHunt())
        {
            if (ShouldPickNewHuntPoint(bat, preyPosition))
            {
                currentHuntPoint = bat.PickRandomHuntSectorPoint(preyPosition);
                hasHuntPoint = true;
                lastPathGoal = preyPosition;
            }

            moveTarget = currentHuntPoint;
            bat.DebugPickReason = "HuntSector";
        }
        else
        {
            if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(preyPosition))
            {
                moveTarget = MosquitoCoilAvoidance.GetFleePointAwayFromAllCoils(bat.Position);
                bat.DebugPickReason = "MosquitoCoilFleeFromPrey";
            }
            else
            {
                moveTarget = preyPosition;
                bat.DebugPickReason = "HuntPrey";
            }
        }

        bat.DebugTarget = moveTarget;
        return CreateHuntIntent(bat, moveTarget);
    }

    private bool ShouldPickNewToyCarChasePoint(Bat2D bat, Vector2 preyPosition)
    {
        if (!hasToyCarChasePoint)
        {
            return true;
        }

        if (bat.Arrived)
        {
            return true;
        }

        float detectRadiusSqr = bat.detectRadius * bat.detectRadius;

        if (((Vector2)preyPosition - bat.Position).sqrMagnitude <= detectRadiusSqr
            && HasPreyGoalChanged(preyPosition))
        {
            return true;
        }

        return false;
    }

    private bool ShouldKeepToyCarChaseWithoutPerception(Bat2D bat)
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

        return hasToyCarChasePoint && !bat.Arrived;
    }

    private bool ShouldPickNewHuntPoint(Bat2D bat, Vector2 preyPosition)
    {
        if (!hasHuntPoint)
        {
            return true;
        }

        if (bat.Arrived)
        {
            return true;
        }

        if (HasPreyGoalChanged(preyPosition))
        {
            return true;
        }

        return !bat.IsWithinHuntSector(preyPosition, currentHuntPoint);
    }

    private BatIntent BuildIdleIntent(Bat2D bat)
    {
        idleTimer -= Time.fixedDeltaTime;

        if (idleTimer <= 0f || bat.Arrived)
        {
            idleTimer = bat.idleMoveInterval;

            Vector2 moveTarget = PickRandomIdleGoal(bat);
            bat.DebugPickReason = "IdleWander";
            bat.DebugTarget = moveTarget;

            return CreateIdleIntent(bat, moveTarget);
        }

        if (hasIssuedIntent && lastIssuedIntent.behaviorState == BatBehavior.Idle)
        {
            return lastIssuedIntent;
        }

        return CreateIdleIntent(bat, bat.Position);
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

    private Vector2 GetRawPreyPosition(Bat2D bat)
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

    private bool ShouldUseSectorHunt()
    {
        if (IsHuntOnlyPrey())
        {
            return false;
        }

        Transform prey = currentPrey != null ? currentPrey : lastDebugPrey;

        if (prey == null)
        {
            return false;
        }

        if (prey.GetComponentInParent<Player>() != null)
        {
            return true;
        }

        if (prey.GetComponentInParent<Fly2D>() != null)
        {
            return true;
        }

        Collider2D col = prey.GetComponent<Collider2D>();
        return col != null && owner.IsFlyCollider(col);
    }

    private bool IsHuntOnlyPrey()
    {
        return currentPreySource == EnemyAttractionSource.MeatBait
            || currentPreySource == EnemyAttractionSource.ToyCar;
    }

    /// <summary>
    /// 在 activityBounds 矩形内随机取点并验证可飞行路径（逻辑参考 FlyUtilityAI）。
    /// </summary>
    private Vector2 PickRandomIdleGoal(Bat2D bat)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;
        Bounds bounds = bat.activityBounds;

        if (bounds.size.sqrMagnitude < 0.01f)
        {
            bounds = new Bounds(bat.Position, new Vector3(14f, 10f, 1f));
        }

        Vector2 min = bounds.min;
        Vector2 max = bounds.max;

        for (int i = 0; i < 30; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y)
            );

            if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(candidate))
            {
                continue;
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

        Vector2 fleeFallback = MosquitoCoilAvoidance.GetFleePointAwayFromAllCoils(bat.Position);
        if (!MosquitoCoilAvoidance.IsInsideAnyActiveCoil(fleeFallback))
        {
            return fleeFallback;
        }

        return bounds.center;
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

            if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(node))
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
