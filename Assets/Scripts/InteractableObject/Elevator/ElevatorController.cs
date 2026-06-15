using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElevatorFloorDock
{
    public ElevatorFloor floor;
    public Transform dockTransform;
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
        interactionFloor = zone.Floor;
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

        return DetectFloorAtPosition(elevatorPlatform.transform.position) == floor;
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
