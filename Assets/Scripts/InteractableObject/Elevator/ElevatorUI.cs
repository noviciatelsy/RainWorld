using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 电梯选层 UI（世界空间 GameObject：按钮 Sprite + TextMeshPro 加粗白字）。
/// </summary>
[DisallowMultipleComponent]
public class ElevatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform optionsRoot;
    [SerializeField] private ElevatorUIAni uiAni;
    [SerializeField] private ElevatorFloorOptionView confirmView;
    [SerializeField] private Transform uiAnchor;

    [Header("Assets")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private string buttonSpriteResourcePath = "textures/ui资源/InGameUI/设置界面ui/按钮";

    [Header("Layout")]
    [SerializeField] private Vector3 worldOffset = new Vector3(2.2f, 1.2f, 0f);
    [SerializeField] private Vector2 optionWorldSize = new Vector2(2.4f, 0.65f);
    [SerializeField] private Vector3 backgroundBaseScale = Vector3.zero;
    [SerializeField] private float fontSize = 2.8f;
    [SerializeField] private string sortingLayerName = "InteractableObject";
    [SerializeField] private int backgroundSortingOrder = 200;
    [SerializeField] private int textSortingOrder = 201;

    private MainInput mainInput;
    private ElevatorController controller;
    private List<ElevatorFloor> unlockedFloors = new List<ElevatorFloor>();
    private int selectedIndex;
    private bool isOpen;
    private bool moveUpHeld;
    private bool moveDownHeld;
    private float inputCooldown;
    private int sortingLayerId;
    private const float InputRepeatDelay = 0.18f;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        mainInput = InputManager.Instance != null ? InputManager.Instance.mainInput : null;
        sortingLayerId = SortingLayer.NameToID(sortingLayerName);
        ResolveDefaultAssets();
        EnsureUiHierarchy();
    }

    private void Update()
    {
        if (!isOpen || controller == null)
        {
            return;
        }

        if (controller.IsMoving)
        {
            controller.CloseUi();
            Close();
            return;
        }

        HandleSelectionInput();
        HandleConfirmInput();
        UpdateConfirmPosition();
        UpdateWorldPosition();
    }

    public void SetUiAnchor(Transform anchor)
    {
        uiAnchor = anchor;
    }

    public void Open(ElevatorController owner, List<ElevatorFloor> floors, int initialSelectedIndex)
    {
        controller = owner;
        unlockedFloors = floors ?? new List<ElevatorFloor>();
        selectedIndex = Mathf.Clamp(initialSelectedIndex, 0, Mathf.Max(0, unlockedFloors.Count - 1));
        isOpen = true;
        inputCooldown = 0f;
        moveUpHeld = false;
        moveDownHeld = false;

        EnsureUiHierarchy();
        visualRoot.gameObject.SetActive(true);
        uiAni.RefreshInstant(unlockedFloors, selectedIndex);
        confirmView?.ApplySelectedStyle();
        UpdateConfirmPosition();
        UpdateWorldPosition();
    }

    public void Close()
    {
        isOpen = false;
        controller = null;

        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(false);
        }
    }

    public void TryConfirm()
    {
        if (!isOpen || controller == null || unlockedFloors.Count == 0)
        {
            return;
        }

        ElevatorFloor selectedFloor = unlockedFloors[selectedIndex];
        controller.ConfirmSelection(selectedFloor);
    }

    private void HandleSelectionInput()
    {
        if (unlockedFloors.Count <= 1)
        {
            return;
        }

        if (inputCooldown > 0f)
        {
            inputCooldown -= Time.unscaledDeltaTime;
        }

        Vector2 move = mainInput != null ? mainInput.Player.Move.ReadValue<Vector2>() : Vector2.zero;
        bool upPressed = move.y > 0.5f;
        bool downPressed = move.y < -0.5f;

        if (upPressed)
        {
            if (!moveUpHeld || inputCooldown <= 0f)
            {
                SelectPrevious();
                inputCooldown = InputRepeatDelay;
            }

            moveUpHeld = true;
        }
        else
        {
            moveUpHeld = false;
        }

        if (downPressed)
        {
            if (!moveDownHeld || inputCooldown <= 0f)
            {
                SelectNext();
                inputCooldown = InputRepeatDelay;
            }

            moveDownHeld = true;
        }
        else
        {
            moveDownHeld = false;
        }
    }

    private void HandleConfirmInput()
    {
        if (mainInput == null || !mainInput.Player.Interact.WasPerformedThisFrame())
        {
            return;
        }

        TryConfirm();
    }

    private void SelectPrevious()
    {
        if (selectedIndex <= 0)
        {
            return;
        }

        int newIndex = selectedIndex - 1;
        uiAni.PlaySelectionChange(newIndex, -1);
        selectedIndex = newIndex;
    }

    private void SelectNext()
    {
        if (selectedIndex >= unlockedFloors.Count - 1)
        {
            return;
        }

        int newIndex = selectedIndex + 1;
        uiAni.PlaySelectionChange(newIndex, 1);
        selectedIndex = newIndex;
    }

    private void UpdateConfirmPosition()
    {
        if (confirmView == null || uiAni == null)
        {
            return;
        }

        confirmView.SetLocalPosition(new Vector3(0f, uiAni.GetConfirmLocalY(), 0f));
    }

    private void UpdateWorldPosition()
    {
        if (visualRoot == null)
        {
            return;
        }

        Transform anchor = uiAnchor != null ? uiAnchor : transform;
        visualRoot.position = anchor.position + anchor.TransformVector(worldOffset);
    }

    private void EnsureUiHierarchy()
    {
        if (visualRoot == null)
        {
            GameObject rootGo = new GameObject("ElevatorUIVisual");
            rootGo.transform.SetParent(transform, false);
            visualRoot = rootGo.transform;
        }

        if (optionsRoot == null)
        {
            GameObject optionsGo = new GameObject("OptionsRoot");
            optionsGo.transform.SetParent(visualRoot, false);
            optionsRoot = optionsGo.transform;
        }

        if (uiAni == null)
        {
            uiAni = GetComponent<ElevatorUIAni>();
            if (uiAni == null)
            {
                uiAni = gameObject.AddComponent<ElevatorUIAni>();
            }

            uiAni.Initialize(optionsRoot, backgroundSprite, labelFont, sortingLayerId, optionWorldSize, backgroundBaseScale, fontSize);
        }

        if (confirmView == null)
        {
            confirmView = ElevatorFloorOptionView.Create(
                visualRoot,
                backgroundSprite,
                labelFont,
                optionWorldSize,
                backgroundBaseScale,
                fontSize,
                sortingLayerId,
                backgroundSortingOrder,
                textSortingOrder);
            confirmView.name = "ConfirmOption";
            confirmView.SetLabel("E 确定");
            confirmView.ApplySelectedStyle();
        }

        visualRoot.gameObject.SetActive(false);
    }

    private void ResolveDefaultAssets()
    {
        if (backgroundSprite == null && !string.IsNullOrWhiteSpace(buttonSpriteResourcePath))
        {
            backgroundSprite = Resources.Load<Sprite>(buttonSpriteResourcePath);
        }

        if (labelFont == null)
        {
            labelFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
    }
}
