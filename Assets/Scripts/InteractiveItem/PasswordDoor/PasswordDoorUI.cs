using TMPro;
using UnityEngine;

/// <summary>
/// 密码门世界空间 UI：按钮 Sprite 背景 + TextMeshPro 白色加粗字。
/// </summary>
[DisallowMultipleComponent]
public class PasswordDoorUI : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private string buttonSpriteResourcePath = "textures/ui资源/InGameUI/设置界面ui/按钮";

    [Header("Layout")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [Tooltip("玩家从左侧进入时 UI 在 anchor.x - offset，从右侧进入时 anchor.x + offset")]
    [SerializeField] private float sideOffsetX = 1.5f;
    [SerializeField] private Vector2 optionWorldSize = new Vector2(2.4f, 0.65f);
    [SerializeField] private Vector3 backgroundBaseScale = Vector3.zero;
    [SerializeField] private float fontSize = 2.8f;
    [SerializeField] private float inputRowSpacing = 0.72f;
    [SerializeField] private string sortingLayerName = "InteractableObject";
    [SerializeField] private int backgroundSortingOrder = 200;
    [SerializeField] private int textSortingOrder = 201;

    private Transform visualRoot;
    private Transform uiAnchor;
    private ElevatorFloorOptionView promptView;
    private ElevatorFloorOptionView passwordView;
    private ElevatorFloorOptionView confirmView;
    private int sortingLayerId;
    private bool isVisible;
    private bool approachFromLeft;

    public bool IsVisible => isVisible;

    private void Awake()
    {
        sortingLayerId = SortingLayer.NameToID(sortingLayerName);
        ResolveDefaultAssets();
        EnsureUiHierarchy();
    }

    private void LateUpdate()
    {
        if (!isVisible)
        {
            return;
        }

        UpdateWorldPosition();
    }

    public void SetUiAnchor(Transform anchor)
    {
        uiAnchor = anchor;
        UpdateWorldPosition();
    }

    /// <summary>
    /// 玩家从 anchor 左侧进入时为 true（UI 显示在 anchor.x - sideOffsetX）。
    /// </summary>
    public void SetApproachFromLeft(bool fromLeft)
    {
        approachFromLeft = fromLeft;
        UpdateWorldPosition();
    }

    public void ShowPrompt()
    {
        EnsureUiHierarchy();
        isVisible = true;
        visualRoot.gameObject.SetActive(true);
        promptView.gameObject.SetActive(true);
        passwordView.gameObject.SetActive(false);
        confirmView.gameObject.SetActive(false);
        promptView.SetLabel("E 输入密码");
        UpdateWorldPosition();
    }

    public void ShowInputMode(string passwordDisplay)
    {
        EnsureUiHierarchy();
        isVisible = true;
        visualRoot.gameObject.SetActive(true);
        promptView.gameObject.SetActive(false);
        passwordView.gameObject.SetActive(true);
        confirmView.gameObject.SetActive(true);
        passwordView.SetLabel(passwordDisplay);
        confirmView.SetLabel("E 确认密码");
        UpdateWorldPosition();
    }

    public void UpdatePasswordDisplay(string passwordDisplay)
    {
        if (passwordView != null)
        {
            passwordView.SetLabel(passwordDisplay);
        }
    }

    public void Hide()
    {
        isVisible = false;

        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(false);
        }
    }

    private void UpdateWorldPosition()
    {
        if (visualRoot == null)
        {
            return;
        }

        Transform anchor = uiAnchor != null ? uiAnchor : transform;
        Vector3 anchorPos = anchor.position;
        Vector3 offset = anchor.TransformVector(worldOffset);
        float targetX = approachFromLeft
            ? anchorPos.x - sideOffsetX
            : anchorPos.x + sideOffsetX;
        visualRoot.position = new Vector3(targetX, anchorPos.y + offset.y, anchorPos.z + offset.z);
    }

    private void EnsureUiHierarchy()
    {
        if (visualRoot == null)
        {
            GameObject rootGo = new GameObject("PasswordDoorUIVisual");
            rootGo.transform.SetParent(transform, false);
            visualRoot = rootGo.transform;
        }

        if (promptView == null)
        {
            promptView = CreateOptionView("PromptOption");
            promptView.SetLocalPosition(Vector3.zero);
        }

        if (passwordView == null)
        {
            passwordView = CreateOptionView("PasswordOption");
            passwordView.SetLocalPosition(new Vector3(0f, inputRowSpacing * 0.5f, 0f));
        }

        if (confirmView == null)
        {
            confirmView = CreateOptionView("ConfirmOption");
            confirmView.SetLocalPosition(new Vector3(0f, -inputRowSpacing * 0.5f, 0f));
        }

        visualRoot.gameObject.SetActive(false);
    }

    private ElevatorFloorOptionView CreateOptionView(string objectName)
    {
        ElevatorFloorOptionView view = ElevatorFloorOptionView.Create(
            visualRoot,
            backgroundSprite,
            labelFont,
            optionWorldSize,
            backgroundBaseScale,
            fontSize,
            sortingLayerId,
            backgroundSortingOrder,
            textSortingOrder);
        view.name = objectName;
        view.ApplySelectedStyle();
        return view;
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
