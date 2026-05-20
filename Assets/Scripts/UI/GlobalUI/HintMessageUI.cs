using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintMessageUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject viewRoot;                 // 背景根（可以SetActive隐藏）
    [SerializeField] private CanvasGroup viewCanvasGroup;         // 背景淡入淡出
    [SerializeField] private RectTransform layoutRoot;            // 挂VerticalLayoutGroup的物体

    [Header("Quick (Top)")]
    [SerializeField] private TextMeshProUGUI quickText;
    [SerializeField] private CanvasGroup quickCanvasGroup;
    [SerializeField] private LayoutElement quickLayoutElement;

    [Header("Long (Bottom)")]
    [SerializeField] private TextMeshProUGUI longText;
    [SerializeField] private CanvasGroup longCanvasGroup;
    [SerializeField] private LayoutElement longLayoutElement;

    [Header("Timings")]
    [SerializeField] private float quickShowSeconds = 0.75f;       // 每条短期显示多久
    [SerializeField] private float switchPauseSeconds = 0.15f;     // 切换停顿（只隐藏文本）
    [SerializeField] private bool useFade = true;                 // 是否淡入淡出
    [SerializeField] private float fadeSeconds = 0.12f;           // 淡入淡出时间


    private Queue<string> quickQueue = new Queue<string>();
    private Coroutine quickCoroutine;
    private Coroutine longCoroutine;

    private bool isLongActive;
    private string longMessage;

    private void Awake()
    {
        HideAllImmediately();
    }

    // Public API
    // 显示短期消息：排队处理
    public void ShowQuickMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        quickQueue.Enqueue(message);

        // 开启短期队列协程
        if (quickCoroutine == null)
        {
            quickCoroutine = StartCoroutine(QuickDriver());
        }

        // 背景确保可见
        EnsureViewVisible();
    }


    // 显示长期消息：直接显示在下方（如果已有长期，则替换，并有切换停顿）
    public void ShowLongTimeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        isLongActive = true;
        longMessage = message;

        // 如果正在切换，先停掉旧协程再开始（避免并发打架）
        if (longCoroutine != null)
        {
            StopCoroutine(longCoroutine);
        }
        longCoroutine = StartCoroutine(LongSwitch(message));

        EnsureViewVisible();
    }


    // 停止长期消息：隐藏下方长期文本（如果此时没有短期队列，则背景也隐藏）
    public void StopLongTimeMessage()
    {
        if (!isLongActive)
        {
            return;
        }

        isLongActive = false;
        longMessage = string.Empty;

        if (longCoroutine != null)
        {
            StopCoroutine(longCoroutine);
            longCoroutine = null;
        }

        StartCoroutine(HideLongAndRefresh());
    }


    // Drivers
    private IEnumerator QuickDriver()
    {
        // 短期消息开始：先把Quick放进布局（占位），并显示
        SetQuickLayoutActive(true);
        EnsureViewVisible();

        while (quickQueue.Count > 0)
        {
            string msg = quickQueue.Dequeue();

            // 切换停顿：隐藏文本但保留布局占位（避免Long上下跳）
            yield return HideQuickTextKeepSpace();
            yield return PauseRealtime(switchPauseSeconds);

            quickText.text = msg;
            yield return ShowQuickTextKeepSpace();

            // 显示固定时间
            float t = 0f;
            while (t < quickShowSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 队列结束：Quick真正不需要显示 -> 从布局移除，让Long（如果有）居中
        yield return HideQuickTextRemoveSpace();

        quickCoroutine = null;

        // 如果此时也没有长期：隐藏背景
        TryHideViewIfNothingToShow();
    }

    private IEnumerator LongSwitch(string newMessage)
    {
        // Long要显示：放进布局
        SetLongLayoutActive(true);
        ForceRebuildLayout();

        // 如果已经有显示内容：做一次“只隐藏文本”的切换停顿
        if (longCanvasGroup.alpha > 0.01f)
        {
            yield return HideLongTextKeepSpace();
            yield return PauseRealtime(switchPauseSeconds);
        }

        longText.text = newMessage;
        yield return ShowLongTextKeepSpace();

        longCoroutine = null;
    }


    // View Visibility
    private void EnsureViewVisible()
    {
        if (!viewRoot.activeSelf)
        {
            viewRoot.SetActive(true);
        }

        if (useFade)
        {
            StartCoroutine(FadeCanvasGroup(viewCanvasGroup, 1f, fadeSeconds));
        }
        else
        {
            viewCanvasGroup.alpha = 1f;
        }
    }

    private void TryHideViewIfNothingToShow()
    {
        bool hasQuickPending = (quickQueue.Count > 0) || (quickCoroutine != null);
        bool hasLong = isLongActive;

        if (!hasQuickPending && !hasLong)
        {
            StartCoroutine(HideView());
        }
    }

    private IEnumerator HideView()
    {
        // 先把两个文本都从布局移除（避免背景隐藏时还占位）
        yield return HideQuickTextRemoveSpace();
        yield return HideLongTextRemoveSpace();

        yield return PauseRealtime(switchPauseSeconds);

        if (useFade)
        {
            yield return FadeCanvasGroup(viewCanvasGroup, 0f, fadeSeconds);
        }
        else
        {
            viewCanvasGroup.alpha = 0f;
        }

        viewRoot.SetActive(false);
    }

    private void HideAllImmediately()
    {
        quickText.text = string.Empty;
        longText.text = string.Empty;

        SetQuickLayoutActive(false);
        SetLongLayoutActive(false);

        quickCanvasGroup.alpha = 0f;
        longCanvasGroup.alpha = 0f;

        viewCanvasGroup.alpha = 0f;
        viewRoot.SetActive(false);
    }


    // Quick: show/hide with layout rules
    private IEnumerator ShowQuickTextKeepSpace()
    {
        SetQuickLayoutActive(true);
        ForceRebuildLayout();

        if (useFade)
        {
            yield return FadeCanvasGroup(quickCanvasGroup, 1f, fadeSeconds);
        }
        else
        {
            quickCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator HideQuickTextKeepSpace()
    {
        SetQuickLayoutActive(true); // 保留布局占位
        ForceRebuildLayout();

        if (useFade)
        {
            yield return FadeCanvasGroup(quickCanvasGroup, 0f, fadeSeconds);
        }
        else
        {
            quickCanvasGroup.alpha = 0f;
        }
    }

    private IEnumerator HideQuickTextRemoveSpace()
    {
        if (useFade)
        {
            yield return FadeCanvasGroup(quickCanvasGroup, 0f, fadeSeconds);
        }
        else
        {
            quickCanvasGroup.alpha = 0f;
        }

        SetQuickLayoutActive(false); // 真正移除布局
        ForceRebuildLayout();
    }

    private void SetQuickLayoutActive(bool activeInLayout)
    {
        if (quickLayoutElement != null)
        {
            quickLayoutElement.ignoreLayout = !activeInLayout;
        }
    }


    // Long: show/hide with layout rules
    private IEnumerator ShowLongTextKeepSpace()
    {
        SetLongLayoutActive(true);
        ForceRebuildLayout();

        if (useFade)
        {
            yield return FadeCanvasGroup(longCanvasGroup, 1f, fadeSeconds);
        }
        else
        {
            longCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator HideLongTextKeepSpace()
    {
        SetLongLayoutActive(true); // 保留布局占位
        ForceRebuildLayout();

        if (useFade)
        {
            yield return FadeCanvasGroup(longCanvasGroup, 0f, fadeSeconds);
        }
        else
        {
            longCanvasGroup.alpha = 0f;
        }
    }

    private IEnumerator HideLongTextRemoveSpace()
    {
        if (useFade)
        {
            yield return FadeCanvasGroup(longCanvasGroup, 0f, fadeSeconds);
        }
        else
        {
            longCanvasGroup.alpha = 0f;
        }

        SetLongLayoutActive(false); // 真正移除布局
        ForceRebuildLayout();
    }

    private void SetLongLayoutActive(bool activeInLayout)
    {
        if (longLayoutElement != null)
        {
            longLayoutElement.ignoreLayout = !activeInLayout;
        }
    }

    private IEnumerator HideLongAndRefresh()
    {
        // 停止长期：隐藏并从布局移除，让Quick（如果有）居中
        yield return HideLongTextRemoveSpace();
        TryHideViewIfNothingToShow();
    }


    // Utilities
    private void ForceRebuildLayout()
    {
        if (layoutRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
    }

    private IEnumerator PauseRealtime(float seconds)
    {
        if (seconds <= 0f)
        {
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float seconds)
    {
        if (cg == null)
        {
            yield break;
        }

        if (!useFade || seconds <= 0f)
        {
            cg.alpha = target;
            yield break;
        }

        float start = cg.alpha;
        float t = 0f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            cg.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }

        cg.alpha = target;
    }
}
