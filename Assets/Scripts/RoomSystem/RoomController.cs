using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("房间存档")]
    [Tooltip("实际游戏房间的唯一 ID。由 RoomsSaveIDRoot 在编辑器中自动生成，不要手动修改。")]
    [SerializeField] private string roomSaveID;
    [SerializeField] private bool saveImmediatelyWhenFirstVisited = true;

    [Header("摄像机限制范围")]
    [SerializeField] private BoxCollider2D boxBoundsCollider;
    [SerializeField] private Collider2D cameraBoundsCollider;

    [Header("房间切换判定范围")]
    [SerializeField] private BoxCollider2D switchTriggerCollider;

    [Header("敌人")]
    [SerializeField] private bool autoCollectEnemiesInChildren = true;
    [SerializeField] private List<RoomEnemyMember> roomEnemies = new List<RoomEnemyMember>();

    [Header("房间所有物")]
    [SerializeField] private GameObject roomContent;
    private RoomManager manager;

    private bool hasStarted;
    private bool isRegistered;
    private bool hasEnableRoomContent;

    public Collider2D CameraBoundsCollider => cameraBoundsCollider;
    public string RoomSaveID => roomSaveID;
    private void Awake()
    {
        if (autoCollectEnemiesInChildren)
        {
            CollectEnemiesInChildren();
        }

        if (switchTriggerCollider != null)
        {
            switchTriggerCollider.isTrigger = true;
        }

        SyncRoomContentFromCurrentRunData();
    }

    private void OnEnable()
    {
        // 第一次启用时不在 OnEnable 注册，避免 RoomManager 生命周期还没准备好
        // 第一次注册交给 Start；后续从禁用恢复时，再在 OnEnable 注册
        if (hasStarted)
        {
            RegisterSelf();
        }
    }

    private void Start()
    {
        hasStarted = true;

        SyncRoomContentFromCurrentRunData();

        RegisterSelf();

        // 初始先休眠，等 RoomManager 确认当前房间后再打开当前房间敌人。
        SetRoomActive(false);
    }

    private void OnDisable()
    {
        UnregisterSelf();
    }

    public void NotifyPlayerEnteredSwitchTrigger(Collider2D other)
    {
        if (manager == null)
        {
            manager = RoomManager.Instance;
        }

        if (manager == null)
        {
            return;
        }

        if (manager.enableRoomSwitchByCollider == false)
        {
            return;
        }
        manager.RequestSwitchRoom(this);
    }

    public bool ContainsPosition(Vector2 worldPosition)
    {
        // 初始房间判断用“大范围”的 cameraBoundsCollider 更合适。
        if (cameraBoundsCollider != null && cameraBoundsCollider.OverlapPoint(worldPosition))
        {
            return true;
        }

        if (switchTriggerCollider != null && switchTriggerCollider.OverlapPoint(worldPosition))
        {
            return true;
        }

        return false;
    }

    public void SetRoomActive(bool active)
    {
        for (int i = 0; i < roomEnemies.Count; i++)
        {
            if (roomEnemies[i] != null)
            {
                roomEnemies[i].SetRoomActive(active);
            }
        }
    }

    private void RegisterSelf()
    {
        if (isRegistered)
        {
            return;
        }

        if (manager == null)
        {
            manager = RoomManager.Instance;
        }

        if (manager == null)
        {
            manager = FindObjectOfType<RoomManager>();
        }

        if (manager == null)
        {
            Debug.LogWarning($"房间 {name} 找不到 RoomManager，暂时无法注册。");
            return;
        }

        manager.RegisterRoom(this);
        isRegistered = true;
    }

    private void UnregisterSelf()
    {
        if (!isRegistered)
        {
            return;
        }

        if (manager != null)
        {
            manager.UnregisterRoom(this);
        }

        isRegistered = false;
    }

    private void CollectEnemiesInChildren()
    {
        roomEnemies.Clear();

        RoomEnemyMember[] foundEnemies = GetComponentsInChildren<RoomEnemyMember>(true);

        for (int i = 0; i < foundEnemies.Length; i++)
        {
            if (foundEnemies[i] != null)
            {
                roomEnemies.Add(foundEnemies[i]);
            }
        }
    }

    private void SyncRoomContentFromCurrentRunData()
    {
        bool shouldEnableRoomContent = IsRoomVisitedInCurrentRunData();

        if (roomContent != null)
        {
            roomContent.SetActive(shouldEnableRoomContent);
        }

        hasEnableRoomContent = shouldEnableRoomContent;
    }

    private bool IsRoomVisitedInCurrentRunData()
    {
        if (string.IsNullOrWhiteSpace(roomSaveID))
        {
            return false;
        }

        if (RoomVisitSaveService.Instance == null)
        {
            return false;
        }

        return RoomVisitSaveService.Instance.IsRoomVisited(roomSaveID);
    }

    public void TryEnableRoomContent()
    {
        if (hasEnableRoomContent)
        {
            return;
        }

        if (roomContent != null)
        {
            roomContent.SetActive(true);
        }

        hasEnableRoomContent = true;

        RecordRoomVisitedToSave();
    }

    private void RecordRoomVisitedToSave()
    {
        if (string.IsNullOrWhiteSpace(roomSaveID))
        {
            Debug.LogWarning($"房间 {name} 没有设置 Room Save ID，无法记录访问状态。");
            return;
        }

        if (RoomVisitSaveService.Instance == null)
        {
            Debug.LogWarning($"房间 {name} 找不到 RoomVisitSaveService，无法记录访问状态。");
            return;
        }

        RoomVisitSaveService.Instance.MarkRoomVisited(roomSaveID, saveImmediatelyWhenFirstVisited);
    }

    private void OnDrawGizmos()
    {
        if (boxBoundsCollider == null)
        {
            return;
        }

        Gizmos.color = Color.blue;

        // 保存原本的 Gizmos 矩阵，避免影响别的 Gizmos
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxBoundsCollider.offset, boxBoundsCollider.size);
        Gizmos.matrix = oldMatrix;
    }
}