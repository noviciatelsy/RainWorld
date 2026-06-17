using System;
using UnityEngine;

/// <summary>
/// 电梯平台：FixedUpdate 内 MovePosition 移动（晚于 PlayerControl，避免上升时地面检测失效）。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(110)]
public class ElevatorPlatform : MovingGroundPlatform
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;

    [Header("Elevator")]
    [SerializeField] private Transform uiAnchor;

    public Transform UiAnchor => uiAnchor != null ? uiAnchor : transform;

    public event Action<Vector2> OnTravelFinished;

    private Vector2 targetWorldPosition;

    protected override void Awake()
    {
        base.Awake();
    }

    public void MoveToPosition(Vector3 worldPosition)
    {
        targetWorldPosition = worldPosition;
        isMoving = true;
        platformVelocity = Vector2.zero;
        frameDelta = Vector2.zero;
    }

    public void SnapToPosition(Vector3 worldPosition)
    {
        Vector2 delta = (Vector2)worldPosition - rb.position;
        isMoving = false;
        platformVelocity = Vector2.zero;
        frameDelta = delta;
        targetWorldPosition = worldPosition;
        rb.position = worldPosition;
    }

    private void FixedUpdate()
    {
        frameDelta = Vector2.zero;

        if (!isMoving)
        {
            platformVelocity = Vector2.zero;
            return;
        }

        Vector2 currentPos = rb.position;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetWorldPosition, moveSpeed * Time.fixedDeltaTime);
        Vector2 delta = newPos - currentPos;

        if (delta.sqrMagnitude > 0f)
        {
            SetFrameMotion(delta);
            rb.MovePosition(newPos);
        }
        else
        {
            ClearFrameMotion();
        }

        if (Vector2.Distance(newPos, targetWorldPosition) <= 0.001f)
        {
            rb.MovePosition(targetWorldPosition);
            isMoving = false;
            ClearFrameMotion();
            OnTravelFinished?.Invoke(targetWorldPosition);
        }
    }
}
