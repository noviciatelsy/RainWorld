using UnityEngine;

/// <summary>
/// 蝙蝠寻路辅助：仅做廉价预检；实际 A* 由 BatMotor 单次调用（同 FlyMotor）。
/// </summary>
public static class BatFlightPath
{
    public static bool CanAttemptPath(Bat2D bat, Vector2 goal)
    {
        if (bat == null)
        {
            return false;
        }

        if ((goal - bat.Position).sqrMagnitude <= bat.arriveThreshold * bat.arriveThreshold)
        {
            return false;
        }

        if ((goal - bat.Position).sqrMagnitude > bat.maxPathFindDistance * bat.maxPathFindDistance)
        {
            return false;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return false;
        }

        if (RepellentAvoidance.IsInsideAnyZone(goal))
        {
            return false;
        }

        return mgr.CanAttemptFindPath(bat.Position, goal, bat.maxPathSearchCells);
    }
}
