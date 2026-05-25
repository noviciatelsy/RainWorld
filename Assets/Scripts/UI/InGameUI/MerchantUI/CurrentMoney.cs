using System.Collections;
using TMPro;
using UnityEngine;

public class CurrentMoney : MonoBehaviour
{
    [Header("目标UI")]
    [SerializeField] private RectTransform moneyIconRect; // 左上角当前金币UI里的金币图标
    [SerializeField] private RectTransform effectRoot;    // 全屏特效层（建议是一个铺满Canvas的RectTransform）

    [Header("金币特效预制体")]
    [SerializeField] private MoneyFlyCoinUI moneyFlyCoinPrefab;

    [Header("生成数量设置")]
    [SerializeField] private bool spawnOneCoinPerMoney = true; // 是否1金币 = 1个图标
    [SerializeField] private int minVisualCoinCount = 3;       // 不1:1时的最少可视金币数
    [SerializeField] private int maxVisualCoinCount = 12;      // 不1:1时的最多可视金币数

    [Header("蹦出散布设置")]
    [SerializeField] private float scatterRadiusMin = 25f;     // 鼠标附近散布半径最小值
    [SerializeField] private float scatterRadiusMax = 90f;     // 鼠标附近散布半径最大值
    [SerializeField] private Vector2 popDurationRange = new Vector2(0.08f, 0.14f); // 蹦出时长范围
    [SerializeField] private Vector2 waitBeforeFlyRange = new Vector2(0.03f, 0.08f); // 蹦出后等待多久再飞
    [SerializeField] private float launchStagger = 0.012f;     // 每个金币额外错开一点起飞时间，让画面更有层次

    [Header("飞行设置")]
    [SerializeField] private Vector2 flyDurationRange = new Vector2(0.35f, 0.55f); // 飞行时长范围（越小越快）
    [SerializeField] private Vector2 arcHeightRange = new Vector2(80f, 160f);       // 弧线高度范围
    [SerializeField] private Vector2 startScaleRange = new Vector2(0.85f, 1.15f);   // 每个金币初始视觉大小随机范围
    [SerializeField] private Vector2 spinSpeedRange = new Vector2(-360f, 360f);     // 每个金币旋转速度范围

    [Header("目标UI反馈")]
    [SerializeField] private float iconPunchScale = 1.18f;     // 到达时金币图标放大多少
    [SerializeField] private float iconPunchDuration = 0.12f;  // 图标弹动时长
    [SerializeField] private float textPunchScale = 1.12f;     // 金币数字放大多少
    [SerializeField] private float textPunchDuration = 0.10f;  // 数字弹动时长

    private TextMeshProUGUI moneyText;
    private InventoryPlayer playerInventory;
    private Canvas rootCanvas;
    private RectTransform selfRect;

    private Vector3 moneyIconOriginalScale;
    private Vector3 moneyTextOriginalScale;

    private Coroutine moneyIconPunchCoroutine;
    private Coroutine moneyTextPunchCoroutine;

    private void Awake()
    {
        moneyText = GetComponent<TextMeshProUGUI>();
        selfRect = transform as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();

        if (moneyIconRect != null)
        {
            moneyIconOriginalScale = moneyIconRect.localScale;
        }

        if (selfRect != null)
        {
            moneyTextOriginalScale = selfRect.localScale;
        }
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
        {
            TrySubscribe(PlayerManager.Instance.TryGetCurrentPlayer());
            PlayerManager.Instance.OnPlayerRegistered += TrySubscribe;
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerRegistered -= TrySubscribe;
        }

        UnsubscribeCurrentPlayer();
    }

    private void TrySubscribe(Player player)
    {
        // 先把旧玩家的订阅解除，避免重复订阅
        UnsubscribeCurrentPlayer();

        if (player == null)
        {
            return;
        }

        playerInventory = player.GetComponent<InventoryPlayer>();

        if (playerInventory == null)
        {
            return;
        }

        playerInventory.onMoneyChanged += UpdateMoneyText;
        playerInventory.onMoneyAdd += PlayMoneyAddAnimation;

        moneyText.text = playerInventory.money.ToString();
    }

    private void UnsubscribeCurrentPlayer()
    {
        if (playerInventory != null)
        {
            playerInventory.onMoneyChanged -= UpdateMoneyText;
            playerInventory.onMoneyAdd -= PlayMoneyAddAnimation;
            playerInventory = null;
        }
    }

    private void UpdateMoneyText(int currentMoney)
    {
        if (moneyText == null)
        {
            return;
        }

        moneyText.text = currentMoney.ToString();
    }

