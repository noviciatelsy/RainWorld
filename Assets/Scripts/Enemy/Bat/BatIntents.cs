using UnityEngine;

public enum BatBehavior
{
    Idle,
    Hunt,
    Attack
}

public struct BatIntent : IIntent
{
    public BatBehavior behaviorState;
    public Vector2 moveTarget;
    public Transform focusTarget;
}
