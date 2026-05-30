using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntelligencerUI : MonoBehaviour
{
    private enum IntelligencerPanelMode
    {
        Exchange,
        Purchase
    }

    [Header("Data")]
    [SerializeField] private IntelligenceExchangeDataBaseSO intelligenceExchangeDataBase;

    [Header("Panel Roots")]
    [SerializeField] private GameObject intelligenceExchangeUI;
    [SerializeField] private GameObject intelligencePurchaseUI;

    [Header("Switch Buttons")]
    [SerializeField] private GameObject switchToIntelligenceExchangeDecoration;
    [SerializeField] private GameObject switchToIntelligencePurchaseDecoration;

    [SerializeField] private Button switchToIntelligenceExchangeButton;
    [SerializeField] private Button switchToIntelligencePurchaseButton;

    [Header("Exchange List")]
    [SerializeField] private Transform intelligenceExchangeContentRoot;
    [SerializeField] private IntelligenceToExchangeUI intelligenceToExchangePrefab;

    [Header("Purchase List")]
    [SerializeField] private Transform intelligencePurchaseContentRoot;
    [SerializeField] private IntelligenceToPurchaseUI intelligenceToPurchasePrefab;

    [Header("Enemy Picture Settings")]
    [SerializeField] private Sprite unknownEnemySprite;
    [SerializeField] private Sprite lockedEnemyPictureSprite;

    [Header("Purchase Debug")]
    [Tooltip("仅用于编辑器测试。开启后，不检查金币也允许购买。")]
    [SerializeField] private bool debugAllowPurchaseWithoutMoneyCheck = false;

    private IntelligencerPanelMode currentMode = IntelligencerPanelMode.Exchange;

    private readonly List<GameObject> runtimeExchangeRows = new List<GameObject>();
    private readonly List<GameObject> runtimePurchaseRows = new List<GameObject>();

    public CanvasGroup canvasGroup {  get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void Open()
    {
        gameObject.SetActive(true);

        ShowExchangePanel();
    }

    public void Close()
    {
        ClearExchangeRows();
        ClearPurchaseRows();

        gameObject.SetActive(false);
    }

    public void ShowExchangePanel()
    {
        currentMode = IntelligencerPanelMode.Exchange;

        SetPanelModeVisual();
        RebuildExchangeList();
    }

    public void ShowPurchasePanel()
    {
        currentMode = IntelligencerPanelMode.Purchase;

        SetPanelModeVisual();
        RebuildPurchaseList();
    }

    private void SetPanelModeVisual()
    {
        bool isExchangeMode = currentMode == IntelligencerPanelMode.Exchange;
        bool isPurchaseMode = currentMode == IntelligencerPanelMode.Purchase;

        SafeSetActive(intelligenceExchangeUI, isExchangeMode);
        SafeSetActive(intelligencePurchaseUI, isPurchaseMode);

        // 当前页签显示“装饰版”，另一个页签显示“可点击按钮版”
        SafeSetActive(switchToIntelligenceExchangeDecoration, isExchangeMode);
        SafeSetActive(switchToIntelligencePurchaseDecoration, isPurchaseMode);

        if (switchToIntelligenceExchangeButton != null)
        {
            SafeSetActive(switchToIntelligenceExchangeButton.gameObject, !isExchangeMode);
        }

        if (switchToIntelligencePurchaseButton != null)
        {
            SafeSetActive(switchToIntelligencePurchaseButton.gameObject, !isPurchaseMode);
        }
    }

    private void RebuildExchangeList()
    {
        ClearExchangeRows();

        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null)
        {
            Debug.LogWarning("IntelligencerUI 构建交换列表失败：场景中没有 IntelligenceArchiveManager。");
            return;
        }

        if (intelligenceExchangeDataBase == null)
        {
            Debug.LogWarning("IntelligencerUI 构建交换列表失败：没有配置 IntelligenceExchangeDataBaseSO。");
            return;
        }

        if (intelligenceToExchangePrefab == null || intelligenceExchangeContentRoot == null)
        {
            Debug.LogWarning("IntelligencerUI 构建交换列表失败：交换条目预制体或 ContentRoot 没有配置。");
            return;
        }

        List<IntelligenceExchangeDataSO> exchangeList = intelligenceExchangeDataBase.GetAllExchangeData();

        exchangeList.RemoveAll(item => !archiveManager.ShouldShowExchangeData(item));

        // 已经满足交换条件的排在最上面
        exchangeList.Sort((a, b) =>
        {
            bool canExchangeA = archiveManager.CanExchangeImportantIntelligence(a);
            bool canExchangeB = archiveManager.CanExchangeImportantIntelligence(b);

            int canExchangeCompare = canExchangeB.CompareTo(canExchangeA);

            if (canExchangeCompare != 0)
            {
                return canExchangeCompare;
            }

            string enemyNameA = a.requiredEnemyInformationData != null ? a.requiredEnemyInformationData.enemyName : string.Empty;
            string enemyNameB = b.requiredEnemyInformationData != null ? b.requiredEnemyInformationData.enemyName : string.Empty;

            return string.Compare(enemyNameA, enemyNameB, System.StringComparison.Ordinal);
        });

        for (int i = 0; i < exchangeList.Count; i++)
        {
            IntelligenceExchangeDataSO exchangeData = exchangeList[i];

            IntelligenceToExchangeUI row = Instantiate(intelligenceToExchangePrefab, intelligenceExchangeContentRoot);
            row.gameObject.SetActive(true);

            row.Setup(
                exchangeData,
                archiveManager,
                unknownEnemySprite,
                lockedEnemyPictureSprite,
                HandleExchangeRowClicked
            );

            runtimeExchangeRows.Add(row.gameObject);
        }
    }

    private void RebuildPurchaseList()
    {
        ClearPurchaseRows();

        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null)
        {
            Debug.LogWarning("IntelligencerUI 构建购买列表失败：场景中没有 IntelligenceArchiveManager。");
            return;
        }

        if (intelligenceToPurchasePrefab == null || intelligencePurchaseContentRoot == null)
        {
            Debug.LogWarning("IntelligencerUI 构建购买列表失败：购买条目预制体或 ContentRoot 没有配置。");
            return;
        }

        List<ArchivePurchaseOffer> purchaseOffers = archiveManager.GetPurchasableIntelligenceOffers();

        for (int i = 0; i < purchaseOffers.Count; i++)
        {
            ArchivePurchaseOffer purchaseOffer = purchaseOffers[i];

            IntelligenceToPurchaseUI row = Instantiate(intelligenceToPurchasePrefab, intelligencePurchaseContentRoot);
            row.gameObject.SetActive(true);

            row.Setup(purchaseOffer, HandlePurchaseRowClicked);

            runtimePurchaseRows.Add(row.gameObject);
        }
    }

    private void HandleExchangeRowClicked(IntelligenceExchangeDataSO exchangeData)
    {
        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null || exchangeData == null)
        {
            return;
        }

        bool exchanged = archiveManager.TryExchangeImportantIntelligence(exchangeData);

        if (!exchanged)
        {
            return;
        }

        // 交换成功后刷新交换列表。
        RebuildExchangeList();
    }

    private void HandlePurchaseRowClicked(ArchivePurchaseOffer purchaseOffer)
    {
        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null || purchaseOffer == null)
        {
            return;
        }

        // 防止 UI 列表没刷新时重复买同一个
        if (!archiveManager.IsPurchaseOfferStillAvailable(purchaseOffer))
        {
            RebuildPurchaseList();
            return;
        }

        bool paid = TryPayForIntelligence(purchaseOffer.Price);

        if (!paid)
        {

            return;
        }

        bool unlocked = UnlockPurchasedIntelligence(archiveManager, purchaseOffer);

        if (!unlocked)
        {
            return;
        }

        // 购买成功后刷新购买列表。
        RebuildPurchaseList();
    }

    private bool UnlockPurchasedIntelligence(IntelligenceArchiveManager archiveManager, ArchivePurchaseOffer purchaseOffer)
    {
        if (purchaseOffer.offerType == ArchivePurchaseOfferType.NormalIntelligence)
        {
            return archiveManager.UnlockIntelligence(purchaseOffer.intelligenceData);
        }

        return archiveManager.UnlockEnemyIntelligence(
            purchaseOffer.enemyInformationData,
            purchaseOffer.enemyIntelligenceData
        );
    }

    private bool TryPayForIntelligence(int price)
    {
#if UNITY_EDITOR
        if (debugAllowPurchaseWithoutMoneyCheck)
        {
            return true;
        }
#endif

        InventoryPlayer playerInventory = PlayerManager.Instance.TryGetCurrentPlayer().GetComponent<InventoryPlayer>();
        if (playerInventory != null)
        {
            if (playerInventory.MoneyCanAfford(price))
            {
                playerInventory.ReduceMoney(price);
                return true;
            }
            return false;
        }
        return false;
    }

    private void ClearExchangeRows()
    {
        for (int i = 0; i < runtimeExchangeRows.Count; i++)
        {
            if (runtimeExchangeRows[i] != null)
            {
                Destroy(runtimeExchangeRows[i]);
            }
        }

        runtimeExchangeRows.Clear();
    }

    private void ClearPurchaseRows()
    {
        for (int i = 0; i < runtimePurchaseRows.Count; i++)
        {
            if (runtimePurchaseRows[i] != null)
            {
                Destroy(runtimePurchaseRows[i]);
            }
        }

        runtimePurchaseRows.Clear();
    }

    private void SafeSetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void OnDestroy()
    {
        if (switchToIntelligenceExchangeButton != null)
        {
            switchToIntelligenceExchangeButton.onClick.RemoveListener(ShowExchangePanel);
        }

        if (switchToIntelligencePurchaseButton != null)
        {
            switchToIntelligencePurchaseButton.onClick.RemoveListener(ShowPurchasePanel);
        }
    }
}