using System.Collections.Generic;
using UnityEngine;

public enum SnailBehavior
{
    IdleWander,
    FollowPath
}

public struct SnailMoveIntent : IIntent
{
    public SnailBehavior behavior;
    public bool clockwise;
    public List<Vector2> pathVertices;
}