    private void PlayMoneyAddAnimation(int addAmount)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (moneyFlyCoinPrefab == null || effectRoot == null || moneyIconRect == null)
        {
            return;
        }

        int visualCoinCount = GetVisualCoinCount(addAmount);

        // 起点：当前鼠标位置
        Vector2 startLocalPosition = ScreenToLocalInEffectRoot(Input.mousePosition);

        // 终点：左上角金币UI图标中心
        Vector2 targetLocalPosition = GetRectCenterLocalPositionInEffectRoot(moneyIconRect);

        for (int i = 0; i < visualCoinCount; i++)
        {
            MoneyFlyCoinUI coin = Instantiate(moneyFlyCoinPrefab, effectRoot);

            float scatterRadius = Random.Range(scatterRadiusMin, scatterRadiusMax);
            Vector2 scatterOffset = Random.insideUnitCircle * scatterRadius;

            float popDuration = Random.Range(popDurationRange.x, popDurationRange.y);
            float waitBeforeFly = Random.Range(waitBeforeFlyRange.x, waitBeforeFlyRange.y) + i * launchStagger;
            float flyDuration = Random.Range(flyDurationRange.x, flyDurationRange.y);
            float arcHeight = Random.Range(arcHeightRange.x, arcHeightRange.y);
            float startScale = Random.Range(startScaleRange.x, startScaleRange.y);
            float spinSpeed = Random.Range(spinSpeedRange.x, spinSpeedRange.y);

            coin.Play
            (
                startLocalPosition,
                startLocalPosition + scatterOffset,
                targetLocalPosition,
                popDuration,
                waitBeforeFly,
                flyDuration,
                arcHeight,
                startScale,
                spinSpeed,
                OnSingleCoinArriveTarget
            );
        }
    }

    private int GetVisualCoinCount(int addAmount)
    {
        if (addAmount <= 0)
        {
            return 0;
        }

        // 如果你真的希望“卖出几金币就飞几个金币图标”，勾上 spawnOneCoinPerMoney 即可
        if (spawnOneCoinPerMoney)
        {
            return addAmount;
        }

        // 如果不想大额时飞太多图标，就做一个视觉压缩
        if (addAmount <= 3)
        {
            return addAmount;
        }

        int visualCount = Mathf.CeilToInt(Mathf.Sqrt(addAmount) * 2.2f);
        return Mathf.Clamp(visualCount, minVisualCoinCount, maxVisualCoinCount);
    }

    private void OnSingleCoinArriveTarget()
    {
        // 每个金币抵达时，都让左上角UI小弹一下
        PlayPunchOnMoneyIcon();
        //PlayPunchOnMoneyText();
    }

    private void PlayPunchOnMoneyIcon()
    {
        if (moneyIconRect == null)
        {
            return;
        }

        if (moneyIconPunchCoroutine != null)
        {
            StopCoroutine(moneyIconPunchCoroutine);
        }

        moneyIconPunchCoroutine = StartCoroutine(PunchScaleRoutine
        (
            moneyIconRect,
            moneyIconOriginalScale,
            iconPunchScale,
            iconPunchDuration
        ));
    }

    private void PlayPunchOnMoneyText()
    {
        if (selfRect == null)
        {
            return;
        }

        if (moneyTextPunchCoroutine != null)
        {
            StopCoroutine(moneyTextPunchCoroutine);
        }

        moneyTextPunchCoroutine = StartCoroutine(PunchScaleRoutine
        (
            selfRect,
            moneyTextOriginalScale,
            textPunchScale,
            textPunchDuration
        ));
    }

    private IEnumerator PunchScaleRoutine(RectTransform target, Vector3 originalScale, float scaleMultiplier, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        float time = 0f;
        Vector3 punchScale = originalScale * scaleMultiplier;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            // 一个比较简洁的先放大再回落的曲线
            float curve = Mathf.Sin(t * Mathf.PI);
            target.localScale = Vector3.LerpUnclamped(originalScale, punchScale, curve);

            yield return null;
        }

        target.localScale = originalScale;
    }

    private Vector2 ScreenToLocalInEffectRoot(Vector2 screenPosition)
    {
 

        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            effectRoot,
            screenPosition,
            null,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector2 GetRectCenterLocalPositionInEffectRoot(RectTransform targetRect)
    {
        if (targetRect == null)
        {
            return Vector2.zero;
        }



        Vector3 worldCenter = targetRect.TransformPoint(targetRect.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);

        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            effectRoot,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        return localPoint;
    }

}