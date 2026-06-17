using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在 ElevatorPlatform 子物体上，用 OverlapBox 检测 DoorTriggerZone（无 Collider，避免干扰玩家站立）。
/// </summary>
public class ElevatorPlatformDoorDetector : MonoBehaviour
{
    [SerializeField] private Vector2 detectSize = new Vector2(5f, 3f);
    [SerializeField] private Vector2 localOffset = new Vector2(0f, 0.25f);

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];
    private readonly HashSet<ElevatorDoorTriggerZone> activeZones = new HashSet<ElevatorDoorTriggerZone>();
    private readonly HashSet<ElevatorDoorTriggerZone> zonesThisFrame = new HashSet<ElevatorDoorTriggerZone>();

    private ContactFilter2D overlapFilter;

    private void Awake()
    {
        overlapFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false
        };
    }

    private void FixedUpdate()
    {
        zonesThisFrame.Clear();

        Vector2 center = transform.TransformPoint(localOffset);
        float angle = transform.eulerAngles.z;
        int count = Physics2D.OverlapBox(center, detectSize, angle, overlapFilter, overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapBuffer[i];

            if (hit == null)
            {
                continue;
            }

            ElevatorDoorTriggerZone zone = hit.GetComponent<ElevatorDoorTriggerZone>();

            if (zone == null)
            {
                zone = hit.GetComponentInParent<ElevatorDoorTriggerZone>();
            }

            if (zone != null)
            {
                zonesThisFrame.Add(zone);
            }
        }

        foreach (ElevatorDoorTriggerZone zone in zonesThisFrame)
        {
            if (activeZones.Add(zone))
            {
                zone.OpenAllDoors();
            }
        }

        if (activeZones.Count == 0)
        {
            return;
        }

        tempExitZones.Clear();

        foreach (ElevatorDoorTriggerZone zone in activeZones)
        {
            if (!zonesThisFrame.Contains(zone))
            {
                tempExitZones.Add(zone);
            }
        }

        for (int i = 0; i < tempExitZones.Count; i++)
        {
            ElevatorDoorTriggerZone zone = tempExitZones[i];
            activeZones.Remove(zone);
            zone.CloseAllDoors();
        }
    }

    private readonly List<ElevatorDoorTriggerZone> tempExitZones = new List<ElevatorDoorTriggerZone>();

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Vector3 center = transform.TransformPoint(localOffset);
        Vector3 scale = transform.lossyScale;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, new Vector3(scale.x, scale.y, 1f));
        Gizmos.DrawWireCube(Vector3.zero, detectSize);
    }
#endif
}
