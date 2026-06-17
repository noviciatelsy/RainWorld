using System.Collections;
using UnityEngine;

/// <summary>
/// 波浪光源：径向渐变打底 + 周期扩散发波（先快后慢）。
/// </summary>
public class WaveLightEffect : MonoBehaviour
{
    private static readonly int CenterAlphaId = Shader.PropertyToID("_CenterAlpha");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static Sprite sharedFilledCircleSprite;

    [Header("References")]
    [SerializeField] private SpriteRenderer baseGlowRenderer;
    [SerializeField] private SpriteRenderer wavePulseRenderer;
    [SerializeField] private Shader radialShader;

    [Header("Base Glow")]
    [SerializeField] private float radius = 3f;
    [SerializeField] [Range(0, 255)] private int centerAlpha = 40;
    [SerializeField] private Color baseColor = Color.white;

    [Header("Wave Pulse")]
    [SerializeField] private float wavePeriod = 1f;
    [SerializeField] [Range(0, 255)] private int waveStartAlpha = 40;
    [SerializeField] private float waveExpandDuration = 1.5f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "VFX";
    [SerializeField] private int baseSortingOrder = 0;
    [SerializeField] private int waveSortingOrder = 1;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [Tooltip("自动播放时的持续时间；0 表示一直播放")]
    [SerializeField] private float playDuration = 0f;

    public float Radius
    {
        get => radius;
        set
        {
            radius = value;
            ApplyVisualSettings();
        }
    }

    private Material baseMaterial;
    private Material waveMaterial;
    private Coroutine lifetimeRoutine;
    private float targetScale = 1f;
    private float waveTimer;
    private bool isPlaying;

    private bool visualsInitialized;

    private void Awake()
    {
        InitializeVisuals();
        ApplyVisualSettings();
    }

    private void Start()
    {
        if (playOnStart && !isPlaying)
        {
            BeginEffect(playDuration);
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        UpdateWavePulse(Time.deltaTime);
    }

    private void OnDestroy()
    {
        DestroyMaterial(baseMaterial);
        DestroyMaterial(waveMaterial);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        InitializeVisuals();
        ApplyVisualSettings();

        if (isPlaying)
        {
            waveTimer = 0f;
            ApplyWaveVisual(0f, 0f);
        }
    }

    /// <summary>
    /// 使用 Inspector 当前参数播放；duration &lt;= 0 则持续播放。
    /// </summary>
    public void Play(float duration = 0f)
    {
        BeginEffect(duration);
    }

    /// <summary>
    /// 覆盖参数后播放；duration &lt;= 0 则持续播放。
    /// </summary>
    public void PlayAttached(
        float effectRadius,
        float duration,
        int effectCenterAlpha = 40,
        int effectWaveStartAlpha = 40,
        Color? effectColor = null,
        float effectWavePeriod = 1f,
        float effectWaveExpandDuration = 1.5f)
    {
        radius = effectRadius;
        centerAlpha = effectCenterAlpha;
        waveStartAlpha = effectWaveStartAlpha;
        baseColor = effectColor ?? Color.white;
        wavePeriod = effectWavePeriod;
        waveExpandDuration = effectWaveExpandDuration;

        playOnStart = false;
        BeginEffect(duration);
    }

    public void StopEffect()
    {
        isPlaying = false;

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        if (wavePulseRenderer != null)
        {
            wavePulseRenderer.enabled = false;
        }
    }

    private void BeginEffect(float duration)
    {
        isPlaying = true;

        InitializeVisuals();
        ApplyVisualSettings();
        waveTimer = 0f;
        ApplyWaveVisual(0f, 0f);
        RestartLifetimeRoutine(duration);
    }

    private void ApplyVisualSettings()
    {
        if (baseGlowRenderer == null || wavePulseRenderer == null)
        {
            return;
        }

        EnsureSpriteAssigned();

        targetScale = CalculateScaleForRadius(radius);
        transform.localScale = Vector3.one;

        baseGlowRenderer.enabled = true;
        baseGlowRenderer.transform.localScale = Vector3.one * targetScale;

        ApplyBaseVisual();
    }

    private void InitializeVisuals()
    {
        if (visualsInitialized)
        {
            return;
        }

        EnsureRenderers();
        EnsureMaterials();
        visualsInitialized = true;
    }

    private void EnsureRenderers()
    {
        if (baseGlowRenderer == null)
        {
            baseGlowRenderer = GetRendererFromChild("BaseGlow", baseSortingOrder);
        }

        if (wavePulseRenderer == null)
        {
            wavePulseRenderer = GetRendererFromChild("WavePulse", waveSortingOrder);
        }
    }

    private SpriteRenderer GetRendererFromChild(string childName, int sortingOrder)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            SpriteRenderer existing = child.GetComponent<SpriteRenderer>();
            if (existing != null)
            {
                ApplySorting(existing, sortingOrder);
                return existing;
            }
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(transform, false);
        SpriteRenderer renderer = childObject.AddComponent<SpriteRenderer>();
        ApplySorting(renderer, sortingOrder);
        return renderer;
    }

    private void ApplySorting(SpriteRenderer renderer, int sortingOrder)
    {
        if (renderer == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            renderer.sortingLayerName = sortingLayerName;
        }

        renderer.sortingOrder = sortingOrder;
    }

