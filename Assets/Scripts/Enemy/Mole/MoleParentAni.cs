using System.Collections;
using UnityEngine;

/// <summary>
/// 鼹鼠爷爷睡觉动画 + 开心状态序列动画。
/// </summary>
[DisallowMultipleComponent]
public class MoleParentAni : MonoBehaviour
{
    private const string HappySpriteResourcePath = "textures/敌人资源/鼹鼠爷爷/开心";

    [Header("Sleep Breath")]
    [Tooltip("做呼吸挤压的视觉 Transform（通常为 texture 下的脸部子物体）")]
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

    [Header("Happy Sequence")]
    [SerializeField] private Transform textureRoot;
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private Sprite happySprite;
    [SerializeField] private float jumpLocalY = 2.5f;
    [SerializeField] private float jumpDuration = 0.35f;
    [SerializeField] private float squashDuration = 0.3f;
    [SerializeField] private float squashAmount = 0.1f;
    [SerializeField] private float fallDuration = 0.5f;

    public bool IsHappy { get; private set; }
    public bool IsPlayingHappySequence { get; private set; }

    private Vector3 baseScale;
    private Vector3 textureBaseLocalPosition;
    private Coroutine breathRoutine;
    private Coroutine happyRoutine;
    private DestructibleWall happySequenceDestructibleWall;
    private bool happySequencePermanentWallDestroy = true;
    private bool isBreathing;
    private bool isZBubbleSpawning;
    private float spawnTimer;

    private void Awake()
    {
        ResolveReferences();
        baseScale = visualTransform.localScale;
        textureBaseLocalPosition = textureRoot.localPosition;

        if (happySprite == null)
        {
            happySprite = Resources.Load<Sprite>(HappySpriteResourcePath);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        baseScale = visualTransform.localScale;
        textureBaseLocalPosition = textureRoot.localPosition;

        if (!IsHappy)
        {
            StartSleepBehavior();
        }
        else
        {
            StartHappyBreathBehavior();
        }
    }

    private void OnDisable()
    {
        StopBreathBehavior();

        if (happyRoutine != null)
        {
            StopCoroutine(happyRoutine);
            happyRoutine = null;
            IsPlayingHappySequence = false;
        }

        happySequenceDestructibleWall = null;

        if (visualTransform != null)
        {
            visualTransform.localScale = baseScale;
        }

        if (textureRoot != null)
        {
            textureRoot.localPosition = textureBaseLocalPosition;
        }
    }

    private void Update()
    {
        if (!isZBubbleSpawning || zTexturePrefab == null || zzzPoint == null)
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

    public void EnterPermanentHappyState(
        Vector2 landingWorldPosition,
        DestructibleWall destructibleWall = null,
        bool permanentWallDestroy = true)
    {
        if (IsHappy || IsPlayingHappySequence)
        {
            return;
        }

        happySequenceDestructibleWall = destructibleWall;
        happySequencePermanentWallDestroy = permanentWallDestroy;

        StopBreathBehavior();

        if (happyRoutine != null)
        {
            StopCoroutine(happyRoutine);
        }

        happyRoutine = StartCoroutine(HappySequenceRoutine(landingWorldPosition));
    }

    private void StartSleepBehavior()
    {
        isZBubbleSpawning = true;
        spawnTimer = 0f;
        visualTransform.localScale = baseScale;
        textureRoot.localPosition = textureBaseLocalPosition;
        StartBreathLoop();

        EnemyMoleParentAudioEmitter audioEmitter = GetComponent<EnemyMoleParentAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.StartSleepLoop();
        }
    }

    private void StartHappyBreathBehavior()
    {
        isZBubbleSpawning = false;
        visualTransform.localScale = baseScale;
        StartBreathLoop();
    }

    private void StartBreathLoop()
    {
        isBreathing = true;

        if (breathRoutine != null)
        {
            StopCoroutine(breathRoutine);
        }

        breathRoutine = StartCoroutine(BreathScaleLoop());
    }

    private void StopBreathBehavior()
    {
        isBreathing = false;
        isZBubbleSpawning = false;

        if (breathRoutine != null)
        {
            StopCoroutine(breathRoutine);
            breathRoutine = null;
        }

        if (visualTransform != null)
        {
            visualTransform.localScale = baseScale;
        }

        EnemyMoleParentAudioEmitter audioEmitter = GetComponent<EnemyMoleParentAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.StopSleepLoop();
        }
    }

