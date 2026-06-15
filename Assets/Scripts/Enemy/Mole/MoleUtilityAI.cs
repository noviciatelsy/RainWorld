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
    private bool wasStealing = false;
    private bool isMovingToCave = false;
    private float teleportCooldown = 0f;

    public MoleUtilityAI(Mole2D mole)
    {
        this.mole = mole;
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

        // 2. ?????
        bool hasPlayer = Physics2D.OverlapCircle(mole.Position, mole.playerCheckRadius, mole.playerLayer) != null;
        if (hasPlayer && !isStealing)
        {
            isStealing = true;
            mole.stealTimer = 3f;
            isMovingToCave = false;
        }

        // 3. Steal ??????
        if (isStealing)
        {
            mole.stealTimer -= Time.fixedDeltaTime;
            if (mole.stealTimer <= 0f)
            {
                isStealing = hasPlayer;
                if (isStealing)
                {
                    mole.stealTimer = 3f;
                }
            }

            if (isStealing)
            {
                UpdateStealClawVisual();
                wasStealing = true;
                return new MoleStealIntent();
            }
        }

        if (wasStealing)
        {
            SetStealClawActive(false);
            wasStealing = false;
        }

        // 4. ??????????????????????????Count??Motor???????0????????????????
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

        // 5. ????????????????????????????????3??????????
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

        // 6. ?????????????
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
                Vector2 worldPos = mgr.CellToWorld(backtrackNode) + new Vector2(0f, -0.45f);
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

    private void UpdateStealClawVisual()
    {
        if (mole.moleAni == null)
        {
            return;
        }

        if (!wasStealing)
        {
            SetStealClawActive(true);
        }

        Collider2D playerHit = Physics2D.OverlapCircle(
            mole.Position,
            mole.playerCheckRadius,
            mole.playerLayer
        );

        if (playerHit != null)
        {
            mole.moleAni.UpdateStealClaw(playerHit.transform.position);
        }
    }

    private void SetStealClawActive(bool active)
    {
        mole.moleAni?.SetActivate(active);
    }
}