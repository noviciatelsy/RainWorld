using System;
using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance;

    [SerializeField] private CanvasGroup HudCanvasGroup;

    public DraggedItemUI draggedItemUI { get; private set; }
    public BackpackUI backpackUI { get; private set; }
    public LootUI lootUI { get; private set; }
    public RetrieveUI retrieveUI { get; private set; }
    public MapUI mapUI { get; private set; }
    public NoteBookUI notebookUI { get; private set; }

    public MerchantUI merchantUI { get; private set; }

    public IntelligencerUI intelligencerUI { get; private set; }
    public DialogueUI dialogueUI { get; private set; }
    public ItemToolTip itemToolTip { get; private set; }

    private bool backpackUIEnabled;
    private bool lootUIEnabled;
    private bool retrieveUIEnabled;
    private bool mapUIEnabled;
    private bool notebookUIEnabled;
    private bool merchantUIEnabled;
    private bool intelligencerUIEnabled;

    // 当前的 NoteBookUI 是不是以“二级覆盖面板”的方式打开的。
    private bool notebookUIOpenedAsSecondary = false;

    private bool canReturnByESC = true;
    private bool hasSubscribedArchiveManager = false;

    private MainInput mainInput;

    // 记录被二级面板盖住的主面板，它们原来的 blocksRaycasts 状态。
    // 关闭二级面板时再恢复。
    private readonly Dictionary<InGamePanelType, bool> coveredPanelOriginalBlocksRaycasts = new Dictionary<InGamePanelType, bool>();

    // 缓存各主面板根物体上的 CanvasGroup。
    // 二级面板打开时，会通过这些 CanvasGroup 禁用底下面板的鼠标交互。
    private readonly Dictionary<InGamePanelType, CanvasGroup> panelCanvasGroups = new Dictionary<InGamePanelType, CanvasGroup>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        mainInput = InputManager.Instance.mainInput;

        draggedItemUI = GetComponentInChildren<DraggedItemUI>(true);
        backpackUI = GetComponentInChildren<BackpackUI>(true);
        lootUI = GetComponentInChildren<LootUI>(true);
        retrieveUI = GetComponentInChildren<RetrieveUI>(true);
        mapUI = GetComponentInChildren<MapUI>(true);
        notebookUI = GetComponentInChildren<NoteBookUI>(true);
        merchantUI = GetComponentInChildren<MerchantUI>(true);
        intelligencerUI = GetComponentInChildren<IntelligencerUI>(true);
        dialogueUI = GetComponentInChildren<DialogueUI>(true);

        backpackUIEnabled = backpackUI.gameObject.activeSelf;
        lootUIEnabled = lootUI.gameObject.activeSelf;
        retrieveUIEnabled = retrieveUI.gameObject.activeSelf;
        mapUIEnabled = mapUI.gameObject.activeSelf;
        notebookUIEnabled = notebookUI.gameObject.activeSelf;
        merchantUIEnabled = merchantUI.gameObject.activeSelf;
        intelligencerUIEnabled = intelligencerUI.gameObject.activeSelf;

        itemToolTip = GetComponentInChildren<ItemToolTip>(true);

        CachePanelCanvasGroups();

        if (HudCanvasGroup != null)
        {
            HudCanvasGroup.alpha = 1;
        }
    }

    private void OnEnable()
    {
        if (mainInput != null)
        {
            mainInput.UI.CheckBackpack.performed += OnCheckBackpackPerformed;
            mainInput.UI.Map.performed += OnMapPerformed;
            mainInput.UI.NoteBook.performed += OnNoteBookPerformed;
            mainInput.UI.Escape.performed += OnEscapePerformed;
        }

        TrySubscribeArchiveManager();
    }

    private void Start()
    {
        TrySubscribeArchiveManager();
    }

    private void OnDisable()
    {
        if (mainInput != null)
        {
            mainInput.UI.CheckBackpack.performed -= OnCheckBackpackPerformed;
            mainInput.UI.Map.performed -= OnMapPerformed;
            mainInput.UI.NoteBook.performed -= OnNoteBookPerformed;
            mainInput.UI.Escape.performed -= OnEscapePerformed;
        }

        UnsubscribeArchiveManager();
    }

    private void CachePanelCanvasGroups()
    {
        panelCanvasGroups.Clear();

        RegisterPanelCanvasGroup(InGamePanelType.Backpack, backpackUI != null ? backpackUI.gameObject : null);
        RegisterPanelCanvasGroup(InGamePanelType.Loot, lootUI != null ? lootUI.gameObject : null);
        RegisterPanelCanvasGroup(InGamePanelType.Retrieve, retrieveUI != null ? retrieveUI.gameObject : null);
        RegisterPanelCanvasGroup(InGamePanelType.Map, mapUI != null ? mapUI.gameObject : null);
        RegisterPanelCanvasGroup(InGamePanelType.NoteBook, notebookUI != null ? notebookUI.gameObject : null);
        RegisterPanelCanvasGroup(InGamePanelType.Merchant, merchantUI != null ? merchantUI.gameObject : null);
        RegisterPanelCanvasGroup(InGamePanelType.Intelligencer, intelligencerUI != null ? intelligencerUI.gameObject : null);
    }

    private void RegisterPanelCanvasGroup(InGamePanelType panelType, GameObject panelObject)
    {
        if (panelObject == null)
        {
            return;
        }

        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = panelObject.AddComponent<CanvasGroup>();
        }

        panelCanvasGroups[panelType] = canvasGroup;
    }

    private void OnCheckBackpackPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        ToggleBackpackUI();
    }

    private void OnMapPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        ToggleMapUI();
    }

    private void OnNoteBookPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        ToggleNoteBookUI();
    }

    private void OnEscapePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        HandleEscape();
    }

    private void TrySubscribeArchiveManager()
    {
        if (hasSubscribedArchiveManager)
        {
            return;
        }

        if (IntelligenceArchiveManager.Instance == null)
        {
            return;
        }

        IntelligenceArchiveManager.Instance.OnArchiveEntryUnlocked += HandleArchiveEntryUnlocked;
        hasSubscribedArchiveManager = true;
    }

    private void UnsubscribeArchiveManager()
    {
        if (!hasSubscribedArchiveManager)
        {
            return;
        }

        if (IntelligenceArchiveManager.Instance != null)
        {
            IntelligenceArchiveManager.Instance.OnArchiveEntryUnlocked -= HandleArchiveEntryUnlocked;
        }

        hasSubscribedArchiveManager = false;
    }

    private void HandleArchiveEntryUnlocked(ArchiveUnlockRecord unlockRecord)
    {
        if (unlockRecord == null)
        {
            return;
        }

        if (notebookUI == null)
        {
            return;
        }

        // 情报解锁自动打开笔记本时，不再关闭当前主面板。
        // 它会作为二级面板盖在当前面板上方。
        OpenNoteBookAsSecondaryPanel(unlockRecord);

        HideToolTips();
    }

    private void HandleEscape()
    {
        if (!canReturnByESC) // 如果此刻禁用ESC返回
        {
            return;
        }

        // 二级面板优先级最高。
        // 如果自动打开的 NoteBookUI 正在翻页，不允许 ESC 关闭。
        if (notebookUIOpenedAsSecondary)
        {
            if (notebookUI != null && notebookUI.IsBusy)
            {
                return;
            }

            CloseSecondaryNoteBookPanel();
            return;
        }

        if (notebookUIEnabled && notebookUI != null && notebookUI.IsBusy) // 如果正在自动翻页
        {
            return;
        }

        // 如果已经有面板开着：只关闭“当前最上层”的那个（按优先级）
        if (AnyPanelOpen())
        {
            if (notebookUIEnabled)
            {
                ToggleNoteBookUI();
                return;
            }

            if (backpackUIEnabled)
            {
                ToggleBackpackUI();
                return;
            }

            if (lootUIEnabled)
            {
                ToggleLootUI(null);
                return;
            }

            if (retrieveUIEnabled)
            {
                ToggleRetrieveUI(null);
                return;
            }

            if (mapUIEnabled)
            {
                ToggleMapUI();
                return;
            }

            if (merchantUIEnabled)
            {
                ToggleMerchantUI();
                return;
            }

            if (intelligencerUIEnabled)
            {
                ToggleIntelligencerUI();
                return;
            }

            //// 选项（最高优先级）
            //if (pauseUIEnabled)
            //{
            //    TogglePauseUI();
            //    return;
            //}

            return;
        }

        //if (!pauseUIEnabled)
        //{
        //    TogglePauseUI();
        //}
    }

    public void ToggleBackpackUI()
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        bool willOpen = !backpackUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Backpack);
        }

        backpackUIEnabled = willOpen;

        SwitchBackpackUI(backpackUIEnabled);
        HideToolTips();
    }

    public void ToggleLootUI(InventoryBase lootInventory)
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        bool willOpen = !lootUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Loot);
        }

        lootUIEnabled = willOpen;

        SwitchLootUI(lootUIEnabled, lootInventory);
        HideToolTips();
    }

    public void ToggleRetrieveUI(InventoryBase retrieveInventory)
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        bool willOpen = !retrieveUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Retrieve);
        }

        retrieveUIEnabled = willOpen;

        SwitchRetrieveUI(retrieveUIEnabled, retrieveInventory);
        HideToolTips();
    }

    public void ToggleMapUI()
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        bool willOpen = !mapUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Map);
        }

        mapUIEnabled = willOpen;

        SwitchMapUI(mapUIEnabled);
        HideToolTips();
    }

    public void ToggleNoteBookUI()
    {
        if (notebookUI != null && notebookUI.IsBusy)
        {
            return;
        }

        // 如果笔记本当前是二级面板，按笔记本键或者关闭按钮时，
        // 应该只关闭二级笔记本，并恢复底下主面板的鼠标交互。
        if (notebookUIOpenedAsSecondary)
        {
            CloseSecondaryNoteBookPanel();
            return;
        }

        bool willOpen = !notebookUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.NoteBook);
        }

        notebookUIEnabled = willOpen;

        SwitchNoteBookUI(notebookUIEnabled);
        HideToolTips();
    }

    public void ToggleMerchantUI()
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        bool willOpen = !merchantUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Merchant);
        }

        merchantUIEnabled = willOpen;

        SwitchMerchantUI(merchantUIEnabled);
        HideToolTips();
    }

    public void ToggleIntelligencerUI()
    {
        if (AnySecondaryPanelOpen())
        {
            return;
        }

        bool willOpen = !intelligencerUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Intelligencer);
        }

        intelligencerUIEnabled = willOpen;

        SwitchIntelligencerUI(intelligencerUIEnabled);
        HideToolTips();
    }

    // 情报自动解锁时使用这个函数。
    // 它不会关闭当前主面板，只会把笔记本盖在上面。
    private void OpenNoteBookAsSecondaryPanel(ArchiveUnlockRecord unlockRecord)
    {
        if (notebookUI == null)
        {
            return;
        }

        // 如果笔记本已经作为普通主面板打开了，那就不需要再变成二级面板。
        // 直接让当前笔记本翻到新解锁条目所在页即可。
        if (notebookUIEnabled && !notebookUIOpenedAsSecondary)
        {
            notebookUI.OpenToUnlockedArchiveEntry(unlockRecord);
            return;
        }

        // 如果笔记本已经是二级面板了，直接刷新并重新定位到新条目。
        if (notebookUIOpenedAsSecondary)
        {
            notebookUI.OpenToUnlockedArchiveEntry(unlockRecord);
            return;
        }

        DisableBlocksRaycastsForOpenedPrimaryPanels();

        notebookUIOpenedAsSecondary = true;
        notebookUIEnabled = true;

        notebookUI.OpenToUnlockedArchiveEntry(unlockRecord);
    }

    private void CloseSecondaryNoteBookPanel()
    {
        if (!notebookUIOpenedAsSecondary)
        {
            return;
        }

        if (notebookUI != null && notebookUI.IsBusy)
        {
            return;
        }

        if (notebookUI != null)
        {
            notebookUI.Close();
        }

        notebookUIOpenedAsSecondary = false;
        notebookUIEnabled = false;

        RestoreBlocksRaycastsForCoveredPrimaryPanels();

        HideToolTips();
    }

    private bool AnySecondaryPanelOpen()
    {
        return notebookUIOpenedAsSecondary;
    }

    // 开新主面板前，关闭其它所有已开启主面板。
    // 注意：二级面板不应该走这个函数。
    private void CloseAllPanelsBeforeOpening(InGamePanelType panelToOpen)
    {
        if (backpackUIEnabled && panelToOpen != InGamePanelType.Backpack)
        {
            ToggleBackpackUI();
        }

        if (lootUIEnabled && panelToOpen != InGamePanelType.Loot)
        {
            ToggleLootUI(null);
        }

        if (retrieveUIEnabled && panelToOpen != InGamePanelType.Retrieve)
        {
            ToggleRetrieveUI(null);
        }

        if (mapUIEnabled && panelToOpen != InGamePanelType.Map)
        {
            ToggleMapUI();
        }

        if (notebookUIEnabled && !notebookUIOpenedAsSecondary && panelToOpen != InGamePanelType.NoteBook)
        {
            ToggleNoteBookUI();
        }

        if (merchantUIEnabled && panelToOpen != InGamePanelType.Merchant)
        {
            ToggleMerchantUI();
        }

        if (intelligencerUIEnabled && panelToOpen != InGamePanelType.Intelligencer)
        {
            ToggleIntelligencerUI();
        }

        // 统一收起各种 ToolTip
        HideToolTips();
    }

    private void DisableBlocksRaycastsForOpenedPrimaryPanels()
    {
        coveredPanelOriginalBlocksRaycasts.Clear();

        TryDisablePanelBlocksRaycasts(InGamePanelType.Backpack, backpackUIEnabled);
        TryDisablePanelBlocksRaycasts(InGamePanelType.Loot, lootUIEnabled);
        TryDisablePanelBlocksRaycasts(InGamePanelType.Retrieve, retrieveUIEnabled);
        TryDisablePanelBlocksRaycasts(InGamePanelType.Map, mapUIEnabled);
        TryDisablePanelBlocksRaycasts(InGamePanelType.Merchant, merchantUIEnabled);
        TryDisablePanelBlocksRaycasts(InGamePanelType.Intelligencer, intelligencerUIEnabled);

        // 如果未来你允许“普通主面板笔记本上面再盖另一个二级面板”，可以在这里处理 NoteBook。
        // 现在笔记本自己就是这个二级面板，所以不需要禁用它自己。
    }

    private void TryDisablePanelBlocksRaycasts(InGamePanelType panelType, bool panelEnabled)
    {
        if (!panelEnabled)
        {
            return;
        }

        if (!panelCanvasGroups.TryGetValue(panelType, out CanvasGroup canvasGroup))
        {
            return;
        }

        if (canvasGroup == null)
        {
            return;
        }

        if (!coveredPanelOriginalBlocksRaycasts.ContainsKey(panelType))
        {
            coveredPanelOriginalBlocksRaycasts.Add(panelType, canvasGroup.blocksRaycasts);
        }

        canvasGroup.blocksRaycasts = false;
    }

    private void RestoreBlocksRaycastsForCoveredPrimaryPanels()
    {
        foreach (KeyValuePair<InGamePanelType, bool> pair in coveredPanelOriginalBlocksRaycasts)
        {
            if (!panelCanvasGroups.TryGetValue(pair.Key, out CanvasGroup canvasGroup))
            {
                continue;
            }

            if (canvasGroup == null)
            {
                continue;
            }

            canvasGroup.blocksRaycasts = pair.Value;
        }

        coveredPanelOriginalBlocksRaycasts.Clear();
    }

    private void SwitchBackpackUI(bool enabled)
    {
        if (enabled)
        {
            backpackUI.Open();
        }
        else
        {
            backpackUI.Close();
        }
    }

    private void SwitchLootUI(bool enabled, InventoryBase lootInventory)
    {
        if (enabled)
        {
            lootUI.Open(lootInventory);
        }
        else
        {
            lootUI.Close();
        }
    }

    private void SwitchRetrieveUI(bool enabled, InventoryBase retrieveInventory)
    {
        if (enabled)
        {
            retrieveUI.Open(retrieveInventory);
        }
        else
        {
            retrieveUI.Close();
        }
    }

    private void SwitchMapUI(bool enabled)
    {
        mapUI.gameObject.SetActive(enabled);
    }

    private void SwitchNoteBookUI(bool enabled)
    {
        if (enabled)
        {
            notebookUI.Open();
        }
        else
        {
            notebookUI.Close();
        }
    }

    private void SwitchMerchantUI(bool enabled)
    {
        if (enabled)
        {
            merchantUI.Open();
            ShowHud(false);
        }
        else
        {
            merchantUI.Close();
            ShowHud(true);
        }
    }

    private void SwitchIntelligencerUI(bool enabled)
    {
        if (enabled)
        {
            intelligencerUI.Open();
            ShowHud(false);
        }
        else
        {
            intelligencerUI.Close();
            ShowHud(true);
        }
    }

    private void HideToolTips()
    {
        if (itemToolTip != null)
        {
            itemToolTip.HideItemToolTip();
        }
    }

    private bool AnyPanelOpen()
    {
        return backpackUIEnabled
            || lootUIEnabled
            || retrieveUIEnabled
            || mapUIEnabled
            || notebookUIEnabled
            || merchantUIEnabled
            || intelligencerUIEnabled;
    }

    public void ShowHud(bool show)
    {
        if (HudCanvasGroup == null)
        {
            return;
        }

        if (show)
        {
            HudCanvasGroup.alpha = 1;
        }
        else
        {
            HudCanvasGroup.alpha = 0;
        }
    }
}

public enum InGamePanelType
{
    None,
    Backpack,
    Loot,
    Retrieve,
    Map,
    NoteBook,
    Merchant,
    Intelligencer
}