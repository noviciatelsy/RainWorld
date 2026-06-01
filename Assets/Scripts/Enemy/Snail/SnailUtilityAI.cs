using System.Collections.Generic;
using UnityEngine;

public class SnailUtilityAI : IMonsterAI
{
    private enum SnailMode
    {
        IdleWander,
        GoToItem,
        WaitEat,
        ReturnHome
    }

    private readonly Snail2D snail;

    private SnailMode mode = SnailMode.IdleWander;
    private PickableObject targetItem;
    private float waitTimer;
    private List<Vector2> activePath;
    private bool clockwise = true;

    public SnailUtilityAI(Snail2D snail)
    {
        this.snail = snail;
    }

    public IIntent Evaluate(MonsterBase owner)
    {
        if (owner is not Snail2D sw)
        {
            return IdleIntent(true);
        }

        TickMode(sw);

        switch (mode)
        {
            case SnailMode.GoToItem:
            case SnailMode.ReturnHome:
                if (activePath != null && activePath.Count > 0 && !sw.Arrived)
                {
                    return PathIntent(activePath);
                }

                if (mode == SnailMode.ReturnHome && sw.Arrived)
                {
                    mode = SnailMode.IdleWander;
                    activePath = null;
                    return BuildIdleWanderIntent(sw);
                }

                return IdleIntent(clockwise);

            case SnailMode.WaitEat:
                return IdleIntent(clockwise);

            default:
                if (activePath != null && activePath.Count > 0 && !sw.Arrived)
                {
                    return PathIntent(activePath);
                }

                PickableObject detected = FindBestPickableInDetectArea(sw);

                if (detected != null)
                {
                    List<Vector2> path = SnailEdgePath.FindVertexPath(sw.Position, detected.transform.position);

                    if (path.Count > 0)
                    {
                        targetItem = detected;
                        activePath = path;
                        mode = SnailMode.GoToItem;
                        sw.Arrived = false;
                        return PathIntent(activePath);
                    }
                }

                return BuildIdleWanderIntent(sw);
        }
    }

    private SnailMoveIntent BuildIdleWanderIntent(Snail2D sw)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return IdleIntent(true);
        }

        bool cw = ScoreDirection(sw);
        activePath = SurfaceEdgePath.BuildWanderPath(
            mgr,
            sw.Position,
            sw.EdgeIndex,
            cw,
            6
        );
        sw.Arrived = false;

        return PathIntent(activePath);
    }

    private void TickMode(Snail2D sw)
    {
        if (mode == SnailMode.GoToItem)
        {
            if (targetItem == null)
            {
                CancelEatAndReturn(sw);
                return;
            }

            if (sw.Arrived && activePath != null)
            {
                mode = SnailMode.WaitEat;
                waitTimer = snail.eatWaitDuration;
                activePath = null;
            }

            return;
        }

        if (mode == SnailMode.WaitEat)
        {
            if (targetItem == null)
            {
                CancelEatAndReturn(sw);
                return;
            }

            waitTimer -= Time.fixedDeltaTime;

            if (waitTimer > 0f)
            {
                return;
            }

            Object.Destroy(targetItem.gameObject);
            targetItem = null;
            BeginReturnHome(sw);
            return;
        }

        if (mode == SnailMode.ReturnHome && sw.Arrived)
        {
            mode = SnailMode.IdleWander;
            activePath = null;
        }
    }

    private void CancelEatAndReturn(Snail2D sw)
    {
        targetItem = null;
        BeginReturnHome(sw);
    }

    private void BeginReturnHome(Snail2D sw)
    {
        activePath = SnailEdgePath.FindVertexPath(sw.Position, snail.spawnPoint);
        mode = SnailMode.ReturnHome;
        sw.Arrived = activePath == null || activePath.Count == 0;

        if (sw.Arrived)
        {
            sw.Transform.position = snail.spawnPoint;
            mode = SnailMode.IdleWander;
            activePath = null;
        }
    }

    private PickableObject FindBestPickableInDetectArea(Snail2D sw)
    {
        PickableObject[] all = Object.FindObjectsOfType<PickableObject>();
        PickableObject best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            PickableObject pickable = all[i];

            if (pickable == null || !pickable.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 pos = pickable.transform.position;

            if (!sw.IsInsideDetectArea(pos))
            {
                continue;
            }

            List<Vector2> path = SnailEdgePath.FindVertexPath(sw.Position, pos);

            if (path.Count <= 0)
            {
                continue;
            }

            float dist = (pos - sw.Position).sqrMagnitude;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = pickable;
            }
        }

        return best;
    }

    private bool ScoreDirection(Snail2D sw)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return clockwise;
        }

        float cw = Score(sw, mgr, true);
        float ccw = Score(sw, mgr, false);
        clockwise = cw >= ccw;
        return clockwise;
    }

    private float Score(Snail2D sw, TileMapGuideManager mgr, bool cw)
    {
        int next = mgr.GetNextIndex(sw.EdgeIndex, cw);
        Edge e = mgr.GetEdge(next);
        Vector2 mid = (e.a + e.b) * 0.5f;
        float score = 1f / (1f + Vector2.Distance(sw.Position, mid));

        if (snail.idleArea.size.sqrMagnitude > 0.01f && !snail.idleArea.Contains(mid))
        {
            score *= 0.25f;
        }

        return score;
    }

    private static SnailMoveIntent IdleIntent(bool cw)
    {
        return new SnailMoveIntent
        {
            behavior = SnailBehavior.IdleWander,
            clockwise = cw,
            pathVertices = null
        };
    }

    private static SnailMoveIntent PathIntent(List<Vector2> path)
    {
        return new SnailMoveIntent
        {
            behavior = SnailBehavior.FollowPath,
            clockwise = true,
            pathVertices = path
        };
    }
}
