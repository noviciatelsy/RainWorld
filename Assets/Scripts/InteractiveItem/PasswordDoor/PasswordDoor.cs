using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 密码门：进入触发区显示提示 UI，E 进入四位密码输入，正确后向上开门；离区关门并重置。
/// 成功解锁一次后写入存档，之后进入触发区自动开门。
/// </summary>
[DisallowMultipleComponent]
public class PasswordDoor : MonoBehaviour
{
    private enum DoorUiState
    {
        Hidden,
        Prompt,
        Input,
        Open
    }

    [Header("Door")]
    [SerializeField] private Transform doorRoot;
    [SerializeField] private float openDistance = 2f;
    [SerializeField] private float openDuration = 0.3f;

    [Header("UI")]
    [SerializeField] private PasswordDoorUI doorUI;

    [Header("Password")]
    [SerializeField] private string correctPassword = "mksl";
    [SerializeField] private int passwordLength = 4;

    [Header("Save")]
    [SerializeField] private string doorSaveID = "PasswordDoor_Default";

    private readonly StringBuilder inputBuffer = new StringBuilder(4);

    private DoorUiState uiState = DoorUiState.Hidden;
    private PasswordDoorInteractZone activeZone;
    private Vector3 closedDoorPosition;
    private Coroutine doorAnimation;
    private bool isDoorOpen;
    private bool isPermanentlyUnlocked;
    private bool isSubscribedToSaveManager;
    private bool playerInputLocked;
    private int ignoreExitInputFrames;
    private MainInput mainInput;

    private void Awake()
    {
        if (doorRoot == null)
        {
            doorRoot = transform;
        }

        if (doorUI == null)
        {
            doorUI = GetComponentInChildren<PasswordDoorUI>(true);
        }

        closedDoorPosition = doorRoot.position;
        mainInput = InputManager.Instance != null ? InputManager.Instance.mainInput : null;
    }

    private void OnEnable()
    {
        TrySubscribeSaveManager();
        LoadUnlockStateFromSave();
        ApplyPermanentUnlockVisualImmediate();
    }

    private void OnDisable()
    {
        UnsubscribeSaveManager();
        ResetSession(false);
        SetPlayerInputLocked(false);
        ElevatorInputGate.SetPasswordDoorInputBlocking(false);
        ElevatorInputGate.SetPasswordDoorUiBlocking(false);
    }

    private void Update()
    {
        if (uiState != DoorUiState.Input)
        {
            return;
        }

        HandleKeyboardInput();
        HandleExitInput();
    }

    private void EnterInputMode()
    {
        uiState = DoorUiState.Input;
        ClearInputBuffer();
        ignoreExitInputFrames = 2;
        SetPlayerInputLocked(true);
        ElevatorInputGate.SetPasswordDoorInputBlocking(true);
        doorUI?.ShowInputMode(FormatPasswordDisplay());
    }

    public void NotifyPlayerEntered(PasswordDoorInteractZone zone, Vector3 playerWorldPosition)
    {
        if (zone == null)
        {
            return;
        }

        activeZone = zone;

        if (isPermanentlyUnlocked)
        {
            uiState = DoorUiState.Open;
            ElevatorInputGate.SetPasswordDoorInputBlocking(false);
            ElevatorInputGate.SetPasswordDoorUiBlocking(false);
            doorUI?.Hide();
            SetDoorOpen(true);
            return;
        }

        if (uiState == DoorUiState.Open)
        {
            return;
        }

        uiState = DoorUiState.Prompt;
        ClearInputBuffer();
        doorUI?.SetUiAnchor(zone.UiAnchor);
        doorUI?.SetApproachFromLeft(IsPlayerApproachingFromLeft(zone, playerWorldPosition));
        doorUI?.ShowPrompt();
        ElevatorInputGate.SetPasswordDoorInputBlocking(false);
        ElevatorInputGate.SetPasswordDoorUiBlocking(true);
    }

    private static bool IsPlayerApproachingFromLeft(
        PasswordDoorInteractZone zone,
        Vector3 playerWorldPosition)
    {
        float anchorX = zone.UiAnchor.position.x;
        return playerWorldPosition.x < anchorX;
    }

    public void NotifyPlayerExited(PasswordDoorInteractZone zone)
    {
        if (zone == null || activeZone != zone)
        {
            return;
        }

        activeZone = null;

        if (isPermanentlyUnlocked)
        {
            return;
        }

        ResetSession(true);
    }

    public void OnInteract(PasswordDoorInteractZone zone)
    {
        if (zone == null || activeZone != zone || isPermanentlyUnlocked || uiState != DoorUiState.Prompt)
        {
            return;
        }

        EnterInputMode();
    }

    private void ExitInputModeToPrompt()
    {
        uiState = DoorUiState.Prompt;
        ClearInputBuffer();
        SetPlayerInputLocked(false);
        ElevatorInputGate.SetPasswordDoorInputBlocking(false);
        ElevatorInputGate.SetPasswordDoorUiBlocking(true);
        doorUI?.ShowPrompt();
    }

