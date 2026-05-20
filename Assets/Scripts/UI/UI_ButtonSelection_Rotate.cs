using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;


public class UI_ButtonSelection_Rotate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("真正执行旋转的目标")]
    private RectTransform rotateTarget;

    [Header("悬浮触发的随机旋转角度范围（度）")]
    [SerializeField] private Vector2 randomRotateAngleRange = new Vector2(5f, 10f);

    [Header("旋转出去的时长（秒）")]
    [SerializeField] private float rotateOutDuration = 0.06f;

    [Header("回正的总时长（秒）")]
    [SerializeField] private float rotateBackDuration = 0.12f;

    [Header("回正时的反向回弹比例，0则没有回弹")]
    [SerializeField] private float backOvershootRatio = 0.18f;

    private Quaternion originalRotation;

    // 当前相对于原始角度的偏移角度
    private float currentOffsetAngle;

    // 当前运行中的旋转协程
    private Coroutine rotateCoroutine;

    private void Awake()
    {
        if (rotateTarget == null)
        {
            rotateTarget = GetComponent<RectTransform>();
        }

        originalRotation = rotateTarget.localRotation;
        currentOffsetAngle = 0f;
    }

    private void OnEnable()
    {
        currentOffsetAngle = 0f;

        if (rotateTarget != null)
        {
            rotateTarget.localRotation = originalRotation;
        }
    }

    private void OnDisable()
    {
        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        currentOffsetAngle = 0f;

        if (rotateTarget != null)
        {
            rotateTarget.localRotation = originalRotation;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }

        rotateCoroutine = StartCoroutine(PlayHoverRotate());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }

    private IEnumerator PlayHoverRotate()
    {
        float minAngle = Mathf.Min(randomRotateAngleRange.x, randomRotateAngleRange.y);
        float maxAngle = Mathf.Max(randomRotateAngleRange.x, randomRotateAngleRange.y);

        float randomAngle = Random.Range(minAngle, maxAngle);

        // 随机决定向左还是向右转
        if (Random.value < 0.5f)
        {
            randomAngle = -randomAngle;
        }

        float startAngle = currentOffsetAngle;
        float targetAngle = randomAngle;

        // ========= 第一段：快速旋出去 =========
        yield return RotateToAngle
        (
            startAngle,
            targetAngle,
            rotateOutDuration
        );

        // ========= 第二段：轻微反向回弹 =========
        if (backOvershootRatio > 0f)
        {
            float overshootAngle = -targetAngle * backOvershootRatio;

            yield return RotateToAngle
            (
                targetAngle,
                overshootAngle,
                rotateBackDuration * 0.45f
            );

            yield return RotateToAngle
            (
                overshootAngle,
                0f,
                rotateBackDuration * 0.55f
            );
        }
        else
        {
            yield return RotateToAngle
            (
                targetAngle,
                0f,
                rotateBackDuration
            );
        }

        currentOffsetAngle = 0f;
        ApplyRotation(currentOffsetAngle);

        rotateCoroutine = null;
    }

    private IEnumerator RotateToAngle(float startAngle, float targetAngle, float duration)
    {
        if (duration <= 0f)
        {
            currentOffsetAngle = targetAngle;
            ApplyRotation(currentOffsetAngle);

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            currentOffsetAngle = Mathf.Lerp(startAngle, targetAngle, smoothT);
            ApplyRotation(currentOffsetAngle);

            yield return null;
        }

        currentOffsetAngle = targetAngle;
        ApplyRotation(currentOffsetAngle);
    }

    private void ApplyRotation(float zAngle)
    {
        rotateTarget.localRotation = originalRotation * Quaternion.Euler(0f, 0f, zAngle);
    }

}