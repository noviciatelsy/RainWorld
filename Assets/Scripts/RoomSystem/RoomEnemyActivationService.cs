using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据玩家当前所在房间，统一同步所有敌人的激活状态。
/// </summary>
public static class RoomEnemyActivationService
{
    private static readonly List<RoomEnemyMember> members = new List<RoomEnemyMember>(64);

    public static void Register(RoomEnemyMember member)
    {
        if (member == null || members.Contains(member))
        {
            return;
        }

        members.Add(member);
        SyncMemberForCurrentRoom(member);
    }

    private static void SyncMemberForCurrentRoom(RoomEnemyMember member)
    {
        RoomManager manager = RoomManager.Instance;

        if (manager == null || manager.CurrentRoom == null)
        {
            return;
        }

        bool shouldActivate = member.ShouldActivateForRoom(manager.CurrentRoom);
        member.ApplyActivation(shouldActivate);
    }

    public static void Unregister(RoomEnemyMember member)
    {
        if (member == null)
        {
            return;
        }

        members.Remove(member);
    }

    /// <summary>
    /// 玩家进入 <paramref name="currentRoom"/> 时调用：激活关联该房间的敌人，禁用其余敌人。
    /// </summary>
    public static void SyncForRoom(RoomController currentRoom)
    {
        for (int i = 0; i < members.Count; i++)
        {
            RoomEnemyMember member = members[i];

            if (member == null)
            {
                continue;
            }

            bool shouldActivate = member.ShouldActivateForRoom(currentRoom);
            member.ApplyActivation(shouldActivate);
        }
    }

    /// <summary>
    /// 按世界坐标查找包含该点的房间；供敌人未手动指定关联房间时使用。
    /// </summary>
    public static RoomController FindRoomContainingPosition(Vector2 worldPosition)
    {
        RoomManager manager = RoomManager.Instance;

        if (manager != null)
        {
            RoomController found = manager.FindRoomContainingPosition(worldPosition);

            if (found != null)
            {
                return found;
            }
        }

        RoomController[] rooms = Object.FindObjectsByType<RoomController>(FindObjectsSortMode.None);

        for (int i = 0; i < rooms.Length; i++)
        {
            RoomController room = rooms[i];

            if (room != null && room.ContainsPosition(worldPosition))
            {
                return room;
            }
        }

        return null;
    }
}
