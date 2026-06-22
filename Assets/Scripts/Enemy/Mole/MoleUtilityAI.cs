using System.Collections.Generic;
using UnityEngine;

// ?????????? Motor ?????? Idle ????????????????????????
public class MoleIdleIntent : IIntent
{
    public List<Vector2> strictPath;
    public bool isTeleportCleanup = false; // ???????
}

public class MoleUtilityAI : IMonsterAI
{
    private Mole2D mole;
    private List<Vector2> lastIssuedPath = new List<Vector2>();
    private bool isStealing = false;
    private bool isMovingToCave = false;
    private float teleportCooldown = 0f;
    private float stealCooldown = 0f;
    private Player lastStealPlayer;
    private bool isChasingToyCar;
    private float toyCarPathRefreshTimer;
    private Vector2 committedToyCarPosition;
    private bool hasCommittedToyCarPosition;

    private const float ToyCarPathRefreshInterval = 0.45f;
    private const float StealDuration = 3f;
    private const float StealRetryCooldown = 4f;

    public MoleUtilityAI(Mole2D mole)
    {
        this.mole = mole;
    }

    public void ForceAttractionRefresh()
    {
        toyCarPathRefreshTimer = 0f;
    }

    public void NotifyRepelledByTorch(Vector2 torchPosition)
    {
        isStealing = false;
        mole.stealTimer = 0f;
        SetStealClawActive(false);
        lastStealPlayer = null;
        stealCooldown = StealRetryCooldown;
        isMovingToCave = false;
        isChasingToyCar = false;

        Vector2 fleeTarget = TorchAvoidance.GetFleePointAwayFromAllTorches(mole.Position);
        lastIssuedPath = BuildTorchFleePath(fleeTarget);
        mole.Arrived = false;
    }

    public IIntent Evaluate(MonsterBase owner)
    {
        // 1. ???????????????????? Idle ???? cleanup ???
        if (teleportCooldown > 0f)
        {
            teleportCooldown -= Time.fixedDeltaTime;
            return new MoleIdleIntent
            {
                strictPath = new List<Vector2> { mole.Position },
                isTeleportCleanup = true
            };
        }

        if (TorchAvoidance.IsInsideAnyActiveTorch(mole.Position))
        {
            Vector2 fleeTarget = TorchAvoidance.GetFleePointAwayFromAllTorches(mole.Position);
            isStealing = false;
            mole.stealTimer = 0f;
            SetStealClawActive(false);
            lastStealPlayer = null;
            stealCooldown = StealRetryCooldown;
            isMovingToCave = false;
            isChasingToyCar = false;

            return new MoleIdleIntent
            {
                strictPath = BuildTorchFleePath(fleeTarget),
                isTeleportCleanup = false
            };
        }

        // 2. 宝物收集（广范围识别）
        if (!isStealing && !isMovingToCave && mole.TreasureCollector != null)
        {
            PickableObject treasureTarget = mole.TreasureCollector.ResolveCollectTarget();
            if (treasureTarget != null)
            {
                isMovingToCave = false;
                return new MoleCollectTreasureIntent
                {
                    targetPickable = treasureTarget
                };
            }
        }

        // 3. 偷取玩家
        if (stealCooldown > 0f)
        {
            stealCooldown -= Time.fixedDeltaTime;
        }

        bool hasPlayer = PlayerInvisibilityPerception.TryFindDetectablePlayer(
            mole.Position,
            mole.playerCheckRadius,
            mole.playerLayer,
            out Player detectedPlayer);

        if (isStealing
            && lastStealPlayer != null
            && !PlayerInvisibilityPerception.IsPlayerDetectable(lastStealPlayer))
        {
            CancelStealAttempt();
        }

        if (hasPlayer && !isStealing && stealCooldown <= 0f)
        {
            BeginStealAttempt(detectedPlayer);
        }

        // 4. Steal 阶段：满 3 秒后立即结算，不要求玩家离开
        if (isStealing)
        {
            mole.stealTimer -= Time.fixedDeltaTime;
            UpdateStealClawVisual();

            if (mole.stealTimer <= 0f)
            {
                CompleteStealAttempt();
            }
            else
            {
                return new MoleStealIntent();
            }
        }

        // 5. 传送后 idle 清理
        if (isMovingToCave && mole.idleArrivalCount == 0)
        {
            isMovingToCave = false;
            lastIssuedPath = null;
            mole.Arrived = true;
            teleportCooldown = 0.4f; // ???? 0.4 ????????????

            return new MoleIdleIntent
            {
                strictPath = new List<Vector2> { mole.Position },
                isTeleportCleanup = true
            };
        }

        // 6. 游荡 3 次后进洞
        if (mole.idleArrivalCount >= 3)
        {
            if (MoleCaveManager.Instance != null && mole.currentHomeCave != null)
            {
                isMovingToCave = true;
                return new MoleUseCaveIntent { targetCave = mole.currentHomeCave };
            }
        }

        if (isMovingToCave)
        {
            return new MoleUseCaveIntent { targetCave = mole.currentHomeCave };
        }

        if (!isStealing && mole.idleArrivalCount < 3 && TryUpdateToyCarChasePath())
        {
            isChasingToyCar = true;
            return new MoleIdleIntent { strictPath = lastIssuedPath, isTeleportCleanup = false };
        }

        isChasingToyCar = false;

        // 7. 日常游荡
        if (mole.Arrived || lastIssuedPath == null || lastIssuedPath.Count == 0)
        {
            lastIssuedPath = GenerateStrictEdgePath();
            mole.Arrived = false;
        }

        // ??????????isTeleportCleanup ???? false
        return new MoleIdleIntent { strictPath = lastIssuedPath, isTeleportCleanup = false };
    }

