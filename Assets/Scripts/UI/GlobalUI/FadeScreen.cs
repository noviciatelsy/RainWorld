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

    [Header("RoomSwitch Fade Settings")]
    [SerializeField] private float roomSwitchHoldTime = 0.15f;     // 黑屏停留时间
    [SerializeField] private float roomSwitchFadeOutDuration = 0.2f; // 变黑时间
    [SerializeField] private float roomSwitchFadeInDuration = 0.15f;  // 变亮时间

    private Coroutine fadeCoroutine;

    public bool IsFading => fadeCoroutine != null;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // 默认不挡视野
        SetAlpha(0f);
        SetCanvasGroupBlocking(false);
    }

    public void PlaySceneSwitchFade(System.Action onBlackReached)
    {
        PlaySceneSwitchFade(onBlackReached, null);
    }

    public void PlaySceneSwitchFade(System.Action onBlackReached, System.Action onFadeCompleted)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(SceneSwitchFadeCo(onBlackReached, onFadeCompleted));
    }

    public void PlayRoomSwitchFade(System.Action onBlackReached)
    {
        PlayRoomSwitchFade(onBlackReached, null);
    }

    public void PlayRoomSwitchFade(System.Action onBlackReached, System.Action onFadeCompleted)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(RoomSwitchFadeCo(onBlackReached, onFadeCompleted));
    }

    private IEnumerator SceneSwitchFadeCo(System.Action onBlackReached, System.Action onFadeCompleted)
    {
        SetCanvasGroupBlocking(true);

        yield return FadeRoutine(1f, sceneSwitchFadeOutDuration);
        onBlackReached?.Invoke();

        yield return WaitUnscaledSeconds(sceneSwitchHoldTime);

        yield return FadeRoutine(0f, sceneSwitchFadeInDuration);

        SetCanvasGroupBlocking(false);

        fadeCoroutine = null;
        onFadeCompleted?.Invoke();
    }

    private IEnumerator RoomSwitchFadeCo(System.Action onBlackReached, System.Action onFadeCompleted)
    {
        SetCanvasGroupBlocking(true);

        yield return FadeRoutine(1f, roomSwitchFadeOutDuration);
        onBlackReached?.Invoke();

        yield return WaitUnscaledSeconds(roomSwitchHoldTime);

        yield return FadeRoutine(0f, roomSwitchFadeInDuration);

        SetCanvasGroupBlocking(false);

        fadeCoroutine = null;
        onFadeCompleted?.Invoke();
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;

        // 已经是目标值就不折腾了（防御性写法）
        if (Mathf.Approximately(startAlpha, targetAlpha))
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float time = 0f;
        float safeDuration = Mathf.Max(0.0001f, duration);

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

    private void SetCanvasGroupBlocking(bool isBlocking)
    {
        if (canvasGroup == null)
        {
            return;
        }

        // 黑屏期间可以挡住 UI 点击，避免玩家在切换中误点菜单。
        canvasGroup.blocksRaycasts = isBlocking;
        canvasGroup.interactable = isBlocking;
    }
}