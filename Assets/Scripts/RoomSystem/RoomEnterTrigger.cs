using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomEnterTrigger : MonoBehaviour
{
    private RoomController ownerRoom;

    private void Awake()
    {
        ownerRoom = GetComponentInParent<RoomController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        NotifyOwnerRoom(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 防止玩家出生点、场景加载、或某些特殊位移刚好已经在 Trigger 内，
        // 但没有触发 Enter 的情况。
        NotifyOwnerRoom(other);
    }

    private void NotifyOwnerRoom(Collider2D other)
    {
        if (ownerRoom == null)
        {
            return;
        }
        if (other.GetComponent<Player>() != null)
            ownerRoom.NotifyPlayerEnteredSwitchTrigger(other);
    }
}