    private List<Vector2> GenerateStrictEdgePath()
    {
        List<Vector2> pathPoints = new List<Vector2>();
        if (mole.currentHomeCave == null) return pathPoints;

        var mgr = TileMapGuideManager.Instance;
        Bounds bounds = mole.currentHomeCave.activityBounds;
        Vector2Int startCell = mgr.WorldToCell(mole.Position);

        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> reachableValidCells = new List<Vector2Int>();

        queue.Enqueue(startCell);
        parentMap.Add(startCell, startCell);

        int maxSearchSteps = 500;
        int steps = 0;

        while (queue.Count > 0 && steps < maxSearchSteps)
        {
            steps++;
            Vector2Int current = queue.Dequeue();
            Vector2 currentWorld = mgr.CellToWorld(current);

            if (bounds.Contains(currentWorld))
            {
                if (!mgr.IsSolid(current) && mgr.IsSolid(current + Vector2Int.down))
                {
                    if (current != startCell)
                    {
                        reachableValidCells.Add(current);
                    }
                }
            }

            Vector2Int[] allowedOffsets = { Vector2Int.left, Vector2Int.right };
            foreach (var offset in allowedOffsets)
            {
                Vector2Int[] verticalTries = {
                    current + offset,
                    current + offset + Vector2Int.up,
                    current + offset + Vector2Int.down
                };

                foreach (var neighbor in verticalTries)
                {
                    if (parentMap.ContainsKey(neighbor)) continue;

                    if (!mgr.IsSolid(neighbor) && mgr.IsSolid(neighbor + Vector2Int.down))
                    {
                        if (Mathf.Abs(neighbor.y - current.y) <= 1)
                        {
                            parentMap.Add(neighbor, current);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }

        if (reachableValidCells.Count > 0)
        {
            Vector2Int chosenEnd = reachableValidCells[Random.Range(0, reachableValidCells.Count)];
            Vector2Int backtrackNode = chosenEnd;

            while (backtrackNode != startCell)
            {
                Vector2 worldPos = RobotGroundPath.CellToFeetWorld(mgr, backtrackNode, mole.feetYOffset);
                pathPoints.Insert(0, worldPos);
                backtrackNode = parentMap[backtrackNode];
            }
        }
        else
        {
            Vector2 backupTarget = new Vector2(
                bounds.center.x + Random.Range(-0.5f, 0.5f),
                mole.Position.y
            );
            pathPoints.Add(backupTarget);
        }

        return pathPoints;
    }

    private List<Vector2> BuildTorchFleePath(Vector2 fleeTarget)
    {
        List<Vector2> path = SurfaceEdgePath.FindVertexPath(mole.Position, fleeTarget);
        if (path != null && path.Count > 0)
        {
            return path;
        }

        return new List<Vector2> { fleeTarget };
    }

    private bool TryUpdateToyCarChasePath()
    {
        bool carInRange = ToyCarRegistry.TryFindClosest(
            mole.Position,
            mole.detectRadius,
            out ToyCarController car,
            out _);

        if (carInRange)
        {
            committedToyCarPosition = car.AttractionCenter;
            hasCommittedToyCarPosition = true;
        }

        if (!hasCommittedToyCarPosition || !ToyCarRegistry.HasActiveCar())
        {
            return false;
        }

        toyCarPathRefreshTimer -= Time.fixedDeltaTime;

        if (isChasingToyCar
            && !mole.Arrived
            && toyCarPathRefreshTimer > 0f
            && lastIssuedPath != null
            && lastIssuedPath.Count > 0)
        {
            return true;
        }

        List<Vector2> path = SurfaceEdgePath.FindVertexPath(mole.Position, committedToyCarPosition);

        if (path == null || path.Count == 0)
        {
            return isChasingToyCar
                && !mole.Arrived
                && lastIssuedPath != null
                && lastIssuedPath.Count > 0;
        }

        lastIssuedPath = path;
        mole.Arrived = false;
        toyCarPathRefreshTimer = ToyCarPathRefreshInterval;
        return true;
    }

    private void BeginStealAttempt(Player player)
    {
        isStealing = true;
        mole.stealTimer = StealDuration;
        isMovingToCave = false;
        lastStealPlayer = player;

        EnemyMoleAudioEmitter audioEmitter = mole.GetComponent<EnemyMoleAudioEmitter>();
        audioEmitter?.PlayStealWarning();
    }

    private void CompleteStealAttempt()
    {
        SetStealClawActive(false);
        isStealing = false;
        stealCooldown = StealRetryCooldown;

        Player stealTarget = lastStealPlayer;
        lastStealPlayer = null;

        if (stealTarget != null)
        {
            mole.CompleteSteal(stealTarget);
        }
    }

    private void CancelStealAttempt()
    {
        SetStealClawActive(false);
        isStealing = false;
        mole.stealTimer = 0f;
        lastStealPlayer = null;
        stealCooldown = StealRetryCooldown;
    }

    private void UpdateStealClawVisual()
    {
        if (mole.moleAni == null)
        {
            return;
        }

        SetStealClawActive(true);

        Collider2D playerHit = Physics2D.OverlapCircle(
            mole.Position,
            mole.playerCheckRadius,
            mole.playerLayer
        );

        if (playerHit != null && PlayerInvisibilityPerception.IsPlayerDetectable(playerHit))
        {
            Player player = playerHit.GetComponentInParent<Player>();
            if (player != null)
            {
                lastStealPlayer = player;
            }

            mole.moleAni.UpdateStealClaw(playerHit.transform.position);
        }
        else if (lastStealPlayer != null)
        {
            mole.moleAni.UpdateStealClaw(lastStealPlayer.transform.position);
        }
    }

    private void SetStealClawActive(bool active)
    {
        mole.moleAni?.SetActivate(active);

        EnemyMoleAudioEmitter audioEmitter = mole.GetComponent<EnemyMoleAudioEmitter>();
        if (audioEmitter == null)
        {
            return;
        }

        if (active)
        {
            audioEmitter.StartStealLoop();
        }
        else
        {
            audioEmitter.StopStealLoop();
        }
    }
}