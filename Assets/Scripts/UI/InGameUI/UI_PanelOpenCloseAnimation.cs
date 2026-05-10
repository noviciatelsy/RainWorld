using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PanelOpenCloseAnimation : MonoBehaviour
{
    [Header("是否在启用时自动播放打开动画")]
    [SerializeField] private bool playOpenOnEnable = true;

    [Header("出现 / 返回的来源位置（可不填）")]
    [SerializeField] private RectTransform appearFrom;

    private Canvas targetCanvas;
    private Camera uiCamera;

    [Header("打开动画：初始缩放倍率")]
    [SerializeField] private float openStartScaleMultiplier = 0.25f;

    [Header("打开动画：超出目标的缩放倍率")]
    [SerializeField] private float openOvershootScaleMultiplier = 1.1f;

    [Header("关闭动画：先额外放大的倍率")]
    [SerializeField] private float closeOvershootScaleMultiplier = 1.05f;

    [Header("关闭动画：最终缩小到的倍率")]
    [SerializeField] private float closeEndScaleMultiplier = 0.25f;

    [Header("打开动画第一段：小 -> 超出")]
    [SerializeField] private float openScaleUpDuration = 0.18f;

    [Header("打开动画第二段：超出 -> 正常")]
    [SerializeField] private float openSettleDuration = 0.10f;

    [Header("关闭动画第一段：当前 -> 略微放大")]
    [SerializeField] private float closeScaleUpDuration = 0.08f;

    [Header("关闭动画第二段：略微放大 -> 缩小")]
    [SerializeField] private float closeShrinkDuration = 0.14f;

    [Header("打开位移动画时长")]
    [SerializeField] private float openMoveDuration = 0.18f;

    [Header("关闭位移动画时长")]
    [SerializeField] private float closeMoveDuration = 0.16f;

    [Header("是否使用非缩放时间（UI 通常建议开）")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("打开第一段缩放曲线（建议先快后慢）")]
    [SerializeField]
    private AnimationCurve openScaleUpCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("打开第二段回落曲线（建议先快后慢）")]
    [SerializeField]
    private AnimationCurve openSettleCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("关闭第一段放大曲线（建议先快后慢）")]
    [SerializeField]
    private AnimationCurve closeScaleUpCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("关闭第二段缩小曲线（建议先快后慢）")]
    [SerializeField]
    private AnimationCurve closeShrinkCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("打开位移动画曲线（建议先快后慢）")]
    [SerializeField]
    private AnimationCurve openMoveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("关闭位移动画曲线（建议先快后慢）")]
    [SerializeField]
    private AnimationCurve closeMoveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rect;

    // 面板在编辑器中的原始位置与原始缩放
    private Vector2 originalAnchoredPosition;
    private Vector3 originalScale;

    // 当前正在运行的动画协程
    private Coroutine animationCoroutine;

    // 是否已经缓存过初始状态
    private bool hasCachedOriginalState;

    // 当前是否正在播放动画
    private bool isAnimating;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 修复点：不要无条件再次缓存，否则会把打开动画刚设置的小缩放当成“原始缩放”
        if (!hasCachedOriginalState)
        {
            CacheOriginalState();
        }
    }

    private void OnEnable()
    {
        if (playOpenOnEnable)
        {
            PlayOpen();
        }
    }

    /// <summary>
    /// 缓存面板在编辑器中的原始位置与缩放
    /// </summary>
    private void CacheOriginalState()
    {
        Canvas.ForceUpdateCanvases();

        originalAnchoredPosition = rect.anchoredPosition;
        originalScale = rect.localScale;

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        hasCachedOriginalState = true;
    }


    /// <summary>
    /// 播放打开动画
    /// </summary>
    public void PlayOpen()
    {
        if (!hasCachedOriginalState)
        {
            CacheOriginalState();
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        gameObject.SetActive(true);
        animationCoroutine = StartCoroutine(PlayOpenCoroutine());
    }

    /// <summary>
    /// 播放关闭动画
    /// 播放完成后会自动禁用该物体
    /// </summary>
    public void PlayClose()
    {
        if (!hasCachedOriginalState)
        {
            CacheOriginalState();
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        animationCoroutine = StartCoroutine(PlayCloseCoroutine());
    }

    public bool IsAnimating
    {
        get
        {
            return isAnimating;
        }
    }

    private IEnumerator PlayOpenCoroutine()
    {
        isAnimating = true;

        Vector3 startScale = originalScale * openStartScaleMultiplier;
        Vector3 overshootScale = originalScale * openOvershootScaleMultiplier;

        bool needMove = appearFrom != null;

        Vector2 startPosition = originalAnchoredPosition;

        if (needMove)
        {
            startPosition = GetAppearFromAnchoredPosition();
        }

        rect.localScale = startScale;

        if (needMove)
        {
            rect.anchoredPosition = startPosition;
        }
        else
        {
            rect.anchoredPosition = originalAnchoredPosition;
        }

        float elapsedTime = 0f;

        while (elapsedTime < openScaleUpDuration)
        {
            elapsedTime += GetDeltaTime();

            float scaleT = Mathf.Clamp01(elapsedTime / openScaleUpDuration);
            float scaleCurveValue = openScaleUpCurve.Evaluate(scaleT);

            rect.localScale = Vector3.Lerp(startScale, overshootScale, scaleCurveValue);

            if (needMove)
            {
                float moveT = Mathf.Clamp01(elapsedTime / openMoveDuration);
                float moveCurveValue = openMoveCurve.Evaluate(moveT);

                rect.anchoredPosition = Vector2.Lerp
                (
                    startPosition,
                    originalAnchoredPosition,
                    moveCurveValue
                );
            }

            yield return null;
        }

        rect.localScale = overshootScale;

        if (needMove)
        {
            rect.anchoredPosition = originalAnchoredPosition;
        }

        elapsedTime = 0f;

        while (elapsedTime < openSettleDuration)
        {
            elapsedTime += GetDeltaTime();

            float t = Mathf.Clamp01(elapsedTime / openSettleDuration);
            float curveValue = openSettleCurve.Evaluate(t);

            rect.localScale = Vector3.Lerp(overshootScale, originalScale, curveValue);

            yield return null;
        }

        rect.localScale = originalScale;
        rect.anchoredPosition = originalAnchoredPosition;

        animationCoroutine = null;
        isAnimating = false;
    }

    private IEnumerator PlayCloseCoroutine()
    {
        isAnimating = true;

        Vector3 closeOvershootScale = originalScale * closeOvershootScaleMultiplier;
        Vector3 endScale = originalScale * closeEndScaleMultiplier;

        bool needMove = appearFrom != null;

        Vector2 endPosition = originalAnchoredPosition;

        if (needMove)
        {
            endPosition = GetAppearFromAnchoredPosition();
        }

        Vector3 currentStartScale = rect.localScale;
        Vector2 currentStartPosition = rect.anchoredPosition;

        float elapsedTime = 0f;

        while (elapsedTime < closeScaleUpDuration)
        {
            elapsedTime += GetDeltaTime();

            float t = Mathf.Clamp01(elapsedTime / closeScaleUpDuration);
            float curveValue = closeScaleUpCurve.Evaluate(t);

            rect.localScale = Vector3.Lerp(currentStartScale, closeOvershootScale, curveValue);

            yield return null;
        }

        rect.localScale = closeOvershootScale;

        elapsedTime = 0f;

        while (elapsedTime < closeShrinkDuration)
        {
            elapsedTime += GetDeltaTime();

            float scaleT = Mathf.Clamp01(elapsedTime / closeShrinkDuration);
            float scaleCurveValue = closeShrinkCurve.Evaluate(scaleT);

            rect.localScale = Vector3.Lerp(closeOvershootScale, endScale, scaleCurveValue);

            if (needMove)
            {
                float moveT = Mathf.Clamp01(elapsedTime / closeMoveDuration);
                float moveCurveValue = closeMoveCurve.Evaluate(moveT);

                rect.anchoredPosition = Vector2.Lerp
                (
                    currentStartPosition,
                    endPosition,
                    moveCurveValue
                );
            }

            yield return null;
        }

        rect.localScale = endScale;

        if (needMove)
        {
            rect.anchoredPosition = endPosition;
        }

        rect.localScale = originalScale;
        rect.anchoredPosition = originalAnchoredPosition;

        animationCoroutine = null;
        isAnimating = false;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 把 appearFrom 的位置转换成“当前面板父级坐标系”下可用的 anchoredPosition
    /// </summary>
    private Vector2 GetAppearFromAnchoredPosition()
    {
        if (appearFrom == null)
        {
            return originalAnchoredPosition;
        }

        RectTransform parentRect = rect.parent as RectTransform;

        if (parentRect == null)
        {
            return originalAnchoredPosition;
        }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint
        (
            null,
            appearFrom.position
        );

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            parentRect,
            screenPoint,
            null,
            out localPoint
        );

        return localPoint;
    }


    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
