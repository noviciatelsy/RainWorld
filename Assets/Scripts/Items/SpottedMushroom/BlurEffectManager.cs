using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BlurEffectManager : MonoBehaviour
{
    public static BlurEffectManager Instance;

    [Header("模糊后处理 Volume")]
    private Volume blurVolume;

    [Header("默认淡入淡出速度")]
    [SerializeField] private float defaultFadeTime = 0.5f;

    [Header("强度曲线：让模糊变化更自然")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine fadeCoroutine;
    private Coroutine temporaryBlurCoroutine;
    private float currentPoisonValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        blurVolume = GetComponent<Volume>();
        SetBlurValue(0f);
    }

    /// <summary>
    /// 直接设置屏幕效果强度。
    /// 0 = 完全没有效果，1 = 效果拉满。
    /// </summary>
    public void SetBlurValue(float value)
    {
        currentPoisonValue = Mathf.Clamp01(value);

        if (blurVolume != null)
        {
            // 使用曲线可以让 0~1 的变化更柔和
            blurVolume.weight = intensityCurve.Evaluate(currentPoisonValue);

            // 强度为 0 时直接关闭 Volume，稍微省一点点性能
            blurVolume.enabled = blurVolume.weight > 0.001f;
        }
    }

    /// <summary>
    /// 让效果平滑变化到目标强度。
    /// </summary>
    public void FadeToBlurValue(float targetValue, float fadeTime)
    {
        StopTemporaryBlurCoroutine();

        targetValue = Mathf.Clamp01(targetValue);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(targetValue, fadeTime));
    }

    /// <summary>
    /// 暂时开启模糊效果。
    /// duration = 模糊保持的时间，单位是秒。
    /// </summary>
    public void StartTemporaryBlur(float duration)
    {
        duration = Mathf.Max(0f, duration);

        if (temporaryBlurCoroutine != null)
        {
            StopCoroutine(temporaryBlurCoroutine);
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        temporaryBlurCoroutine = StartCoroutine(TemporaryBlurRoutine(duration));
    }

    public void StartBlur(float targetValue = 1f)
    {
        FadeToBlurValue(targetValue, defaultFadeTime);
    }

    public void StopBlur()
    {
        FadeToBlurValue(0f, defaultFadeTime);
    }

    private IEnumerator TemporaryBlurRoutine(float duration)
    {
        // 先淡入到完整模糊
        yield return FadeRoutine(1f, defaultFadeTime);

        // 保持模糊一段时间
        yield return new WaitForSeconds(duration);

        // 再淡出到无模糊
        yield return FadeRoutine(0f, defaultFadeTime);

        temporaryBlurCoroutine = null;
    }

    private IEnumerator FadeRoutine(float targetValue, float fadeTime)
    {
        float startValue = currentPoisonValue;
        float timer = 0f;

        if (fadeTime <= 0f)
        {
            SetBlurValue(targetValue);
            fadeCoroutine = null;
            yield break;
        }

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeTime);
            float value = Mathf.Lerp(startValue, targetValue, t);

            SetBlurValue(value);

            yield return null;
        }

        SetBlurValue(targetValue);
        fadeCoroutine = null;
    }

    private void StopTemporaryBlurCoroutine()
    {
        if (temporaryBlurCoroutine != null)
        {
            StopCoroutine(temporaryBlurCoroutine);
            temporaryBlurCoroutine = null;
        }
    }
}