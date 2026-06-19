using System.Collections.Generic;
using UnityEngine;

public class RoomEnemyMember : MonoBehaviour
{
    [Header("????????")]
    [Tooltip("?????????????????????????????????????????????? RoomController??????��?��??????")]
    [SerializeField] private List<RoomController> overlapActiveRooms = new List<RoomController>();

    [Header("???��????")]
    private bool isRoomActive;
    private bool activationRoomsResolved;
    private readonly List<RoomController> resolvedActivationRooms = new List<RoomController>(4);

    public bool IsRoomActive => isRoomActive;
    public IReadOnlyList<RoomController> OverlapActiveRooms => overlapActiveRooms;

    private void Awake()
    {
        ResolveActivationRooms();
        isRoomActive = true;
        ApplyActivation(false);
        RoomEnemyActivationService.Register(this);
    }

    private void OnDestroy()
    {
        RoomEnemyActivationService.Unregister(this);
    }

    /// <summary>
    /// ??????????????????????
    /// </summary>
    public bool ShouldActivateForRoom(RoomController currentRoom)
    {
        if (currentRoom == null)
        {
            return false;
        }

        if (!activationRoomsResolved)
        {
            ResolveActivationRooms();
        }

        for (int i = 0; i < resolvedActivationRooms.Count; i++)
        {
            if (resolvedActivationRooms[i] == currentRoom)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ?? RoomEnemyActivationService ????????????��??��? GameObject??
    /// </summary>
    public void ApplyActivation(bool active)
    {
        if (isRoomActive == active && gameObject.activeSelf == active)
        {
            return;
        }

        isRoomActive = active;
        gameObject.SetActive(active);
    }

    /// <summary>
    /// ????????RoomController ????????????????????
    /// </summary>
    public void SetRoomActive(bool active)
    {
        ApplyActivation(active);
    }

    private void ResolveActivationRooms()
    {
        resolvedActivationRooms.Clear();

        if (overlapActiveRooms != null)
        {
            for (int i = 0; i < overlapActiveRooms.Count; i++)
            {
                RoomController room = overlapActiveRooms[i];

                if (room != null && !resolvedActivationRooms.Contains(room))
                {
                    resolvedActivationRooms.Add(room);
                }
            }
        }

        if (resolvedActivationRooms.Count == 0)
        {
            RoomController parentRoom = GetComponentInParent<RoomController>();

            if (parentRoom != null)
            {
                resolvedActivationRooms.Add(parentRoom);
            }
            else
            {
                RoomController roomAtPosition =
                    RoomEnemyActivationService.FindRoomContainingPosition(transform.position);

                if (roomAtPosition != null)
                {
                    resolvedActivationRooms.Add(roomAtPosition);
                }
            }
        }

        activationRoomsResolved = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        activationRoomsResolved = false;
    }
#endif
}
