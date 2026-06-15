using System.Collections;
using UnityEngine;

public class ShrinkMushroom : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform transformToShrink;
    // 要被缩小的目标


    [Header("Shrink Settings")]
    [Range(0.01f, 1f)]
    [SerializeField] private float shrinkMultiplier = 0.5f;
    // 每次使用缩小菇时的缩小倍率
    // 0.5 表示变成当前大小的一半

    [SerializeField] private float shrinkDuration = 60f;
    // 每次缩小持续时间

    [SerializeField] private bool affectZScale = false;
    // 2D 项目通常只缩放 X/Y
    // Z 一般不需要动

    [Header("Time Settings")]
    [SerializeField] private bool useUnscaledTime = false;
    // 是否使用不受 Time.timeScale 影响的时间
    // 一般保持 false 即可


    public int ActiveShrinkCount
    {
        get
        {
            return activeShrinkCount;
        }
    }

    public float CurrentTotalShrinkMultiplier
    {
        get
        {
            return currentTotalShrinkMultiplier;
        }
    }


    private int activeShrinkCount;
    // 当前正在生效的缩小菇数量

    private float currentTotalShrinkMultiplier = 1f;
    // 当前总缩小倍率
    // 例如连续吃 3 个 0.5：
    // 1 * 0.5 * 0.5 * 0.5 = 0.125




    private void OnDisable()
    {
        ClearAllShrinkEffects();
    }


    private void OnValidate()
    {
        shrinkMultiplier =
            Mathf.Clamp(shrinkMultiplier, 0.01f, 1f);

        shrinkDuration =
            Mathf.Max(0f, shrinkDuration);
    }


    /// <summary>
    /// 使用缩小菇。
    /// 每调用一次，就会叠加一层缩小效果。
    /// </summary>
    public bool UseMushroom()
    {
        if (transformToShrink == null)
        {
            return false;
        }

        if (shrinkDuration <= 0f)
        {
            return false;
        }

        StartCoroutine
        (
            ShrinkRoutine
            (
                shrinkMultiplier,
                shrinkDuration
            )
        );

        return true;
    }


    /// <summary>
    /// 使用指定倍率和持续时间的缩小效果。
    /// 方便以后做不同品质的缩小菇。
    /// </summary>
    public bool UseMushroom
    (
        float myShrinkMultiplier,
        float myShrinkDuration
    )
    {
        if (transformToShrink == null)
        {
            return false;
        }

        myShrinkMultiplier =
            Mathf.Clamp(myShrinkMultiplier, 0.01f, 1f);

        myShrinkDuration =
            Mathf.Max(0f, myShrinkDuration);

        if (myShrinkDuration <= 0f)
        {
            return false;
        }

        StartCoroutine
        (
            ShrinkRoutine
            (
                myShrinkMultiplier,
                myShrinkDuration
            )
        );

        return true;
    }


    private IEnumerator ShrinkRoutine
    (
        float myShrinkMultiplier,
        float myShrinkDuration
    )
    {
        ApplyShrinkMultiplier(myShrinkMultiplier);

        yield return WaitForDuration(myShrinkDuration);

        RemoveShrinkMultiplier(myShrinkMultiplier);
    }


    /// <summary>
    /// 应用一层缩小倍率。
    /// </summary>
    private void ApplyShrinkMultiplier(float myShrinkMultiplier)
    {
        activeShrinkCount++;

        currentTotalShrinkMultiplier *= myShrinkMultiplier;

        Vector3 currentScale =
            transformToShrink.localScale;

        currentScale.x *= myShrinkMultiplier;
        currentScale.y *= myShrinkMultiplier;

        if (affectZScale)
        {
            currentScale.z *= myShrinkMultiplier;
        }

        transformToShrink.localScale =
            currentScale;
    }


    /// <summary>
    /// 移除一层缩小倍率。
    /// </summary>
    private void RemoveShrinkMultiplier(float myShrinkMultiplier)
    {
        if (activeShrinkCount <= 0)
        {
            activeShrinkCount = 0;
            currentTotalShrinkMultiplier = 1f;
            return;
        }

        activeShrinkCount--;

        currentTotalShrinkMultiplier /= myShrinkMultiplier;

        Vector3 currentScale =
            transformToShrink.localScale;

        currentScale.x /= myShrinkMultiplier;
        currentScale.y /= myShrinkMultiplier;

        if (affectZScale)
        {
            currentScale.z /= myShrinkMultiplier;
        }

        transformToShrink.localScale =
            currentScale;
    }


    /// <summary>
    /// 清空全部缩小效果。
    /// 
    /// 适合玩家死亡、切场景、卸下道具、读档时调用。
    /// </summary>
    public void ClearAllShrinkEffects()
    {
        StopAllCoroutines();

        if (transformToShrink == null)
        {
            activeShrinkCount = 0;
            currentTotalShrinkMultiplier = 1f;
            return;
        }

        if (Mathf.Approximately(currentTotalShrinkMultiplier, 1f))
        {
            activeShrinkCount = 0;
            currentTotalShrinkMultiplier = 1f;
            return;
        }

        Vector3 currentScale =
            transformToShrink.localScale;

        currentScale.x /= currentTotalShrinkMultiplier;
        currentScale.y /= currentTotalShrinkMultiplier;

        if (affectZScale)
        {
            currentScale.z /= currentTotalShrinkMultiplier;
        }

        transformToShrink.localScale =
            currentScale;

        activeShrinkCount = 0;
        currentTotalShrinkMultiplier = 1f;
    }


    private IEnumerator WaitForDuration(float myDuration)
    {
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
}