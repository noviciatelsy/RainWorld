using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElevatorFloorDock
{
    public ElevatorFloor floor;
    public Transform dockTransform;
}

[Serializable]
public class ElevatorFloorPresenceEntry
{
    public ElevatorFloor floor;
    [Tooltip("留空则使用 floorDocks 中同层 dockTransform")]
    public Transform boundsAnchor;
    [Tooltip("相对锚点的中心偏移与尺寸（XY 平面）")]
    public Bounds localBounds = new Bounds(Vector3.zero, new Vector3(8f, 4f, 0f));
    [Tooltip("在 localBounds 外再扩一圈（米）")]
    public float expandPadding;
}

/// <summary>
/// 电梯控制器：固定于场景，管理解锁、停靠点与 UI（不随电梯平台移动）。
/// </summary>
[DisallowMultipleComponent]
public class ElevatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ElevatorPlatform elevatorPlatform;
    [SerializeField] private ElevatorUI elevatorUI;
    [SerializeField] private Transform uiAnchor;
    [SerializeField] private ElevatorFloorDock[] floorDocks;

    [Header("Floor Presence")]
    [SerializeField] private ElevatorFloorPresenceEntry[] floorPresenceZones;
    [Tooltip("floorPresenceZones 为空时，从子物体 InteractZone Collider 自动生成并外扩")]
    [SerializeField] private bool autoBuildPresenceFromInteractZones = true;
    [SerializeField] private float interactZoneExpandPadding = 1.5f;
    [SerializeField] private bool enablePresenceDetection = true;
    [SerializeField] private bool ensurePresenceTriggerZonesOnAwake = true;
    [SerializeField] private Vector2 defaultPresenceSize = new Vector2(14f, 10f);
    [SerializeField] private float platformSnapTolerance = 0.05f;
    [SerializeField] private bool drawPresenceGizmos = true;

    [Header("Runtime")]
    [SerializeField] private ElevatorFloor currentFloor = ElevatorFloor.Ground;

    private bool caveUnlocked;
    private bool factoryUnlocked;
    private bool uiOpen;
    private bool isSubscribed;
    private ElevatorFloor interactionFloor;
    private ElevatorInteractZone activeZone;

    public bool IsMoving => elevatorPlatform != null && elevatorPlatform.IsMoving;
    public bool IsUiOpen => uiOpen;
    public bool CanOpenUi => !IsMoving;
    public ElevatorFloor CurrentFloor => currentFloor;
    public ElevatorFloor InteractionFloor => interactionFloor;

    private void Awake()
    {
        EnsurePlatformReference();
        EnsureFloorPresenceZones();
        EnsurePresenceTriggerZones();

        if (elevatorPlatform != null)
        {
            elevatorPlatform.OnTravelFinished += HandleTravelFinished;
        }
    }

    private void EnsurePlatformReference()
    {
        if (elevatorPlatform == null)
        {
            elevatorPlatform = FindObjectOfType<ElevatorPlatform>();
        }
    }

    public bool IsActiveZone(ElevatorInteractZone zone)
    {
        return activeZone == zone;
    }

    public void NotifyZoneEntered(ElevatorInteractZone zone)
    {
        if (zone == null || !CanOpenUi || elevatorUI == null)
        {
            return;
        }

        activeZone = zone;
        NotifyPlayerFloorPresence(zone.Floor, true);
        elevatorUI.SetUiAnchor(zone.UiAnchor);

        List<ElevatorFloor> unlockedFloors = GetUnlockedFloors();
        if (unlockedFloors.Count == 0)
        {
            return;
        }

        int selectedIndex = Mathf.Max(0, unlockedFloors.IndexOf(interactionFloor));
        uiOpen = true;
        elevatorUI.Open(this, unlockedFloors, selectedIndex);
        UpdateInputGate();
    }

    public void NotifyZoneExited(ElevatorInteractZone zone)
    {
        if (zone == null || activeZone != zone)
        {
            return;
        }

        activeZone = null;
        CloseUiInternal(true);
    }

    private void Start()
    {
        EnsurePlatformReference();
        EnsureFloorPresenceZones();
        EnsurePresenceTriggerZones();
    }

    private void OnEnable()
    {
        EnsurePlatformReference();
        TrySubscribeSaveManager();
        LoadUnlockStateFromSave();
        SyncCurrentFloorFromPosition();
        UpdateInputGate();
    }

    private void OnDisable()
    {
        UnsubscribeSaveManager();
        CloseUiInternal(false);
        UpdateInputGate();
    }

    private void OnDestroy()
    {
        if (elevatorPlatform != null)
        {
            elevatorPlatform.OnTravelFinished -= HandleTravelFinished;
        }
    }

    private void Update()
    {
        UpdateInputGate();
    }

    private void LateUpdate()
    {
        UpdateFloorFromPlayerPresence();
    }

    /// <summary>
    /// 玩家进入某层存在区时，将电梯平台瞬移到该层 dock 并同步当前层。
    /// </summary>
    public void NotifyPlayerFloorPresence(ElevatorFloor floor)
    {
        NotifyPlayerFloorPresence(floor, false);
    }

    private void NotifyPlayerFloorPresence(ElevatorFloor floor, bool forceWhileUiOpen)
    {
        if (IsMoving || !CanApplyPlayerPresence())
        {
            return;
        }

        EnsurePlatformReference();
        EnsurePlatformAtFloor(floor);

        if (uiOpen && !forceWhileUiOpen)
        {
            return;
        }

        currentFloor = floor;
        if (!uiOpen)
        {
            interactionFloor = floor;
        }
    }

    private bool CanApplyPlayerPresence()
    {
        PlayerControl playerControl = GetCurrentPlayerControl();
        return playerControl == null || !playerControl.IsOnMovingElevator();
    }

    private void UpdateFloorFromPlayerPresence()
    {
        if (!enablePresenceDetection || IsMoving || !CanApplyPlayerPresence())
        {
            return;
        }

        if (floorPresenceZones == null || floorPresenceZones.Length == 0)
        {
            return;
        }

        Player player = PlayerManager.Instance != null
            ? PlayerManager.Instance.TryGetCurrentPlayer()
            : null;
        if (player == null)
        {
            return;
        }

        Vector2 playerPosition = player.transform.position;
        ElevatorFloor bestFloor = currentFloor;
        float bestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < floorPresenceZones.Length; i++)
        {
            ElevatorFloorPresenceEntry entry = floorPresenceZones[i];
            if (entry == null)
            {
                continue;
            }

            Bounds worldBounds = GetPresenceBoundsWorld(entry);
            if (!RobotGroundPath.IsInsideBoundsXY(worldBounds, playerPosition, 0f))
            {
                continue;
            }

            float distance = Mathf.Abs(worldBounds.center.y - playerPosition.y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestFloor = entry.floor;
                found = true;
            }
        }

        if (found)
        {
            NotifyPlayerFloorPresence(bestFloor);
        }
    }

    private Bounds GetPresenceBoundsWorld(ElevatorFloorPresenceEntry entry)
    {
        Transform anchor = entry.boundsAnchor != null
            ? entry.boundsAnchor
            : GetDockTransform(entry.floor);

        Vector3 center = anchor != null
            ? anchor.position + entry.localBounds.center
            : entry.localBounds.center;

        Vector3 size = entry.localBounds.size;
        if (entry.expandPadding > 0f)
        {
            size += Vector3.one * entry.expandPadding * 2f;
        }

        size.z = 0f;
        return new Bounds(center, size);
    }

    private void EnsureFloorPresenceZones()
    {
        if (!autoBuildPresenceFromInteractZones || HasConfiguredPresenceZones())
        {
            return;
        }

        if (floorDocks != null && floorDocks.Length > 0)
        {
            BuildPresenceFromFloorDocks();
            return;
        }

        ElevatorInteractZone[] zones = GetComponentsInChildren<ElevatorInteractZone>(true);
        if (zones.Length == 0)
        {
            return;
        }

        floorPresenceZones = new ElevatorFloorPresenceEntry[zones.Length];
        for (int i = 0; i < zones.Length; i++)
        {
            ElevatorInteractZone zone = zones[i];
            BoxCollider2D box = zone.GetComponent<BoxCollider2D>();
            Transform anchor = GetDockTransform(zone.Floor);
            if (anchor == null)
            {
                anchor = zone.transform.parent;
            }

            Vector2 size = box != null ? box.size : defaultPresenceSize;
            Vector2 offset = box != null ? box.offset : Vector2.zero;
            Vector3 worldCenter = zone.transform.TransformPoint(offset);
            Vector3 localCenter = anchor != null
                ? anchor.InverseTransformPoint(worldCenter)
                : (Vector3)offset;

            floorPresenceZones[i] = new ElevatorFloorPresenceEntry
            {
                floor = zone.Floor,
                boundsAnchor = anchor,
                localBounds = new Bounds(localCenter, new Vector3(size.x, size.y, 0f)),
                expandPadding = interactZoneExpandPadding
            };
        }
    }

    private void BuildPresenceFromFloorDocks()
    {
        floorPresenceZones = new ElevatorFloorPresenceEntry[floorDocks.Length];
        for (int i = 0; i < floorDocks.Length; i++)
        {
            ElevatorFloorDock dockEntry = floorDocks[i];
            if (dockEntry == null || dockEntry.dockTransform == null)
            {
                continue;
            }

            Transform dock = dockEntry.dockTransform;
            ElevatorInteractZone zone = dock.GetComponentInChildren<ElevatorInteractZone>(true);
            Vector2 size = defaultPresenceSize;
            Vector3 localCenter = Vector3.zero;

            if (zone != null && zone.TryGetComponent(out BoxCollider2D box))
            {
                size = box.size + Vector2.one * interactZoneExpandPadding * 2f;
                Vector3 worldCenter = zone.transform.TransformPoint(box.offset);
                localCenter = dock.InverseTransformPoint(worldCenter);
            }

            floorPresenceZones[i] = new ElevatorFloorPresenceEntry
            {
                floor = dockEntry.floor,
                boundsAnchor = dock,
                localBounds = new Bounds(localCenter, new Vector3(size.x, size.y, 0f)),
                expandPadding = 0f
            };
        }
    }

    private bool HasConfiguredPresenceZones()
    {
        if (floorPresenceZones == null || floorPresenceZones.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < floorPresenceZones.Length; i++)
        {
            ElevatorFloorPresenceEntry entry = floorPresenceZones[i];
            if (entry != null && entry.localBounds.size.sqrMagnitude > 0.01f)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsurePresenceTriggerZones()
    {
        if (!ensurePresenceTriggerZonesOnAwake || floorDocks == null)
        {
            return;
        }

        int triggerLayer = LayerMask.NameToLayer("CanCollideWithPlayer");
        if (triggerLayer < 0)
        {
            triggerLayer = gameObject.layer;
        }

        for (int i = 0; i < floorDocks.Length; i++)
        {
            ElevatorFloorDock dockEntry = floorDocks[i];
            if (dockEntry == null || dockEntry.dockTransform == null)
            {
                continue;
            }

            Transform dock = dockEntry.dockTransform;
            ElevatorInteractZone zone = dock.GetComponentInChildren<ElevatorInteractZone>(true);
            Transform existing = dock.Find("PresenceBounds");
            GameObject presenceObject;

            if (existing != null)
            {
                presenceObject = existing.gameObject;
            }
            else
            {
                presenceObject = new GameObject("PresenceBounds");
                presenceObject.transform.SetParent(dock, false);
                presenceObject.AddComponent<BoxCollider2D>();
                presenceObject.AddComponent<ElevatorFloorPresenceZone>();
            }

            presenceObject.layer = triggerLayer;

            ElevatorFloorPresenceZone presenceZone = presenceObject.GetComponent<ElevatorFloorPresenceZone>();
            if (presenceZone != null)
            {
                presenceZone.Configure(this, dockEntry.floor, zone, interactZoneExpandPadding, defaultPresenceSize);
            }
        }
    }

    private PlayerControl GetCurrentPlayerControl()
    {
        Player player = PlayerManager.Instance != null
            ? PlayerManager.Instance.TryGetCurrentPlayer()
            : null;
        return player != null ? player.GetComponent<PlayerControl>() : null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoBuildPresenceFromInteractZones && !HasConfiguredPresenceZones())
        {
            EnsureFloorPresenceZones();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPresenceGizmos || floorPresenceZones == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
        for (int i = 0; i < floorPresenceZones.Length; i++)
        {
            ElevatorFloorPresenceEntry entry = floorPresenceZones[i];
            if (entry == null)
            {
                continue;
            }

            Bounds worldBounds = GetPresenceBoundsWorld(entry);
            Gizmos.DrawCube(worldBounds.center, worldBounds.size);
        }
    }
#endif

    public void BindElevatorPlatform(ElevatorPlatform platform)
    {
        if (elevatorPlatform == platform)
        {
            return;
        }

        if (elevatorPlatform != null)
        {
            elevatorPlatform.OnTravelFinished -= HandleTravelFinished;
        }

        elevatorPlatform = platform;

        if (elevatorPlatform != null)
        {
            elevatorPlatform.OnTravelFinished += HandleTravelFinished;
            SyncCurrentFloorFromPosition();
        }
    }

    public void OpenUi()
    {
        if (activeZone != null)
        {
            NotifyZoneEntered(activeZone);
        }
    }

    public void CloseUi()
    {
        CloseUiInternal(true);
    }

    public void ConfirmSelection(ElevatorFloor selectedFloor)
    {
        EnsurePlatformReference();

        if (IsMoving || !IsFloorUnlocked(selectedFloor))
        {
            return;
        }

        ElevatorFloor callFloor = interactionFloor;
        bool wasUiOpen = uiOpen;
        CloseUiInternal(true);

        if (!wasUiOpen)
        {
            return;
        }

        if (elevatorPlatform == null)
        {
            Debug.LogWarning("ElevatorController: 未绑定 ElevatorPlatform，无法移动电梯。", this);
            return;
        }

        if (selectedFloor == callFloor)
        {
            EnsurePlatformAtFloor(callFloor);
            return;
        }

        EnsurePlatformAtFloor(callFloor);

        Transform targetDock = GetDockTransform(selectedFloor);
        if (targetDock == null)
        {
            Debug.LogWarning($"ElevatorController: 楼层 {selectedFloor} 未配置 dockTransform。", this);
            return;
        }

        UpdateInputGate();
        elevatorPlatform.MoveToPosition(targetDock.position);
    }

    public void ConfirmSelectionViaUi()
    {
        if (!uiOpen || elevatorUI == null || IsMoving)
        {
            return;
        }

        elevatorUI.TryConfirm();
    }

    public bool IsFloorUnlocked(ElevatorFloor floor)
    {
        switch (floor)
        {
            case ElevatorFloor.Ground:
                return true;
            case ElevatorFloor.Cave:
                return caveUnlocked;
            case ElevatorFloor.Factory:
                return factoryUnlocked;
            default:
                return false;
        }
    }

    public void UnlockFloor(ElevatorFloor floor)
    {
        switch (floor)
        {
            case ElevatorFloor.Ground:
                break;
            case ElevatorFloor.Cave:
                if (caveUnlocked)
                {
                    return;
                }
                caveUnlocked = true;
                break;
            case ElevatorFloor.Factory:
                if (factoryUnlocked)
                {
                    return;
                }
                factoryUnlocked = true;
                break;
        }

        SaveUnlockStateToRunData();
    }

    public List<ElevatorFloor> GetUnlockedFloors()
    {
        List<ElevatorFloor> floors = new List<ElevatorFloor>(3);
        floors.Add(ElevatorFloor.Ground);

        if (caveUnlocked)
        {
            floors.Add(ElevatorFloor.Cave);
        }

        if (factoryUnlocked)
        {
            floors.Add(ElevatorFloor.Factory);
        }

        return floors;
    }

    public Transform GetDockTransform(ElevatorFloor floor)
    {
        if (floorDocks == null)
        {
            return null;
        }

        for (int i = 0; i < floorDocks.Length; i++)
        {
            ElevatorFloorDock dock = floorDocks[i];
            if (dock != null && dock.floor == floor && dock.dockTransform != null)
            {
                return dock.dockTransform;
            }
        }

        return null;
    }

    public void RegisterFirstArrival(ElevatorFloor floor)
    {
        UnlockFloor(floor);
    }

    private void HandleTravelFinished(Vector2 worldPosition)
    {
        ElevatorFloor arrivedFloor = DetectFloorAtPosition(worldPosition);
        currentFloor = arrivedFloor;
        UpdateInputGate();
    }

    private void CloseUiInternal(bool refreshView)
    {
        if (!uiOpen)
        {
            UpdateInputGate();
            return;
        }

        uiOpen = false;
        activeZone = null;

        if (refreshView && elevatorUI != null)
        {
            elevatorUI.Close();
        }

        UpdateInputGate();
    }

    private void EnsurePlatformAtFloor(ElevatorFloor floor)
    {
        if (elevatorPlatform == null || IsPlatformAtFloor(floor))
        {
            return;
        }

        Transform dock = GetDockTransform(floor);
        if (dock == null)
        {
            Debug.LogWarning($"ElevatorController: 楼层 {floor} 未配置 dockTransform。", this);
            return;
        }

        elevatorPlatform.SnapToPosition(dock.position);
        currentFloor = floor;
    }

    private bool IsPlatformAtFloor(ElevatorFloor floor)
    {
        if (elevatorPlatform == null)
        {
            return false;
        }

        Transform dock = GetDockTransform(floor);
        if (dock == null)
        {
            return false;
        }

        Vector2 platformPos = elevatorPlatform.transform.position;
        Vector2 dockPos = dock.position;
        return Vector2.Distance(platformPos, dockPos) <= platformSnapTolerance;
    }

    private void SyncCurrentFloorFromPosition()
    {
        if (elevatorPlatform == null)
        {
            return;
        }

        currentFloor = DetectFloorAtPosition(elevatorPlatform.transform.position);
    }

    private ElevatorFloor DetectFloorAtPosition(Vector3 worldPosition)
    {
        ElevatorFloor bestFloor = ElevatorFloor.Ground;
        float bestDistance = float.MaxValue;

        if (floorDocks == null)
        {
            return bestFloor;
        }

        for (int i = 0; i < floorDocks.Length; i++)
        {
            ElevatorFloorDock dock = floorDocks[i];
            if (dock == null || dock.dockTransform == null)
            {
                continue;
            }

            float distance = Mathf.Abs(dock.dockTransform.position.y - worldPosition.y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestFloor = dock.floor;
            }
        }

        return bestFloor;
    }

    private void UpdateInputGate()
    {
        // 仅移动中阻止 PlayerSensor；UI 打开时仍由 PlayerSensor 处理 E（同 LootArea）
        ElevatorInputGate.SetBlocking(IsMoving);
    }

    private void TrySubscribeSaveManager()
    {
        if (isSubscribed || SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnGameRunDataOverwrite += LoadUnlockStateFromSave;
        isSubscribed = true;
    }

    private void UnsubscribeSaveManager()
    {
        if (!isSubscribed || SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnGameRunDataOverwrite -= LoadUnlockStateFromSave;
        isSubscribed = false;
    }

    private void LoadUnlockStateFromSave()
    {
        caveUnlocked = false;
        factoryUnlocked = false;

        if (SaveManager.Instance == null)
        {
            return;
        }

        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return;
        }

        runData.EnsureDataValid();

        if (runData.unlockedElevatorFloors == null)
        {
            return;
        }

        for (int i = 0; i < runData.unlockedElevatorFloors.Count; i++)
        {
            string key = runData.unlockedElevatorFloors[i];
            if (key == ElevatorFloorUtility.ToSaveKey(ElevatorFloor.Cave))
            {
                caveUnlocked = true;
            }
            else if (key == ElevatorFloorUtility.ToSaveKey(ElevatorFloor.Factory))
            {
                factoryUnlocked = true;
            }
        }
    }

    private void SaveUnlockStateToRunData()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return;
        }

        runData.EnsureDataValid();

        if (runData.unlockedElevatorFloors == null)
        {
            runData.unlockedElevatorFloors = new List<string>();
        }

        runData.unlockedElevatorFloors.Clear();
        runData.unlockedElevatorFloors.Add(ElevatorFloorUtility.ToSaveKey(ElevatorFloor.Ground));

        if (caveUnlocked)
        {
            runData.unlockedElevatorFloors.Add(ElevatorFloorUtility.ToSaveKey(ElevatorFloor.Cave));
        }

        if (factoryUnlocked)
        {
            runData.unlockedElevatorFloors.Add(ElevatorFloorUtility.ToSaveKey(ElevatorFloor.Factory));
        }
    }
}
