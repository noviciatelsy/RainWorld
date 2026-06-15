using System;
using UnityEngine;

/// <summary>
/// 鼹鼠偷取爪：以 pos 为圆心，radius 内匀速圆周运动；texture scale.x 控制左右朝向。
/// </summary>
public class MoleClaw : MonoBehaviour
{
    [Tooltip("用于翻转朝向的 Transform（ClawTexture）")]
    public Transform textureTransform;

    [Tooltip("绕圈运动的视觉节点（应为 ClawTexture 的子物体）；勿填根节点")]
    public Transform orbitTransform;

    [Tooltip("圆周运动半径")]
    public float radius = 0.5f;

    [Tooltip("绕圈一周周期（秒）")]
    public float orbitPeriod = 0.6f;

    [Tooltip("出现/消失淡入淡出时长（秒）")]
    public float fadeDuration = 0.2f;

    private Vector2 centerWorld;
    private bool faceLeft = true;
    private bool running;
    private float phaseElapsed;
    private Vector3 textureBaseScale = Vector3.one;
    private Vector3 orbitRestLocalPos;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private float[] baseAlphas;
    private float fadeAlpha = 1f;
    private bool fadingIn;
    private bool fadingOut;
    private Action fadeOutComplete;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        ResolveTransforms();
        CacheDefaults();
        CacheRenderers();
    }

    private void OnEnable()
    {
        ResolveTransforms();
        CacheDefaults();
        RefreshRendererList();
        phaseElapsed = 0f;
    }

    private void OnDisable()
    {
        StopFadeCoroutine();
        running = false;
        fadingIn = false;
        fadingOut = false;
        fadeOutComplete = null;
        phaseElapsed = 0f;

        if (orbitTransform != null)
        {
            orbitTransform.localPosition = orbitRestLocalPos;
        }

        SetAlpha(0f);
    }

    public void SetHiddenInstant()
    {
        StopFadeCoroutine();
        fadingIn = false;
        fadingOut = false;
        fadeOutComplete = null;
        fadeAlpha = 0f;
        SetAlpha(0f);
    }

    public void PlayAppear()
    {
        StopFadeCoroutine();
        fadingOut = false;
        fadeOutComplete = null;
        fadingIn = true;
        fadeAlpha = 0f;
        RefreshRendererList();
        SetAlpha(0f);
        fadeCoroutine = StartCoroutine(FadeTo(1f, () => fadingIn = false));
    }

    public void PlayDisappear(Action onComplete)
    {
        StopFadeCoroutine();
        fadingIn = false;
        fadingOut = true;
        fadeOutComplete = onComplete;
        fadeCoroutine = StartCoroutine(FadeTo(0f, () =>
        {
            fadingOut = false;
            fadeOutComplete?.Invoke();
            fadeOutComplete = null;
        }));
    }

    public bool IsFadingOut => fadingOut;

    private void ResolveTransforms()
    {
        if (textureTransform == null)
        {
            Transform found = transform.Find("ClawTexture");
            textureTransform = found != null ? found : transform;
        }

        if (orbitTransform == null || orbitTransform == transform)
        {
            if (textureTransform != null && textureTransform.childCount > 0)
            {
                orbitTransform = textureTransform.GetChild(0);
            }
            else
            {
                orbitTransform = textureTransform;
            }
        }
    }

    private void CacheDefaults()
    {
        if (textureTransform != null)
        {
            textureBaseScale = textureTransform.localScale;
        }

        if (orbitTransform != null)
        {
            orbitRestLocalPos = orbitTransform.localPosition;
        }
    }

    private void CacheRenderers()
    {
        RefreshRendererList();
    }

    private void RefreshRendererList()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        if (baseColors == null || baseColors.Length != renderers.Length)
        {
            baseColors = new Color[renderers.Length];
            baseAlphas = new float[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = renderers[i].color;
                baseColors[i] = color;
                baseAlphas[i] = color.a > 0f ? color.a : 1f;
            }
        }
    }

    private void StopFadeCoroutine()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private System.Collections.IEnumerator FadeTo(float targetAlpha, Action onComplete)
    {
        float startAlpha = fadeAlpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetAlpha(fadeAlpha);
            yield return null;
        }

        fadeAlpha = targetAlpha;
        SetAlpha(fadeAlpha);
        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 更新圆心世界坐标与朝向；pos 为绝对世界坐标。
    /// </summary>
    public void ClawMove(Vector2 worldPos, bool face = true)
    {
        centerWorld = worldPos;
        faceLeft = face;
        running = true;
    }

    public void StopMove()
    {
        running = false;
        phaseElapsed = 0f;

        if (orbitTransform != null)
        {
            orbitTransform.localPosition = orbitRestLocalPos;
        }
    }

    private void Update()
    {
        if (!running)
        {
            return;
        }

        transform.SetPositionAndRotation(
            new Vector3(centerWorld.x, centerWorld.y, transform.position.z),
            transform.rotation
        );
        ApplyFacing();

        float period = Mathf.Max(0.01f, orbitPeriod);
        phaseElapsed += Time.deltaTime;
        float theta = phaseElapsed * Mathf.PI * 2f / period;
        Vector2 orbitOffset = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;

        if (orbitTransform != null && orbitTransform != transform)
        {
            orbitTransform.localPosition = orbitRestLocalPos + new Vector3(orbitOffset.x, orbitOffset.y, 0f);
        }
        else if (textureTransform != null && textureTransform != transform)
        {
            textureTransform.localPosition = orbitRestLocalPos + new Vector3(orbitOffset.x, orbitOffset.y, 0f);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (renderers == null || baseColors == null || baseAlphas == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Color color = baseColors[i];
            color.a = baseAlphas[i] * alpha;
            renderers[i].color = color;
        }
    }

    private void ApplyFacing()
    {
        if (textureTransform == null)
        {
            return;
        }

        Vector3 scale = textureBaseScale;
        scale.x = Mathf.Abs(textureBaseScale.x) * (faceLeft ? 1f : -1f);
        textureTransform.localScale = scale;
    }
}
