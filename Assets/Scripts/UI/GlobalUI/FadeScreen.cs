using UnityEngine;
using System.Collections;

public class FadeScreen : MonoBehaviour
{
    [Header("References")]
    private CanvasGroup canvasGroup;

    [Header("SceneSwitch Fade Settings")]
    [SerializeField] private float sceneSwitchHoldTime = 0.5f;     // 黑屏停留时间
    [SerializeField] private float sceneSwitchFadeOutDuration = 0.25f; // 变黑时间
    [SerializeField] private float sceneSwitchFadeInDuration = 0.25f;  // 变亮时间

    private Coroutine fadeCoroutine;

    private void Awake()
    {

        canvasGroup = GetComponent<CanvasGroup>();

        // 默认不挡视野
        SetAlpha(0f);
    }

    public void PlaySceneSwitchFade(System.Action onBlackReached)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(sceneSwitchFadeCo(onBlackReached));
    }

    private IEnumerator sceneSwitchFadeCo(System.Action onBlackReached)
    {
        yield return FadeRoutine(1f, sceneSwitchFadeOutDuration);
        onBlackReached?.Invoke();
        yield return WaitUnscaledSeconds(sceneSwitchHoldTime);
        yield return FadeRoutine(0f, sceneSwitchFadeInDuration);
        fadeCoroutine = null;
    }


    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;
        float safeDuration = Mathf.Max(0.0001f, duration);


        // 已经是目标值就不折腾了（防御性写法）
        if (Mathf.Approximately(startAlpha, targetAlpha))
        {
            SetAlpha(targetAlpha);
            fadeCoroutine = null;
            yield break;
        }

        while (time < safeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / safeDuration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }



    private IEnumerator WaitUnscaledSeconds(float seconds)
    {
        float time = 0f;
        float safeSeconds = Mathf.Max(0f, seconds);

        while (time < safeSeconds)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetAlpha(float a)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = a;
        }
    }
}
