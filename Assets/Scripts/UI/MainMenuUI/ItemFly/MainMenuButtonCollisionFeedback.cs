using System.Collections;
using UnityEngine;

public class MainMenuButtonCollisionFeedback : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private float moveDistanceMax = 10f;
    [SerializeField] private float scalePunchMax = 0.08f;
    [SerializeField] private float duration = 0.12f;

    private RectTransform rectTransform;
    private Coroutine bounceCoroutine;

    private Vector2 originalAnchoredPosition;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = originalScale;
    }

    public void PlayBounce(Vector2 normal, float impulseStrength)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
        }

        bounceCoroutine = StartCoroutine(BounceRoutine(normal, impulseStrength));
    }

    private IEnumerator BounceRoutine(Vector2 normal, float impulseStrength)
    {
        float moveDistance = Mathf.Clamp(impulseStrength * 0.015f, 2f, moveDistanceMax);
        float scalePunch = Mathf.Clamp(impulseStrength * 0.0004f, 0.02f, scalePunchMax);

        Vector2 punchPosition = originalAnchoredPosition + normal * moveDistance;
        Vector3 punchScale = originalScale * (1f + scalePunch);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float wave = Mathf.Sin(t * Mathf.PI);

            rectTransform.anchoredPosition = Vector2.Lerp(originalAnchoredPosition, punchPosition, wave);
            rectTransform.localScale = Vector3.Lerp(originalScale, punchScale, wave);

            yield return null;
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = originalScale;

        bounceCoroutine = null;
    }
}