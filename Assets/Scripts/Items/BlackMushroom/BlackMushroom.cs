using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackMushroom : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] spritesToHide;

    private Coroutine hideSpritesCoroutine;

    /// <summary>
    /// 暂时隐藏 spritesToHide 中的所有 SpriteRenderer。
    /// </summary>
    /// <param name="hideTime">隐藏持续时间，单位：秒。</param>
    public void HideSpritesTemporarily(float hideTime)
    {
        // 如果隐藏时间无效，就不执行隐藏
        if (hideTime <= 0f)
        {
            return;
        }

        // 如果上一次隐藏还没结束，就先停止，避免旧协程提前把玩家显示回来
        if (hideSpritesCoroutine != null)
        {
            StopCoroutine(hideSpritesCoroutine);
        }

        hideSpritesCoroutine = StartCoroutine(HideSpritesCoroutine(hideTime));
    }

    private IEnumerator HideSpritesCoroutine(float hideTime)
    {
        SetSpritesVisible(false);

        yield return new WaitForSeconds(hideTime);

        SetSpritesVisible(true);

        hideSpritesCoroutine = null;
    }

    private void SetSpritesVisible(bool isVisible)
    {
        for (int i = 0; i < spritesToHide.Length; i++)
        {
            // 防止数组里有空引用导致报错
            if (spritesToHide[i] == null)
            {
                continue;
            }

            spritesToHide[i].enabled = isVisible;
        }
    }
}