using System.Collections.Generic;
using UnityEngine;

public class ghostMotor : IMonsterMotor
{
    private enum GhostPhase
    {
        Chase,
        Wait
    }

    private const int SegmentSampleCount = 24;

    private GhostPhase phase = GhostPhase.Chase;
    private float waitTimer;
    private float segmentTimer;

    private readonly List<Vector2> segmentPath = new List<Vector2>();
    private int pathIndex;

    public void Execute(MonsterBase owner, IIntent intent)
    {
        Ghost ghost = owner as Ghost;

        if (ghost == null)
        {
            return;
        }

        if (phase == GhostPhase.Wait)
        {
            TickWait(ghost, intent);
            return;
        }

        if (intent is not GhostIntent chase || chase.target == null)
        {
            ClearSegment(ghost);
            return;
        }

        TickChase(ghost, chase);
    }

    private void TickChase(Ghost ghost, GhostIntent chase)
    {
        segmentTimer += Time.fixedDeltaTime;

        if (NeedsReplan(ghost))
        {
            ReplanSegment(ghost, chase.target);
        }

        if (segmentPath.Count == 0)
        {
            return;
        }

        MoveAlongSegment(ghost);

        if (ghost.IsPlayerInAttackRange(chase.target))
        {
            ghost.TryDamagePlayer(chase.target);
            BeginWait(ghost);
        }
    }

    private void TickWait(Ghost ghost, IIntent intent)
    {
        waitTimer -= Time.fixedDeltaTime;

        if (waitTimer > 0f)
        {
            return;
        }

        phase = GhostPhase.Chase;
        segmentTimer = ghost.pathPlanInterval;

        if (intent is GhostIntent chase && chase.target != null)
        {
            ReplanSegment(ghost, chase.target);
        }
    }

    private void BeginWait(Ghost ghost)
    {
        phase = GhostPhase.Wait;
        waitTimer = ghost.waitDuration;
        ClearSegment(ghost);
    }

    private bool NeedsReplan(Ghost ghost)
    {
        if (segmentPath.Count == 0)
        {
            return true;
        }

        if (segmentTimer >= ghost.pathPlanInterval)
        {
            return true;
        }

        return pathIndex >= segmentPath.Count;
    }

    private void ReplanSegment(Ghost ghost, Transform player)
    {
        segmentTimer = 0f;
        pathIndex = 0;
        segmentPath.Clear();

        Vector2 anchor = player.position;
        Vector2 start = ghost.Position;
        Vector2 offset = start - anchor;
        float startRadius = offset.magnitude;

        if (startRadius < 0.05f)
        {
            offset = Vector2.right * 0.5f;
            startRadius = 0.5f;
        }

        float startAngle = Mathf.Atan2(offset.y, offset.x);
        float travelDistance = ghost.moveSpeed * ghost.pathPlanInterval;
        float endRadius = Mathf.Max(ghost.attackRange * 0.55f, startRadius - travelDistance * 0.75f);
        float totalTurn = ghost.spiralCurvature * Mathf.PI * 2f;
        // Atan2 角度增大为逆时针，减小为顺时针
        const float clockwiseTurnSign = -1f;

        for (int i = 0; i < SegmentSampleCount; i++)
        {
            float t = i / (float)(SegmentSampleCount - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float angle = startAngle + clockwiseTurnSign * totalTurn * t;
            segmentPath.Add(anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        ghost.CurrentTarget = segmentPath[segmentPath.Count - 1];
        ghost.Arrived = false;

        if (ghost.drawDebugGizmos)
        {
            ghost.DebugPath = new List<Vector2>(segmentPath);
            ghost.DebugTarget = ghost.CurrentTarget;
        }
    }

    private void MoveAlongSegment(Ghost ghost)
    {
        Vector2 waypoint = segmentPath[pathIndex];
        ghost.transform.position = Vector2.MoveTowards(
            ghost.Position,
            waypoint,
            ghost.moveSpeed * Time.fixedDeltaTime
        );

        if (Vector2.Distance(ghost.Position, waypoint) > 0.08f)
        {
            return;
        }

        pathIndex++;

        if (pathIndex >= segmentPath.Count)
        {
            ghost.Arrived = true;
        }
    }

    private void ClearSegment(Ghost ghost)
    {
        segmentPath.Clear();
        pathIndex = 0;
        segmentTimer = 0f;
        ghost.Arrived = true;
        ghost.DebugPath = null;
    }
}
