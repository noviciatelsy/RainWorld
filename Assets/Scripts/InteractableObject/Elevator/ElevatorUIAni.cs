using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 电梯选层 UI 动画（世界空间 Transform）。
/// </summary>
public class ElevatorUIAni : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float slotSpacing = 0.75f;
    [SerializeField] private float confirmSpacing = 0.95f;
    [SerializeField] private int maxVisibleDistance = 2;

    [Header("Visual")]
    [SerializeField] private float selectedScale = 1f;
    [SerializeField] private float scaleStepPerDistance = 0.1f;
    [SerializeField] private float selectedAlpha = 1f;
    [SerializeField] private float alphaStepPerDistance = 0.2f;
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float minAlpha = 0.35f;

    [Header("Animation")]
    [SerializeField] private float scrollDuration = 0.18f;

    [Header("References")]
    [SerializeField] private Transform optionsRoot;
    [SerializeField] private ElevatorFloorOptionView optionPrefab;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private Vector2 optionWorldSize = new Vector2(2.4f, 0.65f);
    [SerializeField] private float fontSize = 2.8f;
    [SerializeField] private int sortingLayerId;
    [SerializeField] private int backgroundSortingOrder = 200;
    [SerializeField] private int textSortingOrder = 201;

    private readonly List<ElevatorFloorOptionView> optionViews = new List<ElevatorFloorOptionView>();
    private List<ElevatorFloor> visibleFloors = new List<ElevatorFloor>();
    private int selectedIndex;
    private Coroutine scrollRoutine;
    private float scrollOffset;

    public void Initialize(Transform root, Sprite sprite, TMP_FontAsset font, int layerId)
    {
        if (optionsRoot == null)
        {
            optionsRoot = root;
        }

        if (backgroundSprite == null)
        {
            backgroundSprite = sprite;
        }

        if (labelFont == null)
        {
            labelFont = font;
        }

        if (sortingLayerId == 0 && layerId != 0)
        {
            sortingLayerId = layerId;
        }

        EnsureOptionPool(3);
    }

    public void RefreshInstant(List<ElevatorFloor> floors, int newSelectedIndex)
    {
        visibleFloors = floors ?? new List<ElevatorFloor>();
        selectedIndex = Mathf.Clamp(newSelectedIndex, 0, Mathf.Max(0, visibleFloors.Count - 1));
        scrollOffset = 0f;
        ApplyVisualsInstant();
    }

    public void PlaySelectionChange(int newSelectedIndex, int scrollDirection)
    {
        if (visibleFloors == null || visibleFloors.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(newSelectedIndex, 0, visibleFloors.Count - 1);
        if (clampedIndex == selectedIndex)
        {
            return;
        }

        selectedIndex = clampedIndex;

        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
        }

        float startOffset = scrollDirection > 0 ? -slotSpacing : slotSpacing;
        scrollRoutine = StartCoroutine(ScrollRoutine(startOffset));
    }

    private IEnumerator ScrollRoutine(float startOffset)
    {
        scrollOffset = startOffset;
        float elapsed = 0f;

        while (elapsed < scrollDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scrollDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            scrollOffset = Mathf.Lerp(startOffset, 0f, smoothT);
            ApplyVisualsInstant();
            yield return null;
        }

        scrollOffset = 0f;
        ApplyVisualsInstant();
        scrollRoutine = null;
    }

    private void ApplyVisualsInstant()
    {
        EnsureOptionPool(Mathf.Max(visibleFloors.Count, 1));

        for (int i = 0; i < optionViews.Count; i++)
        {
            ElevatorFloorOptionView view = optionViews[i];
            if (view == null)
            {
                continue;
            }

            if (i >= visibleFloors.Count)
            {
                view.SetVisible(false);
                continue;
            }

            int distance = i - selectedIndex;
            if (Mathf.Abs(distance) > maxVisibleDistance)
            {
                view.SetVisible(false);
                continue;
            }

            view.SetVisible(true);
            view.SetLabel(ElevatorFloorUtility.ToDisplayName(visibleFloors[i]));

            float absDistance = Mathf.Abs(distance);
            float scale = Mathf.Max(minScale, selectedScale - absDistance * scaleStepPerDistance);
            float alpha = Mathf.Max(minAlpha, selectedAlpha - absDistance * alphaStepPerDistance);
            float y = -distance * slotSpacing + scrollOffset;

            view.SetLocalPosition(new Vector3(0f, y, 0f));
            view.SetVisual(scale, alpha);
        }
    }

    public float GetConfirmLocalY()
    {
        float lowestY = 0f;

        for (int i = 0; i < visibleFloors.Count; i++)
        {
            int distance = i - selectedIndex;
            if (Mathf.Abs(distance) > maxVisibleDistance)
            {
                continue;
            }

            float y = -distance * slotSpacing + scrollOffset;
            lowestY = Mathf.Min(lowestY, y);
        }

        return lowestY - confirmSpacing;
    }

    private void EnsureOptionPool(int count)
    {
        while (optionViews.Count < count)
        {
            ElevatorFloorOptionView created = optionPrefab != null
                ? Instantiate(optionPrefab, optionsRoot)
                : ElevatorFloorOptionView.Create(
                    optionsRoot,
                    backgroundSprite,
                    labelFont,
                    optionWorldSize,
                    fontSize,
                    sortingLayerId,
                    backgroundSortingOrder,
                    textSortingOrder);

            created.SetSorting(sortingLayerId, backgroundSortingOrder, textSortingOrder);
            created.gameObject.SetActive(false);
            optionViews.Add(created);
        }
    }
}
