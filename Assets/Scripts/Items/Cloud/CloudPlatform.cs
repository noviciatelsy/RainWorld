using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudPlatform : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    // 云平台身上的所有 SpriteRenderer

    [SerializeField] private Collider2D[] platformColliders;
    // 云平台身上的碰撞体

    [SerializeField] private bool autoCollectComponents = true;
    // 是否自动收集子物体上的 SpriteRenderer 和 Collider2D


    [Header("Life Settings")]
    [SerializeField] private float fadeInDuration = 0.25f;
    // 生成时淡入时间

    [SerializeField] private float lifeDuration = 5f;
    // 完全显现后存在多久

    [SerializeField] private float fadeOutDuration = 0.35f;
    // 消失前淡出时间

    [SerializeField] private bool autoStartLifeCycle = true;
    // 是否在 Start 时自动开始生命周期


    [Header("Collision Settings")]
    [SerializeField] private bool enableCollidersDuringFadeIn = true;
    // 淡入期间是否启用碰撞体

    [SerializeField] private bool disableCollidersDuringFadeOut = false;
    // 淡出期间是否禁用碰撞体
    // 如果希望云还没完全消失前仍然能站，保持 false


    private Coroutine lifeRoutine;
    // 生命周期协程

    private Color[] originalColors;
    // 原始颜色

    private bool hasBeenInitialized;
    // 是否已经初始化


    private void Awake()
    {
        if (autoCollectComponents)
        {
            CollectComponents();
        }

        CacheOriginalColors();
    }


    private void Start()
    {
        if (!autoStartLifeCycle)
        {
            return;
        }

        Initialize();
    }


    private void OnDisable()
    {
        if (lifeRoutine == null)
        {
            return;
        }

        StopCoroutine(lifeRoutine);
        lifeRoutine = null;
    }


    /// <summary>
    /// 初始化云平台生命周期。
    /// </summary>
    public void Initialize()
    {
        if (hasBeenInitialized)
        {
            return;
        }

        hasBeenInitialized = true;

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
        }

        lifeRoutine = StartCoroutine
        (
            LifeRoutine()
        );
    }


    private IEnumerator LifeRoutine()
    {
        SetCollidersEnabled(enableCollidersDuringFadeIn);

        SetAlphaMultiplierInstantly(0f);

        yield return FadeAlphaRoutine
        (
            0f,
            1f,
            fadeInDuration
        );

        SetAlphaMultiplierInstantly(1f);

        SetCollidersEnabled(true);

        if (lifeDuration > 0f)
        {
            yield return new WaitForSeconds(lifeDuration);
        }

        if (disableCollidersDuringFadeOut)
        {
            SetCollidersEnabled(false);
        }

        yield return FadeAlphaRoutine
        (
            1f,
            0f,
            fadeOutDuration
        );

        Destroy(gameObject);
    }


    private IEnumerator FadeAlphaRoutine
    (
        float myFromAlphaMultiplier,
        float myToAlphaMultiplier,
        float myDuration
    )
    {
        if (myDuration <= 0f)
        {
            SetAlphaMultiplierInstantly(myToAlphaMultiplier);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < myDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsedTime / myDuration);

            float smoothProgress =
                Mathf.SmoothStep(0f, 1f, progress);

            float currentAlphaMultiplier =
                Mathf.Lerp
                (
                    myFromAlphaMultiplier,
                    myToAlphaMultiplier,
                    smoothProgress
                );

            SetAlphaMultiplierInstantly
            (
                currentAlphaMultiplier
            );

            yield return null;
        }

        SetAlphaMultiplierInstantly(myToAlphaMultiplier);
    }


    private void SetAlphaMultiplierInstantly(float myAlphaMultiplier)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer currentSpriteRenderer =
                spriteRenderers[i];

            if (currentSpriteRenderer == null)
            {
                continue;
            }

            Color originalColor =
                GetOriginalColor(i);

            Color currentColor =
                originalColor;

            currentColor.a =
                originalColor.a * myAlphaMultiplier;

            currentSpriteRenderer.color =
                currentColor;
        }
    }


    private void SetCollidersEnabled(bool myEnabled)
    {
        if (platformColliders == null)
        {
            return;
        }

        for (int i = 0; i < platformColliders.Length; i++)
        {
            Collider2D currentCollider =
                platformColliders[i];

            if (currentCollider == null)
            {
                continue;
            }

            currentCollider.enabled =
                myEnabled;
        }
    }


    private void CollectComponents()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers =
                GetComponentsInChildren<SpriteRenderer>();
        }

        if (platformColliders == null || platformColliders.Length == 0)
        {
            platformColliders =
                GetComponentsInChildren<Collider2D>();
        }
    }


    private void CacheOriginalColors()
    {
        if (spriteRenderers == null)
        {
            originalColors =
                new Color[0];

            return;
        }

        originalColors =
            new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer currentSpriteRenderer =
                spriteRenderers[i];

            if (currentSpriteRenderer == null)
            {
                originalColors[i] =
                    Color.white;

                continue;
            }

            originalColors[i] =
                currentSpriteRenderer.color;
        }
    }


    private Color GetOriginalColor(int myIndex)
    {
        if (originalColors == null
            || myIndex < 0
            || myIndex >= originalColors.Length)
        {
            return Color.white;
        }

        return originalColors[myIndex];
    }
}