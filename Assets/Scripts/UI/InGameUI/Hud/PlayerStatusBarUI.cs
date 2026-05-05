using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusBarUI : MonoBehaviour
{
    [Header("Status Bar References")]
    [SerializeField] private Slider currentHealthSlider;
    [SerializeField] private Slider hungerBarSlider;
    [SerializeField] private RectTransform maxHealthHealthRect;
    [SerializeField] private RectTransform hungerBarBackgroundRect;
    [SerializeField] private Image hungerIcon;

    [Header("Juicy Animation Settings")]
    [SerializeField] private float barAnimationDuration = 0.25f;
    [SerializeField] private float barOvershootStrength = 1.4f;

    private PlayerVitals playerVitals;
    private int maxHealthAmount;

    private Coroutine currentHealthSliderCoroutine;
    private Coroutine hungerBarSliderCoroutine;
    private Coroutine maxHealthRectCoroutine;
    private Coroutine hungerBarBackgroundCoroutine;

    private void OnEnable()
    {
        TrySubscribe(PlayerManager.Instance.TryGetCurrentPlayer());
        PlayerManager.Instance.OnPlayerRegistered += TrySubscribe;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerRegistered -= TrySubscribe;

        if (playerVitals != null)
        {
            playerVitals.CurrentHealthChanged -= UpdateCurrentHealth;
            playerVitals.HungerChanged -= UpdateHungerBar;
            playerVitals.MaxHealthChanged -= UpdateMaxHealth;
        }

        StopAllStatusBarAnimations();
    }

    private void TrySubscribe(Player player)
    {
        if (player == null)
        {
            return;
        }

        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();

        if (playerVitals != null)
        {
            if (this.playerVitals != null)
            {
                this.playerVitals.CurrentHealthChanged -= UpdateCurrentHealth;
                this.playerVitals.HungerChanged -= UpdateHungerBar;
                this.playerVitals.MaxHealthChanged -= UpdateMaxHealth;
            }

            this.playerVitals = playerVitals;
            maxHealthAmount = playerVitals.BaseMaxHealth;

            playerVitals.CurrentHealthChanged += UpdateCurrentHealth;
            playerVitals.HungerChanged += UpdateHungerBar;
            playerVitals.MaxHealthChanged += UpdateMaxHealth;
        }
    }

    private void UpdateCurrentHealth(int currentHealth)
    {
        float targetValue = (float)currentHealth / maxHealthAmount;
        PlaySliderJuicyAnimation(currentHealthSlider, targetValue, ref currentHealthSliderCoroutine);
    }

    private void UpdateMaxHealth(int newMaxHealth)
    {
        float targetScaleX = (float)newMaxHealth / maxHealthAmount;
        PlayRectScaleXJuicyAnimation(maxHealthHealthRect, targetScaleX, ref maxHealthRectCoroutine);
    }

    private void UpdateHungerBar(int newHungerAmount)
    {
        if (newHungerAmount == 0)
        {
            hungerIcon.enabled = false;
        }
        else
        {
            hungerIcon.enabled = true;
        }

        float targetValue = (float)newHungerAmount / maxHealthAmount;

        PlaySliderJuicyAnimation(hungerBarSlider, targetValue, ref hungerBarSliderCoroutine);
        PlayRectScaleXJuicyAnimation(hungerBarBackgroundRect, targetValue, ref hungerBarBackgroundCoroutine);
    }

    private void PlaySliderJuicyAnimation(Slider slider, float targetValue, ref Coroutine coroutine)
    {
        if (slider == null)
        {
            return;
        }

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        coroutine = StartCoroutine(AnimateSliderValue(slider, targetValue));
    }

    private void PlayRectScaleXJuicyAnimation(RectTransform rectTransform, float targetScaleX, ref Coroutine coroutine)
    {
        if (rectTransform == null)
        {
            return;
        }

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        coroutine = StartCoroutine(AnimateRectScaleX(rectTransform, targetScaleX));
    }

    private IEnumerator AnimateSliderValue(Slider slider, float targetValue)
    {
        float startValue = slider.value;
        float timer = 0f;

        targetValue = Mathf.Clamp01(targetValue);

        while (timer < barAnimationDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / barAnimationDuration);
            float juicyProgress = EaseOutBack(progress);

            float currentValue = Mathf.LerpUnclamped(startValue, targetValue, juicyProgress);

            slider.value = Mathf.Clamp01(currentValue);

            yield return null;
        }

        slider.value = targetValue;
    }

    private IEnumerator AnimateRectScaleX(RectTransform rectTransform, float targetScaleX)
    {
        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = new Vector3(targetScaleX, startScale.y, startScale.z);

        float timer = 0f;

        while (timer < barAnimationDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / barAnimationDuration);
            float juicyProgress = EaseOutBack(progress);

            rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, juicyProgress);

            yield return null;
        }

        rectTransform.localScale = targetScale;
    }

    private float EaseOutBack(float t)
    {
        float c1 = barOvershootStrength;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void StopAllStatusBarAnimations()
    {
        if (currentHealthSliderCoroutine != null)
        {
            StopCoroutine(currentHealthSliderCoroutine);
            currentHealthSliderCoroutine = null;
        }

        if (hungerBarSliderCoroutine != null)
        {
            StopCoroutine(hungerBarSliderCoroutine);
            hungerBarSliderCoroutine = null;
        }

        if (maxHealthRectCoroutine != null)
        {
            StopCoroutine(maxHealthRectCoroutine);
            maxHealthRectCoroutine = null;
        }

        if (hungerBarBackgroundCoroutine != null)
        {
            StopCoroutine(hungerBarBackgroundCoroutine);
            hungerBarBackgroundCoroutine = null;
        }
    }
}