using System.Collections.Generic;
using UnityEngine;

public class MoleMotor : IMonsterMotor
{
    private Mole2D mole;
    private List<Vector2> edgePath;
    private int pathIndex = 0;
    private MoleCave lastTargetCave;

    private float internalTeleportTimer = 0f;

    private int activeSegmentIndex = -1;
    private Vector2 segmentStartPos;
    private float segmentProgress;

    private const float JumpArcHeight = 0.32f;
    private const float JumpMinHeightDelta = 0.04f;
    private const float ArriveThreshold = 0.08f;

    public MoleMotor(Mole2D mole)
    {
        this.mole = mole;
    }

    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (internalTeleportTimer > 0f)
        {
            internalTeleportTimer -= Time.fixedDeltaTime;
            edgePath = null;
            return;
        }

        if (intent is MoleCollectTreasureIntent collectIntent)
        {
            ExecuteCollectTreasure(collectIntent.targetPickable);
            return;
        }

        if (intent is MoleStealIntent)
        {
            ExecuteSteal();
            return;
        }

        if (intent is MoleIdleIntent idleIntent)
        {
            // ???????????????????
            ExecuteStrictIdle(idleIntent.strictPath, idleIntent.isTeleportCleanup);
            return;
        }

        if (intent is MoleUseCaveIntent useCave)
        {
            ExecuteUseCave(useCave.targetCave);
            return;
        }
    }

    private void ExecuteSteal()
    {
        edgePath = null;
        pathIndex = 0;
    }

    private void ExecuteCollectTreasure(PickableObject targetPickable)
    {
        edgePath = null;
        pathIndex = 0;

        if (targetPickable == null)
        {
            mole.TreasureCollector?.ClearRegisteredTarget();
            mole.Arrived = true;
            return;
        }

        MoleTreasureCollector collector = mole.TreasureCollector;
        if (collector == null)
        {
            return;
        }

        if (collector.IsWithinCollectRange(targetPickable))
        {
            collector.TryCollect(targetPickable);
            mole.Arrived = true;
            return;
        }

        Vector2 targetPos = targetPickable.transform.position;
        mole.Transform.position = Vector2.MoveTowards(
            mole.Position,
            targetPos,
            mole.moveSpeed * Time.fixedDeltaTime
        );
        mole.CurrentTarget = targetPos;
        mole.Arrived = false;
    }

    private void ExecuteStrictIdle(List<Vector2> aiStrictPath, bool isTeleportCleanup)
    {
        if (edgePath != aiStrictPath)
        {
            edgePath = aiStrictPath;
            pathIndex = 0;
            mole.Arrived = false;
            ResetSegmentTracking();
        }

        // ?????????????????????????
        if (edgePath == null || edgePath.Count == 0)
        {
            mole.Arrived = true;
            // ??????????????????????????????????????? arrivalCount ???????
            if (!isTeleportCleanup)
            {
                mole.idleArrivalCount++;
            }
            return;
        }

        DriveMovement(false, isTeleportCleanup);
    }

    private void ExecuteUseCave(MoleCave targetCave)
    {
        if (targetCave != mole.currentHomeCave)
        {
            edgePath = null;
            return;
        }

        if (TryTeleportThroughCave(targetCave))
        {
            return;
        }

        Vector2 caveFeet = targetCave.GetMoleFeetPosition(mole.feetYOffset);

        if (lastTargetCave != targetCave || edgePath == null)
        {
            lastTargetCave = targetCave;
            edgePath = GeneratePathToCave(caveFeet);
            pathIndex = 0;
            mole.CurrentTarget = caveFeet;
            ResetSegmentTracking();
        }

        if (edgePath != null && edgePath.Count > 0 && pathIndex < edgePath.Count)
        {
            DriveMovement(true, false);
            return;
        }

        if (!targetCave.IsMoleAtEntrance(mole.Position, mole.feetYOffset))
        {
            Vector2 nextPos = Vector2.MoveTowards(
                mole.Position,
                caveFeet,
                mole.moveSpeed * Time.fixedDeltaTime
            );
            mole.SnapFeetToGround(nextPos);
            mole.CurrentTarget = caveFeet;
        }
    }

    private bool TryTeleportThroughCave(MoleCave targetCave)
    {
        if (targetCave == null || !targetCave.IsMoleAtEntrance(mole.Position, mole.feetYOffset))
        {
            return false;
        }

        if (MoleCaveManager.Instance == null)
        {
            return false;
        }

        List<MoleCave> linkedCaves = MoleCaveManager.Instance.GetLinkedCaves(targetCave);
        if (linkedCaves == null || linkedCaves.Count == 0)
        {
            return false;
        }

        MoleCave exitCave = linkedCaves[Random.Range(0, linkedCaves.Count)];
        if (exitCave == null)
        {
            return false;
        }

        EnemyMoleAudioEmitter audioEmitter = mole.GetComponent<EnemyMoleAudioEmitter>();
        audioEmitter?.PlayDigIn();

        mole.PlaceAtCave(exitCave);
        mole.currentHomeCave = exitCave;
        mole.idleArrivalCount = 0;

        audioEmitter?.PlayDigOut();

        lastTargetCave = exitCave;
        edgePath = null;
        internalTeleportTimer = 0.3f;
        return true;
    }

    private void ResetSegmentTracking()
    {
        activeSegmentIndex = -1;
        segmentProgress = 0f;
    }

    private void DriveMovement(bool isGoingToCave, bool isTeleportCleanup)
    {
        if (edgePath == null || pathIndex >= edgePath.Count)
        {
            return;
        }

        Vector2 nodeTarget = edgePath[pathIndex];
        mole.CurrentTarget = nodeTarget;

        BeginSegmentIfNeeded(nodeTarget);

        float segmentLength = Vector2.Distance(segmentStartPos, nodeTarget);

        if (segmentLength < 0.0001f)
        {
            AdvancePathSegment(isGoingToCave, isTeleportCleanup);
            return;
        }

        float step = mole.moveSpeed * Time.fixedDeltaTime;
        segmentProgress = Mathf.Min(1f, segmentProgress + step / segmentLength);

        Vector2 basePos = Vector2.Lerp(segmentStartPos, nodeTarget, segmentProgress);
        float heightDelta = nodeTarget.y - segmentStartPos.y;
        float jumpArc = 0f;

        if (Mathf.Abs(heightDelta) >= JumpMinHeightDelta)
        {
            jumpArc = JumpArcHeight * 4f * segmentProgress * (1f - segmentProgress);
        }

        Vector3 pos = mole.Transform.position;
        if (jumpArc <= 0.001f)
        {
            mole.SnapFeetToGround(basePos);
        }
        else
        {
            mole.Transform.position = new Vector3(basePos.x, basePos.y + jumpArc, pos.z);
        }

        if (segmentProgress >= 1f || Vector2.Distance(mole.Position, nodeTarget) < ArriveThreshold)
        {
            AdvancePathSegment(isGoingToCave, isTeleportCleanup);
        }
    }

    private void BeginSegmentIfNeeded(Vector2 nodeTarget)
    {
        if (activeSegmentIndex == pathIndex)
        {
            return;
        }

        activeSegmentIndex = pathIndex;
        segmentProgress = 0f;
        segmentStartPos = pathIndex > 0 ? edgePath[pathIndex - 1] : mole.Position;
    }

    private void AdvancePathSegment(bool isGoingToCave, bool isTeleportCleanup)
    {
        pathIndex++;
        activeSegmentIndex = -1;

        if (pathIndex >= edgePath.Count)
        {
            edgePath = null;

            if (!isGoingToCave)
            {
                mole.Arrived = true;

                if (!isTeleportCleanup)
                {
                    mole.idleArrivalCount++;
                }
            }
        }
    }

    private List<Vector2> GeneratePathToCave(Vector2 caveWorldPos)
    {
        List<Vector2> pathPoints = new List<Vector2>();
        var mgr = TileMapGuideManager.Instance;

        Vector2Int startCell = mgr.WorldToCell(mole.Position);
        Vector2Int endCell = mgr.WorldToCell(caveWorldPos);

        if (startCell == endCell)
        {
            if (Vector2.Distance(mole.Position, caveWorldPos) > ArriveThreshold)
            {
                pathPoints.Add(caveWorldPos);
            }

            return pathPoints;
        }

        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        q.Enqueue(startCell);
        parentMap.Add(startCell, startCell);

        bool found = false;
        int maxSearchSteps = 1000;
        int steps = 0;

        while (q.Count > 0 && steps < maxSearchSteps)
        {
            steps++;
            Vector2Int current = q.Dequeue();

            if (current == endCell)
            {
                found = true;
                break;
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
                            q.Enqueue(neighbor);
                        }
                    }
                }
            }
        }

        if (found)
        {
            Vector2Int backtrackNode = endCell;
            while (backtrackNode != startCell)
            {
                Vector2 worldPos = RobotGroundPath.CellToFeetWorld(mgr, backtrackNode, mole.feetYOffset);
                pathPoints.Insert(0, worldPos);
                backtrackNode = parentMap[backtrackNode];
            }
        }
        else
        {
            pathPoints.Add(caveWorldPos);
        }

        return pathPoints;
    }
}