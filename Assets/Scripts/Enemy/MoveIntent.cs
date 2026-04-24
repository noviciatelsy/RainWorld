using UnityEngine;

public enum MoveIntentType
{
    SurfaceMove
}

public struct MoveIntent
{
    public MoveIntentType type;
    public bool clockwise;
}