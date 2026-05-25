using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MoneyTrailGhostUI : MonoBehaviour
{
    [Header("Refs")]
    private Image iconImage;
    private CanvasGroup canvasGroup;

    [Header("Life")]
    [SerializeField] private float lifeTime = 0.14f;
    [SerializeField] private float startAlpha = 0.55f;
    [SerializeField] private float endAlpha = 0f;
    [SerializeField] private float startScaleMultiplier = 0.9f;
    [SerializeField] private float endScaleMultiplier = 0.45f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        iconImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();


        if (iconImage != null)
        {
            iconImage.raycastTarget = false;
        }
    }

    public void Setup(Sprite sprite, Vector2 anchoredPosition, Vector3 eulerAngles, Vector3 sourceScale)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localEulerAngles = eulerAngles;
            rectTransform.localScale = sourceScale * startScaleMultiplier;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = startAlpha;
        }

        StopAllCoroutines();
        StartCoroutine(FadeRoutine(sourceScale));
    }

    private IEnumerator FadeRoutine(Vector3 sourceScale)
    {
        float time = 0f;
        Vector3 startScale = sourceScale * startScaleMultiplier;
        Vector3 endScale = sourceScale * endScaleMultiplier;

        while (time < lifeTime)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / lifeTime);

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}