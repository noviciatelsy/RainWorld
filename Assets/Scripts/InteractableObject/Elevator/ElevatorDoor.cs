using System.Collections;
using UnityEngine;

/// <summary>
/// 电梯门：左右门 1s 平滑开关。
/// </summary>
[DisallowMultipleComponent]
public class ElevatorDoor : MonoBehaviour
{
    public enum DoorState
    {
        Closed,
        Open,
        Moving
    }

    [Header("Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Transform leftDoorPoint;
    [SerializeField] private Transform rightDoorPoint;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 1f;

    [Header("Collision")]
    [SerializeField] private string groundLayerName = "Ground";

    public DoorState CurrentState { get; private set; } = DoorState.Closed;
    public bool IsOpen => CurrentState == DoorState.Open;
    public bool IsAnimating => CurrentState == DoorState.Moving;

    private Vector3 leftClosedLocalPosition;
    private Vector3 rightClosedLocalPosition;
    private Coroutine doorRoutine;

    private void Awake()
    {
        ResolveReferences();
        CacheClosedPositions();
        SetupCollision();
        ApplyClosedStateImmediate();
    }

    private void OnValidate()
    {
        if (leftDoor == null)
        {
            leftDoor = transform.Find("LeftDoor");
        }

        if (rightDoor == null)
        {
            rightDoor = transform.Find("RightDoor");
        }

        if (leftDoorPoint == null)
        {
            leftDoorPoint = transform.Find("LeftDoorPoint");
        }

        if (rightDoorPoint == null)
        {
            rightDoorPoint = transform.Find("RightDoorPoint");
        }
    }

    public void OpenDoors()
    {
        if (leftDoor == null || rightDoor == null || leftDoorPoint == null || rightDoorPoint == null)
        {
            return;
        }

        if (IsOpen)
        {
            return;
        }

        StartDoorRoutine(
            leftDoorPoint.localPosition,
            rightDoorPoint.localPosition,
            DoorState.Open);
    }

    public void CloseDoors()
    {
        if (leftDoor == null || rightDoor == null)
        {
            return;
        }

        if (CurrentState == DoorState.Closed)
        {
            return;
        }

        StartDoorRoutine(leftClosedLocalPosition, rightClosedLocalPosition, DoorState.Closed);
    }

    public void ToggleDoors()
    {
        if (IsOpen)
        {
            CloseDoors();
        }
        else
        {
            OpenDoors();
        }
    }

    private void ResolveReferences()
    {
        if (leftDoor == null)
        {
            leftDoor = transform.Find("LeftDoor");
        }

        if (rightDoor == null)
        {
            rightDoor = transform.Find("RightDoor");
        }

        if (leftDoorPoint == null)
        {
            leftDoorPoint = transform.Find("LeftDoorPoint");
        }

        if (rightDoorPoint == null)
        {
            rightDoorPoint = transform.Find("RightDoorPoint");
        }
    }

    private void CacheClosedPositions()
    {
        leftClosedLocalPosition = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
        rightClosedLocalPosition = rightDoor != null ? rightDoor.localPosition : Vector3.zero;
    }

    private void ApplyClosedStateImmediate()
    {
        if (leftDoor != null)
        {
            leftDoor.localPosition = leftClosedLocalPosition;
        }

        if (rightDoor != null)
        {
            rightDoor.localPosition = rightClosedLocalPosition;
        }

        CurrentState = DoorState.Closed;
    }

    private void SetupCollision()
    {
        int groundLayer = LayerMask.NameToLayer(groundLayerName);

        if (groundLayer < 0)
        {
            Debug.LogWarning($"{nameof(ElevatorDoor)}: 未找到 Layer '{groundLayerName}'。", this);
        }

        ApplyGroundLayer(leftDoor, groundLayer);
        ApplyGroundLayer(rightDoor, groundLayer);
    }

    private static void ApplyGroundLayer(Transform doorRoot, int groundLayer)
    {
        if (doorRoot == null || groundLayer < 0)
        {
            return;
        }

        doorRoot.gameObject.layer = groundLayer;

        Collider2D[] colliders = doorRoot.GetComponents<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider == null)
            {
                continue;
            }

            collider.isTrigger = false;
            collider.usedByEffector = false;
        }
    }

    private void StartDoorRoutine(Vector3 leftTarget, Vector3 rightTarget, DoorState endState)
    {
        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
        }

        doorRoutine = StartCoroutine(AnimateDoorsRoutine(leftTarget, rightTarget, endState));
    }

    private IEnumerator AnimateDoorsRoutine(Vector3 leftTarget, Vector3 rightTarget, DoorState endState)
    {
        CurrentState = DoorState.Moving;

        Vector3 leftStart = leftDoor.localPosition;
        Vector3 rightStart = rightDoor.localPosition;
        float duration = Mathf.Max(0.01f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, t);
            rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, t);

            yield return null;
        }

        leftDoor.localPosition = leftTarget;
        rightDoor.localPosition = rightTarget;
        CurrentState = endState;
        doorRoutine = null;
    }
}
