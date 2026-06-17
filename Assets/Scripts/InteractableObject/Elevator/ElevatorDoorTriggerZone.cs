using UnityEngine;

/// <summary>
/// 每层电梯门停靠区（被动标记）：仅定义 Trigger 范围与关联门，由 ElevatorPlatformDoorDetector 检测。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ElevatorDoorTriggerZone : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private ElevatorDoor[] doors;
    [SerializeField] private bool collectDoorFromParent = true;

    [Header("Zone")]
    [SerializeField] private Collider2D zoneCollider;

    private void Awake()
    {
        ResolveDoors();
        SetupZoneCollider();
    }

    private void OnValidate()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider2D>();
        }

        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    public void OpenAllDoors()
    {
        ResolveDoors();

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null)
            {
                doors[i].OpenDoors();
            }
        }
    }

    public void CloseAllDoors()
    {
        ResolveDoors();

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null)
            {
                doors[i].CloseDoors();
            }
        }
    }

    private void ResolveDoors()
    {
        if (doors != null && doors.Length > 0)
        {
            return;
        }

        if (collectDoorFromParent)
        {
            ElevatorDoor parentDoor = GetComponentInParent<ElevatorDoor>();

            if (parentDoor != null)
            {
                doors = new[] { parentDoor };
                return;
            }
        }

        doors = GetComponentsInChildren<ElevatorDoor>(true);
    }

    private void SetupZoneCollider()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider2D>();
        }

        if (zoneCollider == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(8f, 4f);
            zoneCollider = box;
        }

        zoneCollider.isTrigger = true;
    }
}
