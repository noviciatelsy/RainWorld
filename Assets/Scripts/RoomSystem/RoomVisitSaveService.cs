using System;
using UnityEngine;

public class RoomVisitSaveService : MonoBehaviour
{
    public static RoomVisitSaveService Instance { get; private set; }

    public event Action<string> OnRoomVisited;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsRoomVisited(string roomSaveID)
    {
        if (string.IsNullOrWhiteSpace(roomSaveID))
        {
            return false;
        }

        GameRunData runData = GetCurrentRunData();

        if (runData == null)
        {
            return false;
        }

        runData.EnsureDataValid();

        if (runData.visitedRooms.TryGetValue(roomSaveID, out bool isVisited))
        {
            return isVisited;
        }

        return false;
    }

    public bool MarkRoomVisited(string roomSaveID, bool saveImmediately)
    {
        if (string.IsNullOrWhiteSpace(roomSaveID))
        {
            Debug.LogWarning("试图记录一个空的房间存档ID。");
            return false;
        }

        GameRunData runData = GetCurrentRunData();

        if (runData == null)
        {
            Debug.LogWarning("当前没有选中的局内存档，无法记录房间访问状态：" + roomSaveID);
            return false;
        }

        runData.EnsureDataValid();

        if (runData.visitedRooms.TryGetValue(roomSaveID, out bool isVisited) && isVisited)
        {
            return false;
        }

        runData.visitedRooms[roomSaveID] = true;

        OnRoomVisited?.Invoke(roomSaveID);

        if (saveImmediately && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        return true;
    }

    private GameRunData GetCurrentRunData()
    {
        if (SaveManager.Instance == null)
        {
            return null;
        }

        return SaveManager.Instance.GetRunTimeGameData();
    }
}