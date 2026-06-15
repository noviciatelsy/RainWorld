using UnityEngine;

/// <summary>
/// 玩家步行进入区域时解锁对应电梯楼层（乘移动中的电梯经过时不解锁）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ElevatorFloorUnlockTrigger : MonoBehaviour
{
    [SerializeField] private ElevatorController elevatorController;
    [SerializeField] private ElevatorFloor floorToUnlock;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (elevatorController == null)
        {
            elevatorController = FindObjectOfType<ElevatorController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        if (elevatorController == null)
        {
            return;
        }

        if (elevatorController.IsMoving)
        {
            return;
        }

        PlayerControl playerControl = other.GetComponentInParent<PlayerControl>();
        if (playerControl != null && playerControl.IsOnMovingElevator())
        {
            return;
        }

        elevatorController.UnlockFloor(floorToUnlock);
    }
}
