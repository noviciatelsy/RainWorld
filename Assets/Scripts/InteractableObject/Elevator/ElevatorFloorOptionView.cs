using TMPro;
using UnityEngine;

/// <summary>
/// 单层选项：Sprite 白底 + TextMeshPro 黑字（世界空间 GameObject）。
/// </summary>
public class ElevatorFloorOptionView : MonoBehaviour
{
    [SerializeField] private Transform rootTransform;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TextMeshPro labelText;

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
        SetAlpha(alpha);
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

        if (backgroundSprite != null)
        {
            Vector2 spriteSize = backgroundSprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                backgroundGo.transform.localScale = new Vector3(
                    backgroundWorldSize.x / spriteSize.x,
                    backgroundWorldSize.y / spriteSize.y,
                    1f
                );
            }
        }

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
        label.font = fontAsset;
        label.text = "地面";
        label.fontSize = fontSize;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;
        label.rectTransform.sizeDelta = backgroundWorldSize;
        label.enableWordWrapping = false;
        label.sortingLayerID = sortingLayerId;
        label.sortingOrder = textSortingOrder;

        ElevatorFloorOptionView view = root.GetComponent<ElevatorFloorOptionView>();
        view.rootTransform = root.transform;
        view.backgroundRenderer = background;
        view.labelText = label;
        return view;
    }
}
