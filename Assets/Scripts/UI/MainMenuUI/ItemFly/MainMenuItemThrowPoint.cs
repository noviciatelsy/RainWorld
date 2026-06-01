using UnityEngine;

public enum MainMenuThrowDirection
{
    Left = -1,
    Right = 1
}

public class MainMenuItemThrowPoint : MonoBehaviour
{
    [Header("Throw Direction")]
    [SerializeField] private MainMenuThrowDirection throwDirection = MainMenuThrowDirection.Left;

    [Header("Spawn Interval")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(0.12f, 0.45f);

    [Header("Initial Velocity")]
    [SerializeField] private Vector2 horizontalSpeedRange = new Vector2(450f, 900f);
    [SerializeField] private Vector2 verticalSpeedRange = new Vector2(550f, 1050f);

    [Header("Initial Rotation")]
    [SerializeField] private Vector2 initialAngleRange = new Vector2(-25f, 25f);

    [Tooltip("速度越大，初始旋转越快。左飞时逆时针，右飞时顺时针。")]
    [SerializeField] private float angularVelocityMultiplier = 0.35f;

    [SerializeField] private Vector2 angularVelocityRandomRange = new Vector2(0.85f, 1.25f);

    public int DirectionSign
    {
        get
        {
            return throwDirection == MainMenuThrowDirection.Left ? -1 : 1;
        }
    }

    public float RollSpawnInterval()
    {
        return Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
    }

    public Vector2 RollInitialVelocity()
    {
        float horizontalSpeed = Random.Range(horizontalSpeedRange.x, horizontalSpeedRange.y);
        float verticalSpeed = Random.Range(verticalSpeedRange.x, verticalSpeedRange.y);

        return new Vector2(horizontalSpeed * DirectionSign, verticalSpeed);
    }

    public float RollInitialAngle()
    {
        return Random.Range(initialAngleRange.x, initialAngleRange.y);
    }

    public float RollInitialAngularVelocity(Vector2 initialVelocity)
    {
        float speed = initialVelocity.magnitude;
        float randomMultiplier = Random.Range(angularVelocityRandomRange.x, angularVelocityRandomRange.y);

        // Unity UI 中 Z 轴正旋转通常表现为逆时针。
        // 左飞 DirectionSign = -1，所以 -DirectionSign = 1，逆时针。
        // 右飞 DirectionSign = 1，所以 -DirectionSign = -1，顺时针。
        return -DirectionSign * speed * angularVelocityMultiplier * randomMultiplier;
    }
}