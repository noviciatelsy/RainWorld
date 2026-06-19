using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 密码门：进入触发区显示提示 UI，E 进入四位密码输入，正确后向上开门；离区关门并重置。
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

    private readonly StringBuilder inputBuffer = new StringBuilder(4);

    private DoorUiState uiState = DoorUiState.Hidden;
    private PasswordDoorInteractZone activeZone;
    private Vector3 closedDoorPosition;
    private Coroutine doorAnimation;
    private bool isDoorOpen;
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

    private void OnDisable()
    {
        ResetSession(false);
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
        HandleConfirmInput();
    }

    public void NotifyPlayerEntered(PasswordDoorInteractZone zone, Vector3 playerWorldPosition)
    {
        if (zone == null || uiState == DoorUiState.Open)
        {
            activeZone = zone;
            return;
        }

        activeZone = zone;
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
        ResetSession(true);
    }

    public void OnInteract(PasswordDoorInteractZone zone)
    {
        if (zone == null || activeZone != zone || uiState != DoorUiState.Prompt)
        {
            return;
        }

        EnterInputMode();
    }

    private void EnterInputMode()
    {
        uiState = DoorUiState.Input;
        ClearInputBuffer();
        ElevatorInputGate.SetPasswordDoorInputBlocking(true);
        doorUI?.ShowInputMode(FormatPasswordDisplay());
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
            if (!keyboard[key].wasPressedThisFrame)
            {
                continue;
            }

            char letter = (char)('a' + (key - Key.A));
            inputBuffer.Append(letter);
            doorUI?.UpdatePasswordDisplay(FormatPasswordDisplay());
            return;
        }
    }

    private void HandleConfirmInput()
    {
        if (mainInput == null || !mainInput.Player.Interact.WasPerformedThisFrame())
        {
            return;
        }

        if (inputBuffer.Length < passwordLength)
        {
            return;
        }

        if (IsPasswordCorrect())
        {
            OnPasswordAccepted();
            return;
        }

        ClearInputBuffer();
        doorUI?.UpdatePasswordDisplay(FormatPasswordDisplay());
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
        uiState = DoorUiState.Open;
        ClearInputBuffer();
        ElevatorInputGate.SetPasswordDoorInputBlocking(false);
        ElevatorInputGate.SetPasswordDoorUiBlocking(false);
        doorUI?.Hide();
        SetDoorOpen(true);
    }

    private void ResetSession(bool closeDoor)
    {
        uiState = DoorUiState.Hidden;
        ClearInputBuffer();
        ElevatorInputGate.SetPasswordDoorInputBlocking(false);
        ElevatorInputGate.SetPasswordDoorUiBlocking(false);
        doorUI?.Hide();

        if (closeDoor)
        {
            SetDoorOpen(false);
        }
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
}