    private void HandleKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || inputBuffer.Length >= passwordLength)
        {
            return;
        }

        for (Key key = Key.A; key <= Key.Z; key++)
        {
            if (key == Key.E || !keyboard[key].wasPressedThisFrame)
            {
                continue;
            }

            char letter = (char)('a' + (key - Key.A));
            inputBuffer.Append(letter);
            doorUI?.UpdatePasswordDisplay(FormatPasswordDisplay());

            if (inputBuffer.Length >= passwordLength)
            {
                TrySubmitPassword();
            }

            return;
        }
    }

    private void HandleExitInput()
    {
        if (ignoreExitInputFrames > 0)
        {
            ignoreExitInputFrames--;
            return;
        }

        if (!WasExitPressedThisFrame())
        {
            return;
        }

        ExitInputModeToPrompt();
    }

    private void TrySubmitPassword()
    {
        if (inputBuffer.Length < passwordLength)
        {
            return;
        }

        if (IsPasswordCorrect())
        {
            OnPasswordAccepted();
            return;
        }

        OnPasswordRejected();
    }

    private bool WasExitPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
    }

    private bool IsPasswordCorrect()
    {
        return string.Equals(
            inputBuffer.ToString(),
            correctPassword,
            System.StringComparison.OrdinalIgnoreCase);
    }

    private void OnPasswordAccepted()
    {
        isPermanentlyUnlocked = true;
        SaveUnlockStateToRunData();

        uiState = DoorUiState.Open;
        ClearInputBuffer();
        SetPlayerInputLocked(false);
        ElevatorInputGate.SetPasswordDoorInputBlocking(false);
        ElevatorInputGate.SetPasswordDoorUiBlocking(false);
        doorUI?.Hide();
        SetDoorOpen(true);
    }

    private void OnPasswordRejected()
    {
        ClearInputBuffer();
        ExitInputModeToPrompt();
    }

    private void ResetSession(bool closeDoor)
    {
        if (isPermanentlyUnlocked)
        {
            return;
        }

        uiState = DoorUiState.Hidden;
        ClearInputBuffer();
        SetPlayerInputLocked(false);
        ElevatorInputGate.SetPasswordDoorInputBlocking(false);
        ElevatorInputGate.SetPasswordDoorUiBlocking(false);
        doorUI?.Hide();

        if (closeDoor)
        {
            SetDoorOpen(false);
        }
    }

    private void SetPlayerInputLocked(bool locked)
    {
        if (mainInput == null || playerInputLocked == locked)
        {
            return;
        }

        playerInputLocked = locked;

        if (locked)
        {
            mainInput.Player.Disable();
            mainInput.UI.Disable();
            return;
        }

        mainInput.Player.Enable();
        mainInput.UI.Enable();
    }

    private void ClearInputBuffer()
    {
        inputBuffer.Clear();
    }

    private string FormatPasswordDisplay()
    {
        var display = new StringBuilder(passwordLength * 2 - 1);

        for (int i = 0; i < passwordLength; i++)
        {
            if (i > 0)
            {
                display.Append(' ');
            }

            display.Append(i < inputBuffer.Length ? inputBuffer[i] : '_');
        }

        return display.ToString();
    }

    private void SetDoorOpen(bool open)
    {
        if (doorAnimation != null)
        {
            StopCoroutine(doorAnimation);
            doorAnimation = null;
        }

        if (open == isDoorOpen && doorAnimation == null)
        {
            return;
        }

        doorAnimation = StartCoroutine(AnimateDoor(open));
    }

    private void ApplyPermanentUnlockVisualImmediate()
    {
        if (!isPermanentlyUnlocked)
        {
            return;
        }

        isDoorOpen = true;
        doorRoot.position = closedDoorPosition + Vector3.up * openDistance;
    }

    private IEnumerator AnimateDoor(bool open)
    {
        Vector3 startPos = doorRoot.position;
        Vector3 targetPos = closedDoorPosition + (open ? Vector3.up * openDistance : Vector3.zero);
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = openDuration > 0f ? Mathf.Clamp01(elapsed / openDuration) : 1f;
            doorRoot.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        doorRoot.position = targetPos;
        isDoorOpen = open;
        doorAnimation = null;
    }

    private void TrySubscribeSaveManager()
    {
        if (isSubscribedToSaveManager || SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnGameRunDataOverwrite += LoadUnlockStateFromSave;
        isSubscribedToSaveManager = true;
    }

    private void UnsubscribeSaveManager()
    {
        if (!isSubscribedToSaveManager || SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnGameRunDataOverwrite -= LoadUnlockStateFromSave;
        isSubscribedToSaveManager = false;
    }

    private void LoadUnlockStateFromSave()
    {
        isPermanentlyUnlocked = false;

        if (string.IsNullOrWhiteSpace(doorSaveID) || SaveManager.Instance == null)
        {
            return;
        }

        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return;
        }

        runData.EnsureDataValid();
        isPermanentlyUnlocked = runData.unlockedPasswordDoors.Contains(doorSaveID);
    }

    private void SaveUnlockStateToRunData()
    {
        if (string.IsNullOrWhiteSpace(doorSaveID) || SaveManager.Instance == null)
        {
            return;
        }

        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return;
        }

        runData.EnsureDataValid();

        if (runData.unlockedPasswordDoors == null)
        {
            runData.unlockedPasswordDoors = new System.Collections.Generic.List<string>();
        }

        if (!runData.unlockedPasswordDoors.Contains(doorSaveID))
        {
            runData.unlockedPasswordDoors.Add(doorSaveID);
        }

        SaveManager.Instance.SaveGame();
    }
}
