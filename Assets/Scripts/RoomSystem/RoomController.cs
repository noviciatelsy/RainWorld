using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    private const string MinimapLayerName = "Minimap";

    [Header("房间存档")]
    [Tooltip("实际游戏房间的唯一 ID。由 RoomsSaveIDRoot 在编辑器中自动生成，不要手动修改。")]
    [SerializeField] private string roomSaveID;
    [SerializeField] private bool saveImmediatelyWhenFirstVisited = true;

    [Header("摄像机限制范围")]
    [SerializeField] private Collider2D cameraBoundsCollider;
    [SerializeField] private BoxCollider2D roomBoundsCollider;

    [Header("房间切换判定范围")]
    [SerializeField] private BoxCollider2D switchTriggerCollider;

    [Header("敌人")]
    [SerializeField] private bool autoCollectEnemiesInChildren = true;
    [SerializeField] private List<RoomEnemyMember> roomEnemies = new List<RoomEnemyMember>();

    [Header("房间小地图")]
    private readonly List<GameObject> miniMaps = new List<GameObject>();

    private RoomManager manager;

    private bool hasStarted;
    private bool isRegistered;
    private bool hasEnableMinimap;

    public Collider2D CameraBoundsCollider => cameraBoundsCollider;
    public string RoomSaveID => roomSaveID;

    private void Awake()
    {
        if (autoCollectEnemiesInChildren)
        {
            CollectEnemiesInChildren();
        }

        CollectMinimapObjectsInChildren();

        if (switchTriggerCollider != null)
        {
            switchTriggerCollider.isTrigger = true;
        }

        SyncMinimapFromCurrentRunData();
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

        SyncMinimapFromCurrentRunData();

        RegisterSelf();
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

    /// <summary>
    /// 将该房间列表中的敌人全部设为同一激活状态（兼容旧调用；切房逻辑请走 RoomEnemyActivationService）。
    /// </summary>
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

    private void CollectMinimapObjectsInChildren()
    {
        miniMaps.Clear();

        int minimapLayer = LayerMask.NameToLayer(MinimapLayerName);

        if (minimapLayer == -1)
        {
            Debug.LogWarning($"找不到名为 {MinimapLayerName} 的 Layer，房间 {name} 无法自动收集小地图物体。");
            return;
        }

        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i] == null)
            {
                continue;
            }

            // GetComponentsInChildren 会把自己也包含进去，这里只收集真正的子物体
            if (childTransforms[i] == transform)
            {
                continue;
            }

            GameObject childGameObject = childTransforms[i].gameObject;

            if (childGameObject.layer == minimapLayer)
            {
                miniMaps.Add(childGameObject);
            }
        }
    }

    private void SyncMinimapFromCurrentRunData()
    {
        bool shouldEnableMinimap = IsRoomVisitedInCurrentRunData();

        SetMinimapActive(shouldEnableMinimap);

        hasEnableMinimap = shouldEnableMinimap;
    }

    private void SetMinimapActive(bool active)
    {
        for (int i = 0; i < miniMaps.Count; i++)
        {
            if (miniMaps[i] != null)
            {
                miniMaps[i].SetActive(active);
            }
        }
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

    public void TryEnableMinimap()
    {
        if (hasEnableMinimap)
        {
            return;
        }

        SetMinimapActive(true);

        hasEnableMinimap = true;

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
        DrawBoxColliderGizmo(roomBoundsCollider, Color.blue);
        DrawBoxColliderGizmo(switchTriggerCollider, Color.green);
    }

    private void DrawBoxColliderGizmo(BoxCollider2D targetCollider, Color color)
    {
        if (targetCollider == null)
        {
            return;
        }

        Gizmos.color = color;

        // 保存原本的 Gizmos 矩阵，避免影响别的 Gizmos
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // 使用 Collider 自己的 Transform，避免 Collider 在子物体上时画错位置
        Gizmos.matrix = targetCollider.transform.localToWorldMatrix;

        // BoxCollider2D 的 offset 和 size 都是本地空间数据
        Gizmos.DrawWireCube(targetCollider.offset, targetCollider.size);

        Gizmos.matrix = oldMatrix;
    }
}