using System.Collections.Generic;
using UnityEngine;

public class MoleMotor : IMonsterMotor
{
    private Mole2D mole;
    private List<Vector2> edgePath;
    private int pathIndex = 0;
    private MoleCave lastTargetCave;

    private float internalTeleportTimer = 0f;

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

        if (intent is MoleStealIntent)
        {
            ExecuteSteal();
            return;
        }

        if (intent is MoleIdleIntent idleIntent)
        {
            // 改造点：把意图里的标记传进去
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

    private void ExecuteStrictIdle(List<Vector2> aiStrictPath, bool isTeleportCleanup)
    {
        if (edgePath != aiStrictPath)
        {
            edgePath = aiStrictPath;
            pathIndex = 0;
            mole.Arrived = false;
        }

        // 改造点：如果是保底空路径或到站判定
        if (edgePath == null || edgePath.Count == 0)
        {
            mole.Arrived = true;
            // 【核心修复】如果是传送清理期间的单点，绝对不增加 arrivalCount 避免突变！
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

        if (lastTargetCave != targetCave || edgePath == null)
        {
            lastTargetCave = targetCave;
            edgePath = GeneratePathToCave(targetCave.Position);
            pathIndex = 0;
            mole.CurrentTarget = targetCave.Position;
        }

        if (edgePath != null && edgePath.Count > 0 && pathIndex < edgePath.Count)
        {
            DriveMovement(true, false);
        }

        if (Vector2.Distance(mole.Position, targetCave.Position) < 0.25f)
        {
            if (MoleCaveManager.Instance != null)
            {
                List<MoleCave> linkedCaves = MoleCaveManager.Instance.GetLinkedCaves(targetCave);
                if (linkedCaves != null && linkedCaves.Count > 0)
                {
                    MoleCave exitCave = linkedCaves[Random.Range(0, linkedCaves.Count)];

                    mole.Transform.position = exitCave.Position;

                    mole.currentHomeCave = exitCave;
                    mole.idleArrivalCount = 0; // 物理层切断归零

                    lastTargetCave = exitCave;
                    edgePath = null;

                    internalTeleportTimer = 0.3f;
                }
            }
        }
    }

    private void DriveMovement(bool isGoingToCave, bool isTeleportCleanup)
    {
        if (edgePath == null || pathIndex >= edgePath.Count) return;

        Vector2 nodeTarget = edgePath[pathIndex];
        mole.CurrentTarget = nodeTarget;

        mole.Transform.position = Vector2.MoveTowards(
            mole.Transform.position,
            nodeTarget,
            mole.moveSpeed * Time.fixedDeltaTime
        );

        if (Vector2.Distance(mole.Position, nodeTarget) < 0.08f)
        {
            pathIndex++;

            if (pathIndex >= edgePath.Count)
            {
                edgePath = null;

                if (!isGoingToCave)
                {
                    mole.Arrived = true;
                    // 【核心修复】同样，在这里也增加拦截，防止走完单步点时突变
                    if (!isTeleportCleanup)
                    {
                        mole.idleArrivalCount++;
                    }
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

        if (startCell == endCell) return pathPoints;

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
                Vector2 worldPos = mgr.CellToWorld(backtrackNode) + new Vector2(0f, -0.45f);
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