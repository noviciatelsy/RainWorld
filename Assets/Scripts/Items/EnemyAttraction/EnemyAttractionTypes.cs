using UnityEngine;

public enum EnemyAttractionSource
{
    None = 0,
    MeatBait = 1,
    ToyCar = 2,
    Fly = 3,
    Player = 4
}

[System.Flags]
public enum EnemyAttractionCapabilities
{
    None = 0,
    MeatBait = 1 << 0,
    ToyCar = 1 << 1,
    Fly = 1 << 2,
    Player = 1 << 3
}

public readonly struct EnemyAttractionTarget
{
    public EnemyAttractionSource Source { get; }
    public Vector2 Position { get; }
    public Transform Transform { get; }

    public EnemyAttractionTarget(EnemyAttractionSource source, Vector2 position, Transform transform)
    {
        Source = source;
        Position = position;
        Transform = transform;
    }

    public bool IsValid => Source != EnemyAttractionSource.None;

    public bool IsHuntOnly =>
        Source == EnemyAttractionSource.MeatBait
        || Source == EnemyAttractionSource.ToyCar;
}
