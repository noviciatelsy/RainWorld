using System.Collections.Generic;
using UnityEngine;

public enum RobotBehavior
{
    Idle,
    Charge,
    Recover
}

public struct RobotMoveIntent : IIntent
{
    public RobotBehavior behavior;
    public List<Vector2> pathVertices;
    public float moveSpeed;
    public Transform chargeTarget;
}
