using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ButtonSelection_Scale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("真正执行缩放的目标，不填则默认缩放自己")]
    private RectTransform scaleTarget;

    [Header("悬浮缩放倍率")]
    [SerializeField] private float selectedScaleMultiPlier = 1.1f;

    [Header("缩放平滑时间")]
    [SerializeField] private float smoothTime = 0.05f;

    [Header("按下时额外脉冲倍率")]
    [SerializeField] private float pressPunchMultiplier = 1.075f;

    [Header("按下脉冲总时长（秒）")]
    [SerializeField] private float pressPunchDuration = 0.1f;

    private Vector3 originalScale;
    private Vector3 currentVelocity;

    // 当前是否处于悬浮状态
    private bool isHovered;

    // 点击脉冲的临时倍率
    private float currentPressMultiplier = 1f;

    // 当前运行中的点击脉冲协程
    private Coroutine pressPunchCoroutine;

    private void Awake()
    {
        if (scaleTarget == null)
        {
            scaleTarget = GetComponent<RectTransform>();
        }

        originalScale = scaleTarget.localScale;
    }

    private void Update()
    {
        Vector3 baseTargetScale = isHovered
            ? originalScale * selectedScaleMultiPlier
            : originalScale;

        Vector3 finalTargetScale = baseTargetScale * currentPressMultiplier;

        scaleTarget.localScale = Vector3.SmoothDamp
        (
            scaleTarget.localScale,
            finalTargetScale,
            ref currentVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

        if (pressPunchCoroutine != null)
        {
            StopCoroutine(pressPunchCoroutine);
        }

        pressPunchCoroutine = StartCoroutine(PlayPressPunch());
    }

    private IEnumerator PlayPressPunch()
    {
        float halfDuration = pressPunchDuration * 0.5f;

        // ========= 第一段：快速变大 =========
        float elapsedTime = 0f;
        float startMultiplier = currentPressMultiplier;
        float targetMultiplier = pressPunchMultiplier;

        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsedTime / halfDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            currentPressMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, smoothT);

            yield return null;
        }

        currentPressMultiplier = targetMultiplier;

        // ========= 第二段：缩回原倍率 =========
        elapsedTime = 0f;
        startMultiplier = currentPressMultiplier;
        targetMultiplier = 1f;

        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsedTime / halfDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            currentPressMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, smoothT);

            yield return null;
        }

        currentPressMultiplier = 1f;
        pressPunchCoroutine = null;
    }
}
