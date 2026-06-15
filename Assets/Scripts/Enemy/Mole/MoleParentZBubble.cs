using System.Collections;
using UnityEngine;

/// <summary>
/// 睡觉 Z 字气泡：沿向上抛物线漂移，lifetime 内渐隐并销毁。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class MoleParentZBubble : MonoBehaviour
{
    [Tooltip("存在时长（秒），同时作为淡出时长")]
    public float lifetime = 1f;

    [Tooltip("向上漂移高度")]
    public float riseHeight = 1.2f;

    [Tooltip("水平漂移距离（向右为正）")]
    public float horizontalDrift = 0.35f;

    [Tooltip("轨迹幂次：2≈x²，3≈x³")]
    [Range(2, 3)]
    public int trajectoryPower = 2;

    private SpriteRenderer spriteRenderer;
    private Color baseColor = Color.white;
    private Coroutine motionRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    private void OnDisable()
    {
        if (motionRoutine != null)
        {
            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }
    }

    public void Play(Vector3 worldStart)
    {
        transform.position = worldStart;
        SetAlpha(1f);

        if (motionRoutine != null)
        {
            StopCoroutine(motionRoutine);
        }

        motionRoutine = StartCoroutine(RunMotion(worldStart));
    }

    private IEnumerator RunMotion(Vector3 worldStart)
    {
        float duration = Mathf.Max(0.01f, lifetime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float horizontal = horizontalDrift * t;
            float vertical = trajectoryPower >= 3
                ? riseHeight * t * t * t
                : riseHeight * (2f * t - t * t);

            transform.position = worldStart + new Vector3(horizontal, vertical, 0f);
            SetAlpha(1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        Color color = baseColor;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }
}
