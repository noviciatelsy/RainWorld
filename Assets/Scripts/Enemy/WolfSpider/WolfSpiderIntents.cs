using UnityEngine;

public enum WolfSpiderBehavior
{
    Idle,
    Hunt,
    Attack
}

public struct WolfSpiderIntent : IIntent
{
    public WolfSpiderBehavior behaviorState;
    public Vector2 jumpTarget;
    public Transform focusTarget;
}
