using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image characterSpriteImage;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Typewriter Settings")]
    [SerializeField] private float characterInterval = 0.03f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Input Settings")]
    [SerializeField] private float inputLockTimeAfterOpen = 0.08f;
    [SerializeField] private float inputLockTimeAfterLineStart = 0.03f;

    public CanvasGroup canvasGroup {  get; private set; }

    private DialogueDataSO currentDialogue;
    private Action onDialogueClosed;

    private Coroutine typewriterCoroutine;
    private InGameUI inGameUI;

    private int currentSegmentIndex = -1;
    private int currentSegmentTotalCharacterCount;

    private bool isOpen;
    private bool isTyping;
    private bool callbackInvoked;

    private float inputLockedUntilTime;

    private void Awake()
    {
        inGameUI = GetComponentInParent<InGameUI>();
        canvasGroup=GetComponent<CanvasGroup>();    
    }

    private void OnEnable()
    {
        inGameUI.ShowHud(false);
    }

    private void OnDisable()
    {
        inGameUI.ShowHud(true);
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (IsInputLocked())
        {
            return;
        }

        if (!IsAdvancePressed())
        {
            return;
        }

        // 正在打字时点击：立刻显示完整当前段文字
        if (isTyping)
        {
            CompleteCurrentSegmentInstantly();
            return;
        }

        // 当前段已经完整显示时点击：进入下一段
        TryShowNextSegment();
    }

    public void StartDialogue(DialogueDataSO dialogueData, Action onClosed = null)
    {
        if (dialogueData == null)
        {
            Debug.LogWarning($"{nameof(DialogueUI)} 打开失败：传入的 DialogueDataSO 为空。");
            return;
        }

        // 如果正在播放旧对话，直接关闭旧对话，但不触发旧回调
        if (isOpen)
        {
            CloseInternal(false);
        }

        currentDialogue = dialogueData;
        onDialogueClosed = onClosed;

        currentSegmentIndex = -1;
        currentSegmentTotalCharacterCount = 0;

        isOpen = true;
        isTyping = false;
        callbackInvoked = false;

        gameObject.SetActive(true);

        // 防止“用于打开对话的那一下点击”立刻跳过第一句话
        LockInput(inputLockTimeAfterOpen);

        TryShowNextSegment();
    }

    public void Close()
    {
        CloseInternal(true);
    }

    private void TryShowNextSegment()
    {
        if (currentDialogue == null || currentDialogue.Segments == null || currentDialogue.Segments.Count == 0)
        {
            Close();
            return;
        }

        currentSegmentIndex++;

        if (currentSegmentIndex >= currentDialogue.Segments.Count)
        {
            Close();
            return;
        }

        DialogueSegment segment = currentDialogue.Segments[currentSegmentIndex];

        ShowCharacterSprite(segment);
        StartTypewriter(segment != null ? segment.Content : string.Empty);
    }

    private void ShowCharacterSprite(DialogueSegment segment)
    {
        if (characterSpriteImage == null)
        {
            return;
        }

        Sprite spriteToShow = null;

        if (segment != null && segment.CharacterSprite != null)
        {
            spriteToShow = segment.CharacterSprite;
        }
        else if (currentDialogue != null)
        {
            spriteToShow = currentDialogue.DefaultCharacterSprite;
        }

        characterSpriteImage.sprite = spriteToShow;
        characterSpriteImage.enabled = spriteToShow != null;
    }

    private void StartTypewriter(string content)
    {
        if (dialogueText == null)
        {
            Debug.LogWarning($"{nameof(DialogueUI)} 缺少 DialogueText 引用。");
            Close();
            return;
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        typewriterCoroutine = StartCoroutine(TypewriterCoroutine(content));
    }

    private IEnumerator TypewriterCoroutine(string content)
    {
        isTyping = true;

        dialogueText.text = content;
        dialogueText.maxVisibleCharacters = 0;

        // 让 TMP 立刻生成文本信息，这样才能拿到正确的 characterCount
        dialogueText.ForceMeshUpdate();

        currentSegmentTotalCharacterCount = dialogueText.textInfo.characterCount;

        LockInput(inputLockTimeAfterLineStart);

        for (int i = 0; i <= currentSegmentTotalCharacterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            if (i >= currentSegmentTotalCharacterCount)
            {
                break;
            }

            yield return WaitForSecondsByMode(characterInterval);
        }

        isTyping = false;
        typewriterCoroutine = null;
    }

    private void CompleteCurrentSegmentInstantly()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        dialogueText.maxVisibleCharacters = currentSegmentTotalCharacterCount;

        isTyping = false;
    }

    private IEnumerator WaitForSecondsByMode(float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void CloseInternal(bool shouldInvokeCallback)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        bool shouldActuallyInvokeCallback = shouldInvokeCallback && isOpen && !callbackInvoked;
        Action cachedCallback = onDialogueClosed;

        isOpen = false;
        isTyping = false;
        callbackInvoked = true;

        currentDialogue = null;
        onDialogueClosed = null;

        currentSegmentIndex = -1;
        currentSegmentTotalCharacterCount = 0;

        HideInstantly();

        if (shouldActuallyInvokeCallback)
        {
            cachedCallback?.Invoke();
        }
    }

    private void HideInstantly()
    {
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        if (characterSpriteImage != null)
        {
            characterSpriteImage.sprite = null;
            characterSpriteImage.enabled = false;
        }
        gameObject.SetActive(false);

    }

    private void LockInput(float duration)
    {
        float lockEndTime = GetCurrentTime() + Mathf.Max(0f, duration);
        inputLockedUntilTime = Mathf.Max(inputLockedUntilTime, lockEndTime);
    }

    private bool IsInputLocked()
    {
        return GetCurrentTime() < inputLockedUntilTime;
    }

    private float GetCurrentTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    private bool IsAdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif

        return false;
    }
}