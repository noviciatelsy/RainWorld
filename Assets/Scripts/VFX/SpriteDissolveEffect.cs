using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 驱动 Sprites/DissolveBurn 材质的溶解进度（0=全溶解，1=全显示）。
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class SpriteDissolveEffect : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");

    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] [Range(0f, 1f)] private float dissolveAmount = 1f;
    [SerializeField] private float defaultDissolveDuration = 1f;

    private Material runtimeMaterial;
    private Coroutine dissolveRoutine;

    public float DissolveAmount => dissolveAmount;

    private void OnEnable()
    {
        CacheTargetRenderer();
        EnsureRuntimeMaterial();
        ApplyVisualState();
    }

    private void LateUpdate()
    {
        ApplySpriteTexture();
    }

    private void OnValidate()
    {
        dissolveAmount = Mathf.Clamp01(dissolveAmount);
        CacheTargetRenderer();
        EnsureRuntimeMaterial();
        ApplyVisualState();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    public void SetDissolveAmount(float amount)
    {
        dissolveAmount = Mathf.Clamp01(amount);
        ApplyVisualState();
    }

    public void PlayDissolveOut(float duration = -1f, Action onComplete = null)
    {
        float playDuration = duration > 0f ? duration : defaultDissolveDuration;
        StopDissolveRoutine();
        dissolveRoutine = StartCoroutine(DissolveRoutine(1f, 0f, playDuration, onComplete));
    }

    public void PlayDissolveIn(float duration = -1f, Action onComplete = null)
    {
        float playDuration = duration > 0f ? duration : defaultDissolveDuration;
        StopDissolveRoutine();
        dissolveRoutine = StartCoroutine(DissolveRoutine(0f, 1f, playDuration, onComplete));
    }

    public void SetEdgeColor(Color edgeColor)
    {
        EnsureRuntimeMaterial();
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetColor(EdgeColorId, edgeColor);
        }
    }

    public void SetEdgeWidth(float edgeWidth)
    {
        EnsureRuntimeMaterial();
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(EdgeWidthId, edgeWidth);
        }
    }

    public void SetNoiseScale(float noiseScale)
    {
        EnsureRuntimeMaterial();
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(NoiseScaleId, Mathf.Max(0.01f, noiseScale));
        }
    }

    private void CacheTargetRenderer()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void EnsureRuntimeMaterial()
    {
        CacheTargetRenderer();

        if (targetRenderer == null)
        {
            return;
        }

        if (runtimeMaterial != null)
        {
            return;
        }

        Material sourceMaterial = targetRenderer.sharedMaterial;
        Shader dissolveShader = Shader.Find("Sprites/DissolveBurn");

        if (dissolveShader != null)
        {
            runtimeMaterial = new Material(dissolveShader);

            if (sourceMaterial != null)
            {
                if (sourceMaterial.HasProperty(EdgeColorId))
                {
                    runtimeMaterial.SetColor(EdgeColorId, sourceMaterial.GetColor(EdgeColorId));
                }

                if (sourceMaterial.HasProperty(EdgeWidthId))
                {
                    runtimeMaterial.SetFloat(EdgeWidthId, sourceMaterial.GetFloat(EdgeWidthId));
                }

                if (sourceMaterial.HasProperty(NoiseScaleId))
                {
                    runtimeMaterial.SetFloat(NoiseScaleId, sourceMaterial.GetFloat(NoiseScaleId));
                }
            }
        }
        else if (sourceMaterial != null)
        {
            runtimeMaterial = new Material(sourceMaterial);
        }

        if (runtimeMaterial == null)
        {
            return;
        }

        targetRenderer.sharedMaterial = runtimeMaterial;
        ApplySpriteTexture();
    }

    private void ApplySpriteTexture()
    {
        if (targetRenderer == null || runtimeMaterial == null)
        {
            return;
        }

        Sprite sprite = targetRenderer.sprite;
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Texture currentTexture = runtimeMaterial.GetTexture(MainTexId);
        if (currentTexture != sprite.texture)
        {
            runtimeMaterial.SetTexture(MainTexId, sprite.texture);
        }
    }

    private void ApplyVisualState()
    {
        EnsureRuntimeMaterial();

        if (runtimeMaterial == null)
        {
            return;
        }

        ApplySpriteTexture();
        runtimeMaterial.SetFloat(DissolveAmountId, dissolveAmount);
    }

    private IEnumerator DissolveRoutine(float from, float to, float duration, Action onComplete)
    {
        if (duration <= 0f)
        {
            SetDissolveAmount(to);
            onComplete?.Invoke();
            dissolveRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        SetDissolveAmount(from);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDissolveAmount(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetDissolveAmount(to);
        onComplete?.Invoke();
        dissolveRoutine = null;
    }

    private void StopDissolveRoutine()
    {
        if (dissolveRoutine != null)
        {
            StopCoroutine(dissolveRoutine);
            dissolveRoutine = null;
        }
    }
}
