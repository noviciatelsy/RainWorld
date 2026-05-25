using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MoneyFlyCoinUI : MonoBehaviour
{
    [Header("Refs")]
    private Image iconImage;
    private CanvasGroup canvasGroup;
    [SerializeField] private MoneyTrailGhostUI trailPrefab;

    [Header("Trail Settings")]
    [SerializeField] private bool spawnTrail = true;
    [SerializeField] private float trailSpawnInterval = 0.03f; // 尾迹生成间隔
    [SerializeField] private float endShrinkMultiplier = 0.82f; // 飞到终点时略缩小一点
    [SerializeField] private float endFadeStartNormalizedTime = 0.82f; // 飞行后段开始淡出

    private RectTransform rectTransform;
    private Action onArriveCallback;

    private float trailTimer;
    private float spinSpeed;

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

    public void Play
    (
        Vector2 startPosition,
        Vector2 scatterPosition,
        Vector2 targetPosition,
        float popDuration,
        float waitBeforeFly,
        float flyDuration,
        float arcHeight,
        float startScale,
        float spinSpeed,
        Action onArriveCallback
    )
    {
        this.spinSpeed = spinSpeed;
        this.onArriveCallback = onArriveCallback;

        StopAllCoroutines();
        StartCoroutine(PlayRoutine
        (
            startPosition,
            scatterPosition,
            targetPosition,
            popDuration,
            waitBeforeFly,
            flyDuration,
            arcHeight,
            startScale
        ));
    }

    private IEnumerator PlayRoutine
    (
        Vector2 startPosition,
        Vector2 scatterPosition,
        Vector2 targetPosition,
        float popDuration,
        float waitBeforeFly,
        float flyDuration,
        float arcHeight,
        float startScale
    )
    {
        if (rectTransform == null)
        {
            yield break;
        }

        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.zero;
        rectTransform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-20f, 20f));

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // =========================
        // 第一段：从鼠标位置“蹦出来”并散开
        // =========================
        float popTime = 0f;

        while (popTime < popDuration)
        {
            popTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(popTime / popDuration);

            float easedT = EaseOutBack(t);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, scatterPosition, easedT);
            rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0f, startScale, easedT);

            yield return null;
        }

        rectTransform.anchoredPosition = scatterPosition;
        rectTransform.localScale = Vector3.one * startScale;

        // =========================
        // 第二段：短暂等待
        // =========================
        if (waitBeforeFly > 0f)
        {
            yield return new WaitForSecondsRealtime(waitBeforeFly);
        }

        // =========================
        // 第三段：沿弧线飞向目标
        // =========================
        Vector2 midPoint = (scatterPosition + targetPosition) * 0.5f;

        // 控制点：在中点上方向上抬，形成向上顶的弧线
        Vector2 controlPoint = midPoint + Vector2.up * arcHeight;

        float flyTime = 0f;
        trailTimer = 0f;

        while (flyTime < flyDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            flyTime += deltaTime;

            float t = Mathf.Clamp01(flyTime / flyDuration);
            float easedT = EaseOutCubic(t);

            Vector2 currentPosition = GetQuadraticBezierPoint(scatterPosition, controlPoint, targetPosition, easedT);
            rectTransform.anchoredPosition = currentPosition;

            // 金币边飞边旋转一点点，会灵动很多
            Vector3 euler = rectTransform.localEulerAngles;
            euler.z += spinSpeed * deltaTime;
            rectTransform.localEulerAngles = euler;

            // 后半段微微收一下尺寸，避免生硬
            float currentScale = Mathf.Lerp(startScale, startScale * endShrinkMultiplier, easedT);
            rectTransform.localScale = Vector3.one * currentScale;

            // 飞行后段开始轻微淡出
            if (canvasGroup != null)
            {
                if (t < endFadeStartNormalizedTime)
                {
                    canvasGroup.alpha = 1f;
                }
                else
                {
                    float fadeT = Mathf.InverseLerp(endFadeStartNormalizedTime, 1f, t);
                    canvasGroup.alpha = Mathf.Lerp(1f, 0.15f, fadeT);
                }
            }

            // 生成短尾迹
            if (spawnTrail && trailPrefab != null)
            {
                trailTimer -= deltaTime;

                if (trailTimer <= 0f)
                {
                    SpawnTrailGhost();
                    trailTimer = trailSpawnInterval;
                }
            }

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;

        onArriveCallback?.Invoke();

        Destroy(gameObject);
    }

    private void SpawnTrailGhost()
    {
        if (trailPrefab == null || rectTransform == null)
        {
            return;
        }

        MoneyTrailGhostUI trail = Instantiate(trailPrefab, rectTransform.parent);

        trail.Setup
        (
            iconImage != null ? iconImage.sprite : null,
            rectTransform.anchoredPosition,
            rectTransform.localEulerAngles,
            rectTransform.localScale
        );
    }

    private Vector2 GetQuadraticBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * p0
             + 2f * oneMinusT * t * p1
             + t * t * p2;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}