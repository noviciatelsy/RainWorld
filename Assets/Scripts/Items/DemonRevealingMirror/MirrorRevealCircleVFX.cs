using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MirrorRevealCircleVFX : MonoBehaviour
{
    [Header("References")]
    private SpriteRenderer circleRenderer;
    // 圆圈 SpriteRenderer


    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.35f;
    // 扩散持续时间

    [SerializeField]
    private AnimationCurve scaleCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // 缩放曲线
    // 横轴：动画进度 0 ~ 1
    // 纵轴：圆圈大小 0 ~ 1

    [SerializeField]
    private AnimationCurve alphaCurve =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    // 透明度曲线
    // 默认从完全显示逐渐淡出

    [SerializeField] private bool destroyAfterPlay = true;
    // 播放结束后是否销毁自身


    private Coroutine playCoroutine;
    // 当前播放协程

    private Color originalColor;
    // 初始颜色

    private Transform followTarget;
    // 是否跟随某个目标
    // 如果传入玩家 Transform，就可以在扩散期间跟着玩家移动


    private void Awake()
    {

        circleRenderer = GetComponent<SpriteRenderer>();
        originalColor = circleRenderer.color;
    }


    /// <summary>
    /// 在指定世界坐标播放扩散圆圈。
    /// 圆圈不会跟随目标移动。
    /// </summary>
    public void PlayAtPosition(
        Vector3 myWorldPosition,
        float myTargetRadius)
    {
        followTarget = null;

        transform.position = myWorldPosition;

        StartPlay(myTargetRadius);
    }


    /// <summary>
    /// 围绕指定目标播放扩散圆圈。
    /// 圆圈会在播放期间跟随目标移动。
    /// </summary>
    public void PlayAroundTarget(
        Transform myTarget,
        float myTargetRadius)
    {
        followTarget = myTarget;

        if (followTarget != null)
        {
            transform.position = followTarget.position;
        }

        StartPlay(myTargetRadius);
    }


    private void StartPlay(float myTargetRadius)
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
        }

        playCoroutine = StartCoroutine
        (
            PlayRoutine(myTargetRadius)
        );
    }


    private IEnumerator PlayRoutine(float myTargetRadius)
    {
        float elapsedTime = 0f;

        float targetScaleValue =
            CalculateTargetScaleValue(myTargetRadius);

        transform.localScale = Vector3.zero;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsedTime / duration);

            if (followTarget != null)
            {
                transform.position = followTarget.position;
            }

            float scaleProgress =
                scaleCurve.Evaluate(progress);

            float alphaProgress =
                alphaCurve.Evaluate(progress);

            float currentScale =
                Mathf.Lerp(0f, targetScaleValue, scaleProgress);

            transform.localScale =
                Vector3.one * currentScale;

            Color currentColor =
                originalColor;

            currentColor.a =
                originalColor.a * alphaProgress;

            circleRenderer.color =
                currentColor;

            yield return null;
        }

        transform.localScale =
            Vector3.one * targetScaleValue;

        Color finalColor =
            originalColor;

        finalColor.a = 0f;

        circleRenderer.color =
            finalColor;

        playCoroutine = null;

        if (destroyAfterPlay)
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// 根据目标半径计算圆圈最终缩放值。
    /// 
    /// 关键点：
    /// 检测范围是半径 myTargetRadius，
    /// 但圆圈 Sprite 的显示大小需要覆盖直径，
    /// 所以目标世界直径 = myTargetRadius * 2。
    /// </summary>
    private float CalculateTargetScaleValue(float myTargetRadius)
    {
        if (circleRenderer == null
            || circleRenderer.sprite == null)
        {
            return myTargetRadius * 2f;
        }

        float spriteWorldDiameterAtScaleOne =
            Mathf.Max
            (
                circleRenderer.sprite.bounds.size.x,
                circleRenderer.sprite.bounds.size.y
            );

        if (spriteWorldDiameterAtScaleOne <= 0.0001f)
        {
            return myTargetRadius * 2f;
        }

        float targetWorldDiameter =
            myTargetRadius * 2f;

        return targetWorldDiameter / spriteWorldDiameterAtScaleOne;
    }
}