using UnityEngine;

public enum BigRobotBehavior
{
    Idle,
    Attack,
    Cooldown
}

public struct BigRobotIntent : IIntent
{
    public BigRobotBehavior behavior;
    public Transform attackTarget;
}
