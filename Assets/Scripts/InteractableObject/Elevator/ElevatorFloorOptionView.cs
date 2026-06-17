using TMPro;
using UnityEngine;

/// <summary>
/// 单层选项：按钮 Sprite + TextMeshPro 加粗白字（世界空间 GameObject）。
/// </summary>
public class ElevatorFloorOptionView : MonoBehaviour
{
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Transform backgroundTransform;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TextMeshPro labelText;

    private Vector3 backgroundBaseScale = Vector3.one;
    private static readonly Color SelectedTextColor = Color.white;

    public Transform RootTransform => rootTransform != null ? rootTransform : transform;

    private void Awake()
    {
        if (rootTransform == null)
        {
            rootTransform = transform;
        }

        if (backgroundRenderer == null)
        {
            backgroundRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (labelText == null)
        {
            labelText = GetComponentInChildren<TextMeshPro>();
        }
    }

    public void SetLabel(string text)
    {
        if (labelText != null)
        {
            labelText.text = text;
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetVisual(float scale, float alpha)
    {
        RootTransform.localScale = Vector3.one * scale;

        if (backgroundTransform != null)
        {
            backgroundTransform.localScale = backgroundBaseScale;
        }

        SetAlpha(alpha);
    }

    public void ApplySelectedStyle()
    {
        SetVisual(1f, 1f);
    }

    public void SetLocalPosition(Vector3 localPosition)
    {
        RootTransform.localPosition = localPosition;
    }

    public void SetSorting(int sortingLayerId, int backgroundOrder, int textOrder)
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.sortingLayerID = sortingLayerId;
            backgroundRenderer.sortingOrder = backgroundOrder;
        }

        if (labelText != null)
        {
            labelText.sortingLayerID = sortingLayerId;
            labelText.sortingOrder = textOrder;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (backgroundRenderer != null)
        {
            Color bgColor = backgroundRenderer.color;
            bgColor.a = alpha;
            backgroundRenderer.color = bgColor;
        }

        if (labelText != null)
        {
            Color textColor = labelText.color;
            textColor.a = alpha;
            labelText.color = textColor;
        }
    }

    public static ElevatorFloorOptionView Create(
        Transform parent,
        Sprite backgroundSprite,
        TMP_FontAsset fontAsset,
        Vector2 backgroundWorldSize,
        Vector3 backgroundBaseScale,
        float fontSize,
        int sortingLayerId,
        int backgroundSortingOrder,
        int textSortingOrder)
    {
        GameObject root = new GameObject("FloorOption", typeof(ElevatorFloorOptionView));
        root.transform.SetParent(parent, false);

        GameObject backgroundGo = new GameObject("Background");
        backgroundGo.transform.SetParent(root.transform, false);

        SpriteRenderer background = backgroundGo.AddComponent<SpriteRenderer>();
        background.sprite = backgroundSprite;
        background.color = Color.white;
        background.sortingLayerID = sortingLayerId;
        background.sortingOrder = backgroundSortingOrder;

        Vector3 resolvedBackgroundScale = ResolveBackgroundScale(
            backgroundSprite,
            backgroundWorldSize,
            backgroundBaseScale);
        backgroundGo.transform.localScale = resolvedBackgroundScale;

        Vector2 labelWorldSize = backgroundSprite != null
            ? new Vector2(
                backgroundSprite.bounds.size.x * resolvedBackgroundScale.x,
                backgroundSprite.bounds.size.y * resolvedBackgroundScale.y)
            : backgroundWorldSize;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
        label.font = fontAsset;
        label.text = "地面";
        label.fontSize = fontSize;
        label.color = SelectedTextColor;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;
        label.rectTransform.sizeDelta = labelWorldSize;
        label.enableWordWrapping = false;
        label.sortingLayerID = sortingLayerId;
        label.sortingOrder = textSortingOrder;

        ElevatorFloorOptionView view = root.GetComponent<ElevatorFloorOptionView>();
        view.rootTransform = root.transform;
        view.backgroundTransform = backgroundGo.transform;
        view.backgroundRenderer = background;
        view.labelText = label;
        view.backgroundBaseScale = resolvedBackgroundScale;
        return view;
    }

    private static Vector3 ResolveBackgroundScale(
        Sprite backgroundSprite,
        Vector2 backgroundWorldSize,
        Vector3 backgroundBaseScale)
    {
        if (backgroundBaseScale != Vector3.zero && backgroundBaseScale != Vector3.one)
        {
            return backgroundBaseScale;
        }

        if (backgroundSprite == null)
        {
            return new Vector3(0.2f, 0.2f, 1f);
        }

        Vector2 spriteSize = backgroundSprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return new Vector3(0.2f, 0.2f, 1f);
        }

        return new Vector3(
            backgroundWorldSize.x / spriteSize.x,
            backgroundWorldSize.y / spriteSize.y,
            1f);
    }
}