    private void ResolveReferences()
    {
        if (textureRoot == null)
        {
            Transform texture = transform.Find("texture");
            textureRoot = texture != null ? texture : transform;
        }

        if (visualTransform == null)
        {
            if (textureRoot != null && textureRoot.childCount > 0)
            {
                visualTransform = textureRoot.GetChild(0);
            }
            else
            {
                visualTransform = textureRoot != null ? textureRoot : transform;
            }
        }

        if (faceRenderer == null && visualTransform != null)
        {
            faceRenderer = visualTransform.GetComponent<SpriteRenderer>();
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

    private IEnumerator HappySequenceRoutine(Vector2 landingWorldPosition)
    {
        IsPlayingHappySequence = true;

        if (faceRenderer != null && happySprite != null)
        {
            faceRenderer.sprite = happySprite;
        }

        Vector3 textureStart = textureBaseLocalPosition;
        Vector3 texturePeak = textureBaseLocalPosition + new Vector3(0f, jumpLocalY, 0f);
        Vector3 rootStart = transform.position;
        Vector3 rootTarget = new Vector3(landingWorldPosition.x, landingWorldPosition.y, transform.position.z);

        yield return TweenLocalPosition(textureRoot, textureStart, texturePeak, jumpDuration);
        yield return PlaySquashSequence();
        NotifyHappyFallStarted();
        yield return TweenHappyFall(rootStart, rootTarget, texturePeak, textureStart);

        transform.position = rootTarget;
        textureRoot.localPosition = Vector3.zero;

        visualTransform.localScale = baseScale;
        IsHappy = true;
        IsPlayingHappySequence = false;
        happyRoutine = null;

        StartHappyBreathBehavior();
    }

    private void NotifyHappyFallStarted()
    {
        if (happySequenceDestructibleWall == null)
        {
            return;
        }

        happySequenceDestructibleWall.NotifyWallDestroy(happySequencePermanentWallDestroy);
        happySequenceDestructibleWall = null;
    }

    private IEnumerator PlaySquashSequence()
    {
        Vector3 one = Vector3.one;
        Vector3 squashA = new Vector3(1f + squashAmount, 1f - squashAmount, 1f);
        Vector3 squashB = new Vector3(1f - squashAmount, 1f + squashAmount, 1f);
        float stepDuration = squashDuration / 4f;

        yield return TweenVisualScale(one, squashA, stepDuration);
        yield return TweenVisualScale(squashA, one, stepDuration);
        yield return TweenVisualScale(one, squashB, stepDuration);
        yield return TweenVisualScale(squashB, one, stepDuration);
    }

    private IEnumerator TweenHappyFall(
        Vector3 rootStart,
        Vector3 rootTarget,
        Vector3 textureStart,
        Vector3 textureEnd)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fallDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            transform.position = Vector3.LerpUnclamped(rootStart, rootTarget, t);
            textureRoot.localPosition = Vector3.LerpUnclamped(textureStart, textureEnd, t);

            yield return null;
        }

        transform.position = rootTarget;
        textureRoot.localPosition = textureEnd;
    }

    private IEnumerator BreathScaleLoop()
    {
        Vector3 squishScale = new Vector3(
            baseScale.x * breathSquishMultiplier.x,
            baseScale.y * breathSquishMultiplier.y,
            baseScale.z * breathSquishMultiplier.z
        );

        float halfDuration = Mathf.Max(0.01f, breathCycleDuration * 0.5f);

        while (isBreathing)
        {
            yield return TweenScaleAbsolute(baseScale, squishScale, halfDuration);
            yield return TweenScaleAbsolute(squishScale, baseScale, halfDuration);
        }
    }

    private IEnumerator TweenScaleAbsolute(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / safeDuration);
            visualTransform.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        visualTransform.localScale = to;
    }

    private IEnumerator TweenVisualScale(Vector3 fromMultiplier, Vector3 toMultiplier, float duration)
    {
        Vector3 from = new Vector3(
            baseScale.x * fromMultiplier.x,
            baseScale.y * fromMultiplier.y,
            baseScale.z * fromMultiplier.z);

        Vector3 to = new Vector3(
            baseScale.x * toMultiplier.x,
            baseScale.y * toMultiplier.y,
            baseScale.z * toMultiplier.z);

        return TweenScaleAbsolute(from, to, duration);
    }

    private IEnumerator TweenLocalPosition(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        target.localPosition = from;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / safeDuration);
            target.localPosition = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        target.localPosition = to;
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
