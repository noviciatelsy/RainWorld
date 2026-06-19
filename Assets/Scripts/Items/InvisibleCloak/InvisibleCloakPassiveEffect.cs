using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvisibleCloakPassiveEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer[] spritesToBeInvisible;
    // 需要跟着隐身一起变透明的贴图

    [Header("Invisible Settings")]
    [SerializeField] private float invisibleInterval = 5f;
    // 每隔多久触发一次虚化

    [SerializeField] private float invisibleDuration = 1f;
    // 每次虚化持续多久

    [Range(0f, 1f)]
    [SerializeField] private float invisibleAlphaMultiplier = 0.35f;
    // 虚化时的透明度倍率
    // 不是直接设置成 0.35，而是 原本Alpha * 0.35

    [SerializeField] private float fadeDuration = 0.15f;
    // 进入/退出虚化时的渐变时间

    [SerializeField] private bool becomeInvisibleImmediatelyOnEnable = false;
    // 开启效果后是否立刻进入第一次虚化
    // false：先等待 invisibleInterval
    // true：立刻虚化一次


    [Header("Time Settings")]
    [SerializeField] private bool useUnscaledTime = false;
    // 是否使用不受 Time.timeScale 影响的时间
    // 一般保持 false 即可


    public bool isInvisible { get; private set; }
    // 当前是否处于虚化状态
    // 怪物攻击判定可以读取这个 bool

    public bool IsEffectEnabled
    {
        get
        {
            return equippedCount > 0;
        }
    }

    public int EquippedCount
    {
        get
        {
            return equippedCount;
        }
    }


    private int equippedCount;
    // 当前装备了几个隐身斗篷
    // 大于等于 1 时，只开启一套效果

    private Coroutine invisibleLoopRoutine;
    // 虚化循环协程

    private Color[] originalSpriteColors;
    // 记录每个 SpriteRenderer 的原始颜色
    // 主要用于恢复原本 Alpha


    private void Awake()
    {
        TryAutoCollectSprites();

        CacheOriginalSpriteColors();

        SetSpritesAlphaMultiplierInstantly(1f);
    }


    private void OnDisable()
    {
        StopInvisibleLoop();

        isInvisible = false;

        SetSpritesAlphaMultiplierInstantly(1f);
    }


    /// <summary>
    /// 开启隐身斗篷效果。
    /// 每装备一个隐身斗篷时调用一次。
    /// </summary>
    public void EnableEffect()
    {
        equippedCount++;

        if (equippedCount > 1)
        {
            // 已经有隐身斗篷在生效了。
            // 重复装备时只记录数量，不额外启动一套循环。
            return;
        }

        StartInvisibleLoop();
    }


    /// <summary>
    /// 关闭隐身斗篷效果。
    /// 每移除一个隐身斗篷时调用一次。
    /// </summary>
    public void DisableEffect()
    {
        if (equippedCount <= 0)
        {
            equippedCount = 0;
            return;
        }

        equippedCount--;

        if (equippedCount > 0)
        {
            // 还有其他隐身斗篷正在装备。
            // 此时不能关闭效果。
            return;
        }

        StopInvisibleLoop();

        isInvisible = false;

        SetSpritesAlphaMultiplierInstantly(1f);
    }


    /// <summary>
    /// 强制清空效果。
    /// 适合玩家死亡、卸下全部被动道具、读档时调用。
    /// </summary>
    public void ForceClearEffect()
    {
        equippedCount = 0;

        StopInvisibleLoop();

        isInvisible = false;

        SetSpritesAlphaMultiplierInstantly(1f);
    }


    private void StartInvisibleLoop()
    {
        StopInvisibleLoop();

        invisibleLoopRoutine = StartCoroutine
        (
            InvisibleLoopRoutine()
        );
    }


    private void StopInvisibleLoop()
    {
        if (invisibleLoopRoutine == null)
        {
            return;
        }

        StopCoroutine(invisibleLoopRoutine);

        invisibleLoopRoutine = null;
    }


    private IEnumerator InvisibleLoopRoutine()
    {
        bool shouldWaitBeforeInvisible =
            !becomeInvisibleImmediatelyOnEnable;

        while (true)
        {
            if (shouldWaitBeforeInvisible)
            {
                yield return WaitForDuration(invisibleInterval);
            }

            shouldWaitBeforeInvisible = true;

            yield return EnterInvisibleRoutine();

            AudioManager.Instance.PlaySFX("UseItemInvisibleCloakSFX");
            yield return WaitForDuration(invisibleDuration);

            yield return ExitInvisibleRoutine();
        }
    }


    private IEnumerator EnterInvisibleRoutine()
    {
        isInvisible = true;

        yield return FadeSpritesAlphaMultiplier
        (
            invisibleAlphaMultiplier,
            fadeDuration
        );
    }


    private IEnumerator ExitInvisibleRoutine()
    {
        isInvisible = false;

        yield return FadeSpritesAlphaMultiplier
        (
            1f,
            fadeDuration
        );
    }


    private IEnumerator FadeSpritesAlphaMultiplier
    (
        float myTargetAlphaMultiplier,
        float myFadeDuration
    )
    {
        if (spritesToBeInvisible == null
            || spritesToBeInvisible.Length == 0)
        {
            yield break;
        }

        if (myFadeDuration <= 0f)
        {
            SetSpritesAlphaMultiplierInstantly
            (
                myTargetAlphaMultiplier
            );

            yield break;
        }

        float elapsedTime = 0f;

        float[] startAlphas =
            new float[spritesToBeInvisible.Length];

        for (int i = 0; i < spritesToBeInvisible.Length; i++)
        {
            SpriteRenderer currentSprite =
                spritesToBeInvisible[i];

            if (currentSprite == null)
            {
                continue;
            }

            startAlphas[i] =
                currentSprite.color.a;
        }

        while (elapsedTime < myFadeDuration)
        {
            elapsedTime += GetDeltaTime();

            float progress =
                Mathf.Clamp01(elapsedTime / myFadeDuration);

            float smoothProgress =
                Mathf.SmoothStep(0f, 1f, progress);

            for (int i = 0; i < spritesToBeInvisible.Length; i++)
            {
                SpriteRenderer currentSprite =
                    spritesToBeInvisible[i];

                if (currentSprite == null)
                {
                    continue;
                }

                float originalAlpha =
                    GetOriginalAlpha(i);

                float targetAlpha =
                    originalAlpha * myTargetAlphaMultiplier;

                Color currentColor =
                    currentSprite.color;

                currentColor.a =
                    Mathf.Lerp
                    (
                        startAlphas[i],
                        targetAlpha,
                        smoothProgress
                    );

                currentSprite.color =
                    currentColor;
            }

            yield return null;
        }

        SetSpritesAlphaMultiplierInstantly
        (
            myTargetAlphaMultiplier
        );
    }


    private void SetSpritesAlphaMultiplierInstantly(float myAlphaMultiplier)
    {
        if (spritesToBeInvisible == null)
        {
            return;
        }

        for (int i = 0; i < spritesToBeInvisible.Length; i++)
        {
            SpriteRenderer currentSprite =
                spritesToBeInvisible[i];

            if (currentSprite == null)
            {
                continue;
            }

            float originalAlpha =
                GetOriginalAlpha(i);

            Color currentColor =
                currentSprite.color;

            currentColor.a =
                originalAlpha * myAlphaMultiplier;

            currentSprite.color =
                currentColor;
        }
    }


    private float GetOriginalAlpha(int myIndex)
    {
        if (originalSpriteColors == null
            || myIndex < 0
            || myIndex >= originalSpriteColors.Length)
        {
            return 1f;
        }

        return originalSpriteColors[myIndex].a;
    }


    private IEnumerator WaitForDuration(float myDuration)
    {
        if (myDuration <= 0f)
        {
            yield return null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < myDuration)
        {
            elapsedTime += GetDeltaTime();

            yield return null;
        }
    }


    private float GetDeltaTime()
    {
        if (useUnscaledTime)
        {
            return Time.unscaledDeltaTime;
        }

        return Time.deltaTime;
    }


    private void TryAutoCollectSprites()
    {

        if (spritesToBeInvisible != null
            && spritesToBeInvisible.Length > 0)
        {
            return;
        }

        spritesToBeInvisible =
            GetComponentsInChildren<SpriteRenderer>();
    }


    private void CacheOriginalSpriteColors()
    {
        if (spritesToBeInvisible == null)
        {
            originalSpriteColors =
                new Color[0];

            return;
        }

        originalSpriteColors =
            new Color[spritesToBeInvisible.Length];

        for (int i = 0; i < spritesToBeInvisible.Length; i++)
        {
            SpriteRenderer currentSprite =
                spritesToBeInvisible[i];

            if (currentSprite == null)
            {
                originalSpriteColors[i] =
                    Color.white;

                continue;
            }

            originalSpriteColors[i] =
                currentSprite.color;
        }
    }
}