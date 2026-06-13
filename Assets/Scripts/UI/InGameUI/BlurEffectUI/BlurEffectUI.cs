using UnityEngine;
using UnityEngine.UI;

public class BlurEffectUI : MonoBehaviour
{
    [Header("整体控制")]
    private CanvasGroup canvasGroup;

    [Header("暗角 Image")]
    [SerializeField] private Image vignetteImage;

    [Header("毒雾 RawImage")]
    [SerializeField] private RawImage noiseRawImage;

    [Header("眩晕波纹 Image 或 RawImage")]
    [SerializeField] private Graphic dizzyWaveGraphic;

    [Header("眩晕波纹 RectTransform")]
    [SerializeField] private RectTransform dizzyWaveRect;

    [Header("最大整体透明度")]
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;

    [Header("淡入淡出速度")]
    [SerializeField] private float fadeSpeed = 4f;

    [Header("毒雾流动速度")]
    [SerializeField] private Vector2 noiseScrollSpeed = new Vector2(0.035f, 0.018f);

    [Header("眩晕波纹旋转速度")]
    [SerializeField] private float dizzyRotateSpeed = 3f;

    [Header("眩晕波纹缩放幅度")]
    [SerializeField] private float dizzyScaleAmount = 0.035f;

    [Header("眩晕波纹缩放速度")]
    [SerializeField] private float dizzyScaleSpeed = 1.6f;

    private float targetAlpha;
    private float currentAlpha;

    private Rect noiseUvRect;
    private Vector3 dizzyBaseScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (vignetteImage != null)
        {
            vignetteImage.raycastTarget = false;
        }

        if (noiseRawImage != null)
        {
            noiseRawImage.raycastTarget = false;
            noiseUvRect = noiseRawImage.uvRect;
        }

        if (dizzyWaveGraphic != null)
        {
            dizzyWaveGraphic.raycastTarget = false;
        }

        if (dizzyWaveRect != null)
        {
            dizzyBaseScale = dizzyWaveRect.localScale;
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            EnableBlur(true);
        }
        if(Input.GetKeyDown(KeyCode.N))
        {
            EnableBlur(false);
        }
        UpdateFade();
        UpdateNoise();
        UpdateDizzyWave();
    }

    public void EnableBlur(bool showBlur)
    {
        targetAlpha = showBlur ? maxAlpha : 0f;
    }

    public void SetBlurAmount(float amount01)
    {
        float amount = Mathf.Clamp01(amount01);
        targetAlpha = maxAlpha * amount;
    }

    private void UpdateFade()
    {
        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );

        if (canvasGroup != null)
        {
            canvasGroup.alpha = currentAlpha;
        }
    }

    private void UpdateNoise()
    {
        if (noiseRawImage == null)
        {
            return;
        }

        // 滚动 UV，让毒雾贴图像是在屏幕上慢慢流动
        noiseUvRect.x += noiseScrollSpeed.x * Time.unscaledDeltaTime;
        noiseUvRect.y += noiseScrollSpeed.y * Time.unscaledDeltaTime;

        noiseRawImage.uvRect = noiseUvRect;
    }

    private void UpdateDizzyWave()
    {
        if (dizzyWaveRect == null)
        {
            return;
        }

        // 轻微旋转，制造头晕感
        dizzyWaveRect.Rotate(
            0f,
            0f,
            dizzyRotateSpeed * Time.unscaledDeltaTime
        );

        // 轻微呼吸缩放，避免波纹太死板
        float scaleOffset = Mathf.Sin(Time.unscaledTime * dizzyScaleSpeed) * dizzyScaleAmount;
        dizzyWaveRect.localScale = dizzyBaseScale * (1f + scaleOffset);
    }
}
