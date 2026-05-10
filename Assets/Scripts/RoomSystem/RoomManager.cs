using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }
    private Transform player;
    private RoomCameraController roomCameraController;

    [Header("初始房间")]
    [SerializeField] private RoomController initialRoom;

    [Tooltip("如果为 true，则会根据玩家出生位置自动查找初始房间")]
    [SerializeField] private bool findInitialRoomByPlayerPosition = true;

    [Header("黑屏控制")]
    [Tooltip("房间切换时是否使用黑屏过渡")]
    [SerializeField] private bool useFadeWhenSwitchRoom = true;
    [SerializeField] private bool disablePlayerControlWhenSwitchRoom=true;


    private readonly HashSet<RoomController> registeredRooms = new HashSet<RoomController>();

    private bool hasInitialized;
    private bool isRoomSwitchFadePlaying;
    private RoomController pendingRoomToSwitch;
    private MainInput mainInput;
    public bool enableRoomSwitchByCollider {  get; private set; }

    public RoomController CurrentRoom { get; private set; }

    public event System.Action<RoomController, RoomController> OnRoomChanged;
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        mainInput=InputManager.Instance.mainInput;
        enableRoomSwitchByCollider = false;
    }

    private IEnumerator Start()
    {
        // 等一帧，是为了让所有 RoomController 的 Start 都有机会先完成注册
        // 这样可以避开“RoomManager 先 Start，房间还没注册”的生命周期问题
        yield return null;
        roomCameraController=FindAnyObjectByType<RoomCameraController>();
        InitializeInitialRoomIfNeeded();
    }

    private void OnEnable()
    {
        TryGetPlayer(PlayerManager.Instance.TryGetCurrentPlayer());
        PlayerManager.Instance.OnPlayerRegistered += TryGetPlayer;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerRegistered -= TryGetPlayer;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterRoom(RoomController room)
    {
        if (room == null)
        {
            return;
        }

        registeredRooms.Add(room);

        // 如果游戏已经初始化过，后续重新启用的房间应该立刻同步自己的激活状态。
        if (hasInitialized)
        {
            room.SetRoomActive(room == CurrentRoom);
        }
    }

    public void UnregisterRoom(RoomController room)
    {
        if (room == null)
        {
            return;
        }

        registeredRooms.Remove(room);

        if (CurrentRoom == room)
        {
            CurrentRoom = null;
        }
    }

    public void RequestSwitchRoom(RoomController targetRoom)
    {
        if (targetRoom == null)
        {
            return;
        }

        if (!registeredRooms.Contains(targetRoom))
        {
            RegisterRoom(targetRoom);
        }
        if (!hasInitialized)
        {
            hasInitialized = true;
        }
        if (CurrentRoom == targetRoom)
        {
            return;
        }
        if (isRoomSwitchFadePlaying)
        {
            pendingRoomToSwitch = targetRoom;
            return;
        }
        if (useFadeWhenSwitchRoom && CurrentRoom != null)
        {
            StartRoomSwitchFade(targetRoom);
            return;
        }
    }
   
    private void InitializeInitialRoomIfNeeded()
    {
        if (hasInitialized)
        {
            return;
        }
        hasInitialized = true;

        if (roomCameraController != null && player != null)
        {

            roomCameraController.SetFollowTarget(player);
        }

        RoomController roomToEnter = initialRoom;

        if (findInitialRoomByPlayerPosition && player != null)
        {
            RoomController foundRoom = FindRoomContainingPosition(player.position);

            if (foundRoom != null)
            {
                roomToEnter = foundRoom;
            }
        }

        // 先把所有房间敌人休眠，再开启初始房间
        foreach (RoomController room in registeredRooms)
        {
            if (room != null)
            {
                room.SetRoomActive(false);
            }
        }

        if (roomToEnter != null)
        {
            SwitchToRoom(roomToEnter, true,true);
            enableRoomSwitchByCollider=true;
        }
        else
        {
            Debug.LogWarning("RoomManager 没有找到初始房间。请设置 initialRoom，或确保玩家出生点位于某个房间的 CameraBounds 内。");
        }
    }

    private RoomController FindRoomContainingPosition(Vector2 worldPosition)
    {
        foreach (RoomController room in registeredRooms)
        {
            if (room != null && room.ContainsPosition(worldPosition))
            {
                return room;
            }
        }

        return null;
    }

    private void StartRoomSwitchFade(RoomController targetRoom)
    {
        pendingRoomToSwitch = targetRoom;
        isRoomSwitchFadePlaying = true;

        SetRoomFadeControlledBehavioursActive(false);

        GlobalUI.Instance.fadeScreen.PlayRoomSwitchFade(
            HandleRoomSwitchBlackReached,
            HandleRoomSwitchFadeCompleted
        );
    }
    private void HandleRoomSwitchBlackReached()
    {
        if (pendingRoomToSwitch == null)
        {
            return;
        }

        RoomController targetRoom = pendingRoomToSwitch;
        pendingRoomToSwitch = null;

        SwitchToRoom(targetRoom, false, true);
    }

    private void HandleRoomSwitchFadeCompleted()
    {
        isRoomSwitchFadePlaying = false;
        pendingRoomToSwitch = null;

        SetRoomFadeControlledBehavioursActive(true);
    }

    private void SwitchToRoom(RoomController newRoom, bool isInitialRoom, bool forceCameraImmediately)
    {
        if (newRoom == null)
        {
            return;
        }

        if (CurrentRoom == newRoom)
        {
            return;
        }

        RoomController previousRoom = CurrentRoom;
        CurrentRoom = newRoom;

        if (previousRoom != null)
        {
            previousRoom.SetRoomActive(false);
        }

        CurrentRoom.SetRoomActive(true);
        CurrentRoom.TryEnableMinimap();
        if (roomCameraController != null)
        {
            roomCameraController.ApplyRoom(CurrentRoom, forceCameraImmediately);
        }

        OnRoomChanged?.Invoke(previousRoom, CurrentRoom);
    }

    private void SetRoomFadeControlledBehavioursActive(bool active)
    {
        if (!disablePlayerControlWhenSwitchRoom)
        {
            return;
        }

        if(active)
        {
            mainInput.Player.Enable();
        }
        else
        {
            mainInput.Player.Disable();
        }
    }

    private void TryGetPlayer(Player player)
    {
        if(player == null)
        {
            return;
        }
        this.player = player.transform;
    }
}