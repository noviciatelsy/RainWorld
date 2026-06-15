using System.Collections;
using UnityEngine;

/// <summary>
/// 鼹鼠爷爷睡觉动画：呼吸式 scale 循环 + 定时在 zzzPoint 发射 Z 气泡。
/// </summary>
[DisallowMultipleComponent]
public class MoleParentAni : MonoBehaviour
{
    [Header("Sleep Breath")]
    [Tooltip("做呼吸挤压的视觉 Transform（通常为 texture）")]
    public Transform visualTransform;

    [Tooltip("完整呼吸周期（秒）：(1,1,1) → squish → (1,1,1)")]
    public float breathCycleDuration = 1.1f;

    [Tooltip("挤压目标相对基准 scale 的倍率")]
    public Vector3 breathSquishMultiplier = new Vector3(1.05f, 0.8f, 1f);

    [Header("Z Bubble")]
    [Tooltip("Z 字发射点")]
    public Transform zzzPoint;

    [Tooltip("带 MoleParentZBubble 的 Z 字预制体")]
    public GameObject zTexturePrefab;

    [Tooltip("发射间隔（秒）")]
    public float zSpawnInterval = 0.5f;

    private Vector3 baseScale;
    private Coroutine breathRoutine;
    private float spawnTimer;

    private void Awake()
    {
        ResolveReferences();
        baseScale = visualTransform.localScale;
    }

    private void OnEnable()
    {
        ResolveReferences();
        baseScale = visualTransform.localScale;
        visualTransform.localScale = baseScale;
        breathRoutine = StartCoroutine(BreathScaleLoop());
        spawnTimer = 0f;
    }

    private void OnDisable()
    {
        if (breathRoutine != null)
        {
            StopCoroutine(breathRoutine);
            breathRoutine = null;
        }

        if (visualTransform != null)
        {
            visualTransform.localScale = baseScale;
        }
    }

    private void Update()
    {
        if (zTexturePrefab == null || zzzPoint == null)
        {
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer < zSpawnInterval)
        {
            return;
        }

        spawnTimer -= zSpawnInterval;
        SpawnZBubble();
    }

    private void ResolveReferences()
    {
        if (visualTransform == null)
        {
            Transform texture = transform.Find("texture");
            visualTransform = texture != null ? texture : transform;
        }

        if (zzzPoint == null)
        {
            Transform point = transform.Find("zzzPoint");
            if (point != null)
            {
                zzzPoint = point;
            }
        }
    }

    private IEnumerator BreathScaleLoop()
    {
        Vector3 squishScale = new Vector3(
            baseScale.x * breathSquishMultiplier.x,
            baseScale.y * breathSquishMultiplier.y,
            baseScale.z * breathSquishMultiplier.z
        );

        float halfDuration = Mathf.Max(0.01f, breathCycleDuration * 0.5f);

        while (enabled)
        {
            yield return TweenScale(baseScale, squishScale, halfDuration);
            yield return TweenScale(squishScale, baseScale, halfDuration);
        }
    }

    private IEnumerator TweenScale(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            visualTransform.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        visualTransform.localScale = to;
    }

    private void SpawnZBubble()
    {
        GameObject instance = Instantiate(zTexturePrefab, zzzPoint.position, Quaternion.identity);
        MoleParentZBubble bubble = instance.GetComponent<MoleParentZBubble>();
        if (bubble != null)
        {
            bubble.Play(zzzPoint.position);
        }
    }
}
