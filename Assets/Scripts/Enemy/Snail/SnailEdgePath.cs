using System.Collections.Generic;
using UnityEngine;

public static class SnailEdgePath
{
    public static List<Vector2> FindVertexPath(Vector2 fromWorld, Vector2 toWorld, int maxEdgeSteps = 500)
    {
        return SurfaceEdgePath.FindVertexPath(fromWorld, toWorld, maxEdgeSteps);
    }
}
