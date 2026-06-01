using System.Collections.Generic;
using UnityEngine;

public class SnailUtilityAI : IMonsterAI
{
    private enum SnailMode
    {
        IdleWander,
        GoToItem,
        WaitEat,
        ReturnToIdle
    }

    private readonly Snail2D snail;

    private SnailMode mode = SnailMode.IdleWander;
    private PickableObject targetItem;
    private float waitTimer;
    private List<Vector2> activePath;

    public SnailUtilityAI(Snail2D snail)
    {
        this.snail = snail;
    }

    public IIntent Evaluate(MonsterBase owner)
    {
        if (owner is not Snail2D sw)
        {
            return IdleIntent(null, false);
        }

        TickMode(sw);

        if (mode == SnailMode.ReturnToIdle)
        {
            return EvaluateReturnToIdle(sw);
        }

        switch (mode)
        {
            case SnailMode.GoToItem:
                if (activePath != null && activePath.Count > 0 && !sw.Arrived)
                {
                    return PathIntent(activePath);
                }

                return IdleIntent(sw, false);

            case SnailMode.WaitEat:
                return IdleIntent(sw, true);

            default:
                if (activePath != null && activePath.Count > 0 && !sw.Arrived)
                {
                    return PathIntent(activePath);
                }

                if (sw.NeedsReturnToIdle())
                {
                    BeginReturnToIdle(sw);
                    return EvaluateReturnToIdle(sw);
                }

                if (TryStartEatItem(sw))
                {
                    return PathIntent(activePath);
                }

                return IdleIntent(sw, false);
        }
    }

    private IIntent EvaluateReturnToIdle(Snail2D sw)
    {
        if (activePath != null && activePath.Count > 0 && !sw.Arrived)
        {
            return PathIntent(activePath);
        }

        if (sw.Arrived)
        {
            snail.SnapToIdleAnchor();
        }

        if (!sw.NeedsReturnToIdle())
        {
            mode = SnailMode.IdleWander;
            activePath = null;
            sw.Arrived = true;
            return IdleIntent(sw, false);
        }

        if (sw.Arrived)
        {
            if (Vector2.Distance(sw.Position, snail.GetIdleAnchorOnEdge()) <= sw.arriveThreshold * 2f)
            {
                mode = SnailMode.IdleWander;
                activePath = null;
                return IdleIntent(sw, false);
            }

            BeginReturnToIdle(sw);

            if (activePath != null && activePath.Count > 0 && !sw.Arrived)
            {
                return PathIntent(activePath);
            }
        }

        return IdleIntent(sw, true);
    }

    private bool TryStartEatItem(Snail2D sw)
    {
        if (sw.NeedsReturnToIdle())
        {
            return false;
        }

        PickableObject detected = FindBestPickableInDetectArea(sw);

        if (detected == null)
        {
            return false;
        }

        List<Vector2> path = SnailEdgePath.FindVertexPath(sw.Position, detected.transform.position);

        if (path.Count <= 0)
        {
            return false;
        }

        targetItem = detected;
        activePath = path;
        mode = SnailMode.GoToItem;
        sw.Arrived = false;
        return true;
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

            if (sw.Arrived)
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
            BeginReturnToIdle(sw);
        }
    }

    private void CancelEatAndReturn(Snail2D sw)
    {
        targetItem = null;
        BeginReturnToIdle(sw);
    }

    private void BeginReturnToIdle(Snail2D sw)
    {
        Vector2 anchor = snail.GetIdleAnchorOnEdge();
        List<Vector2> path = SnailEdgePath.FindVertexPath(sw.Position, anchor);

        activePath = path;
        mode = SnailMode.ReturnToIdle;
        sw.Arrived = path == null || path.Count == 0;

        if (sw.Arrived)
        {
            snail.SnapToIdleAnchor();

            if (!sw.NeedsReturnToIdle())
            {
                mode = SnailMode.IdleWander;
                activePath = null;
            }
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

    private static SnailMoveIntent IdleIntent(Snail2D sw, bool holdPosition)
    {
        return new SnailMoveIntent
        {
            behavior = SnailBehavior.IdleWander,
            clockwise = sw != null && sw.idleClockwise,
            pathVertices = null,
            holdPosition = holdPosition
        };
    }

    private static SnailMoveIntent PathIntent(List<Vector2> path)
    {
        return new SnailMoveIntent
        {
            behavior = SnailBehavior.FollowPath,
            clockwise = true,
            pathVertices = path,
            holdPosition = false
        };
    }
}
