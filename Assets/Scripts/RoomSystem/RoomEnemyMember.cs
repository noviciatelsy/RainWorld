using UnityEngine;

public class RoomEnemyMember : MonoBehaviour
{
    [Header("房间激活状态")]
    private bool isRoomActive = true;
    public bool IsRoomActive => isRoomActive;

    /// <summary>
    /// 由 RoomController 调用。
    /// 当玩家进入该房间时 active = true。
    /// 当玩家离开该房间时 active = false。
    /// </summary>
    public void SetRoomActive(bool active)
    {
        isRoomActive = active;
        gameObject.SetActive(active);
    }

}