    private void EnsureMaterials()
    {
        if (radialShader == null)
        {
            radialShader = Shader.Find("Sprites/WaveLightRadial");
        }

        if (radialShader == null)
        {
            Debug.LogWarning($"{nameof(WaveLightEffect)}: 未找到 Sprites/WaveLightRadial Shader。", this);
            return;
        }

        EnsureSpriteAssigned();

        if (baseGlowRenderer != null && baseMaterial == null)
        {
            baseMaterial = new Material(radialShader);
            baseGlowRenderer.sharedMaterial = baseMaterial;
        }

        if (wavePulseRenderer != null && waveMaterial == null)
        {
            waveMaterial = new Material(radialShader);
            wavePulseRenderer.sharedMaterial = waveMaterial;
        }
    }

    private void EnsureSpriteAssigned()
    {
        if (baseGlowRenderer == null || wavePulseRenderer == null)
        {
            return;
        }

        Sprite filledCircle = baseGlowRenderer.sprite ?? wavePulseRenderer.sprite ?? GetFilledCircleSprite();

        if (baseGlowRenderer.sprite == null)
        {
            baseGlowRenderer.sprite = filledCircle;
        }

        if (wavePulseRenderer.sprite == null)
        {
            wavePulseRenderer.sprite = filledCircle;
        }
    }

    private void ApplyBaseVisual()
    {
        if (baseGlowRenderer == null)
        {
            return;
        }

        if (baseMaterial == null)
        {
            return;
        }

        Color tint = baseColor;
        tint.a = 1f;
        baseMaterial.SetColor(ColorId, tint);
        baseMaterial.SetFloat(CenterAlphaId, GetCenterAlpha01());

        baseGlowRenderer.color = Color.white;
    }

    private void ApplyWaveVisual(float expand01, float alpha01)
    {
        if (wavePulseRenderer == null)
        {
            return;
        }

        float currentScale = targetScale * Mathf.Max(0f, expand01);
        wavePulseRenderer.transform.localScale = Vector3.one * currentScale;

        if (waveMaterial == null)
        {
            return;
        }

        Color tint = baseColor;
        tint.a = 1f;
        waveMaterial.SetColor(ColorId, tint);
        waveMaterial.SetFloat(CenterAlphaId, GetWaveStartAlpha01() * Mathf.Clamp01(alpha01));

        wavePulseRenderer.enabled = true;
        wavePulseRenderer.color = Color.white;
    }

    private void RestartLifetimeRoutine(float duration)
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        if (duration <= 0f)
        {
            return;
        }

        lifetimeRoutine = StartCoroutine(LifetimeRoutine(duration));
    }

    private IEnumerator LifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        lifetimeRoutine = null;
        Destroy(gameObject);
    }

    private void UpdateWavePulse(float deltaTime)
    {
        if (wavePulseRenderer == null)
        {
            return;
        }

        float safePeriod = Mathf.Max(0.05f, wavePeriod);
        float safeExpandDuration = Mathf.Max(0.05f, waveExpandDuration);

        waveTimer += deltaTime;
        if (waveTimer >= safePeriod)
        {
            waveTimer -= safePeriod;
        }

        float expandProgress = waveTimer / safeExpandDuration;
        if (expandProgress >= 1f)
        {
            wavePulseRenderer.transform.localScale = Vector3.zero;
            return;
        }

        wavePulseRenderer.enabled = true;
        float easedExpand = EaseOutQuad(expandProgress);
        float alphaProgress = 1f - expandProgress;
        ApplyWaveVisual(easedExpand, alphaProgress);
    }

    private float GetCenterAlpha01()
    {
        return centerAlpha / 255f;
    }

    private float GetWaveStartAlpha01()
    {
        return waveStartAlpha / 255f;
    }

    private static float EaseOutQuad(float t)
    {
        float clamped = Mathf.Clamp01(t);
        return 1f - (1f - clamped) * (1f - clamped);
    }

    private float CalculateScaleForRadius(float targetRadius)
    {
        Sprite sprite = baseGlowRenderer != null ? baseGlowRenderer.sprite : null;
        if (sprite == null)
        {
            return Mathf.Max(0.01f, targetRadius * 2f);
        }

        float spriteDiameter = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        if (spriteDiameter <= 0.0001f)
        {
            return Mathf.Max(0.01f, targetRadius * 2f);
        }

        return targetRadius * 2f / spriteDiameter;
    }

    private static Sprite GetFilledCircleSprite()
    {
        if (sharedFilledCircleSprite != null)
        {
            return sharedFilledCircleSprite;
        }

        sharedFilledCircleSprite = CreateFilledCircleSprite();
        return sharedFilledCircleSprite;
    }

    private static void DestroyMaterial(Material material)
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

    private static Sprite CreateFilledCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f - 1f;
        float radiusSq = radius * radius;

        for (int y = 0; y < size; y++)
        {
            float dy = y - center;

            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float alpha = dx * dx + dy * dy <= radiusSq ? 1f : 0f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PrewarmFilledCircleSprite()
    {
        if (sharedFilledCircleSprite == null)
        {
            sharedFilledCircleSprite = CreateFilledCircleSprite();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
