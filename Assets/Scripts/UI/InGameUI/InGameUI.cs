using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance;

    [Header("CanvasGroup")]
    [SerializeField] private CanvasGroup HudCanvasGroup;
    [SerializeField] private CanvasGroup draggedItemCanvasGroup;

    [Header("Primary Panel CanvasGroups")]
    private CanvasGroup backpackCanvasGroup;
    private CanvasGroup lootCanvasGroup;
    private CanvasGroup retrieveCanvasGroup;
    private CanvasGroup mapCanvasGroup;
    private CanvasGroup merchantCanvasGroup;
    private CanvasGroup intelligencerCanvasGroup;

    [Header("Secondary Panel CanvasGroups")]
    private CanvasGroup noteBookCanvasGroup;

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

    private InGamePrimaryPanelType currentPrimaryPanel = InGamePrimaryPanelType.None;
    private InGameSecondaryPanelType currentSecondaryPanel = InGameSecondaryPanelType.None;

    private bool canReturnByESC = true;
    private bool hasSubscribedArchiveManager = false;

    private MainInput mainInput;

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
        itemToolTip = GetComponentInChildren<ItemToolTip>(true);

        AutoFillCanvasGroupsIfNeeded();
        InitializePanelStateFromHierarchy();

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

    private void AutoFillCanvasGroupsIfNeeded()
    {
        if (backpackCanvasGroup == null && backpackUI != null)
        {
            backpackCanvasGroup = backpackUI.GetComponent<CanvasGroup>();
        }

        if (lootCanvasGroup == null && lootUI != null)
        {
            lootCanvasGroup = lootUI.GetComponent<CanvasGroup>();
        }

        if (retrieveCanvasGroup == null && retrieveUI != null)
        {
            retrieveCanvasGroup = retrieveUI.GetComponent<CanvasGroup>();
        }

        if (mapCanvasGroup == null && mapUI != null)
        {
            mapCanvasGroup = mapUI.GetComponent<CanvasGroup>();
        }

        if (merchantCanvasGroup == null && merchantUI != null)
        {
            merchantCanvasGroup = merchantUI.GetComponent<CanvasGroup>();
        }

        if (intelligencerCanvasGroup == null && intelligencerUI != null)
        {
            intelligencerCanvasGroup = intelligencerUI.GetComponent<CanvasGroup>();
        }

        if (noteBookCanvasGroup == null && notebookUI != null)
        {
            noteBookCanvasGroup = notebookUI.GetComponent<CanvasGroup>();
        }
    }

    private void InitializePanelStateFromHierarchy()
    {
        currentPrimaryPanel = InGamePrimaryPanelType.None;
        currentSecondaryPanel = InGameSecondaryPanelType.None;

        if (backpackUI != null && backpackUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Backpack;
        }
        else if (lootUI != null && lootUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Loot;
        }
        else if (retrieveUI != null && retrieveUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Retrieve;
        }
        else if (mapUI != null && mapUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Map;
        }
        else if (merchantUI != null && merchantUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Merchant;
        }
        else if (intelligencerUI != null && intelligencerUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Intelligencer;
        }

        if (notebookUI != null && notebookUI.gameObject.activeSelf)
        {
            currentSecondaryPanel = InGameSecondaryPanelType.NoteBook;

            if (currentPrimaryPanel != InGamePrimaryPanelType.None)
            {
                SetPrimaryPanelBlocksRaycasts(currentPrimaryPanel, false);
            }
        }
    }

    private void OnCheckBackpackPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        ToggleBackpackUI();
    }

    private void OnMapPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
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

        OpenNoteBookToArchiveUnlockRecord(unlockRecord);

        HideToolTips();
    }

    private void HandleEscape()
    {
        if (!canReturnByESC)
        {
            return;
        }

        // 1. ESC 优先处理二级面板
        if (currentSecondaryPanel != InGameSecondaryPanelType.None)
        {
            if (IsCurrentSecondaryPanelBusy())
            {
                return;
            }

            CloseCurrentSecondaryPanel();
            return;
        }

        // 2. 其次处理 PauseUI 以外的一级面板
        if (currentPrimaryPanel != InGamePrimaryPanelType.None
            && currentPrimaryPanel != InGamePrimaryPanelType.Pause)
        {
            CloseCurrentPrimaryPanel();
            return;
        }

        // 3. 最后处理 PauseUI
        TogglePauseUI();
    }

    public void ToggleBackpackUI()
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Backpack);
    }

    public void ToggleLootUI(InventoryBase lootInventory)
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Loot, lootInventory);
    }

    public void ToggleRetrieveUI(InventoryBase retrieveInventory)
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Retrieve, retrieveInventory);
    }

    public void ToggleMapUI()
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Map);
    }

    public void ToggleMerchantUI()
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Merchant);
    }

    public void ToggleIntelligencerUI()
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Intelligencer);
    }

    public void ToggleNoteBookUI()
    {
        TryToggleSecondaryPanel(InGameSecondaryPanelType.NoteBook);
    }

    // PauseUI 现在还没有实装，所以先留一个空壳。
    public void TogglePauseUI()
    {
        //if (currentSecondaryPanel != InGameSecondaryPanelType.None)
        //{
        //    return;
        //}

        //if (currentPrimaryPanel == InGamePrimaryPanelType.Pause)
        //{
        //    CloseCurrentPrimaryPanel();
        //    return;
        //}

        //if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        //{
        //    return;
        //}

        //currentPrimaryPanel = InGamePrimaryPanelType.Pause;

        //// TODO：之后实装 PauseUI 时，在这里写：
        //// pauseUI.Open();

        //HideToolTips();
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Pause);
    }

    private bool TryTogglePrimaryPanel(InGamePrimaryPanelType targetPanel, InventoryBase inventory = null)
    {
        if (targetPanel == InGamePrimaryPanelType.None)
        {
            return false;
        }

        // 有二级面板打开时，不允许开启 / 切换一级面板。
        // 因为二级面板可能是独立打开，也可能盖在一级面板上方。
        if (currentSecondaryPanel != InGameSecondaryPanelType.None)
        {
            return false;
        }

        // 当前已经打开的就是这个一级面板，则本次 Toggle 表示关闭它。
        if (currentPrimaryPanel == targetPanel)
        {
            CloseCurrentPrimaryPanel();
            return true;
        }

        // 已经有其它一级面板时，拦截新一级面板开启。
        if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        {
            return false;
        }

        OpenPrimaryPanel(targetPanel, inventory);
        return true;
    }

    private bool TryToggleSecondaryPanel(InGameSecondaryPanelType targetPanel)
    {
        if (targetPanel == InGameSecondaryPanelType.None)
        {
            return false;
        }

        if (IsCurrentSecondaryPanelBusy())
        {
            return false;
        }

        // 当前已经打开的就是这个二级面板，则本次 Toggle 表示关闭它。
        if (currentSecondaryPanel == targetPanel)
        {
            CloseCurrentSecondaryPanel();
            return true;
        }

        // 已经有其它二级面板时，拦截新二级面板开启。
        if (currentSecondaryPanel != InGameSecondaryPanelType.None)
        {
            return false;
        }

        OpenSecondaryPanel(targetPanel);
        return true;
    }

    private void OpenPrimaryPanel(InGamePrimaryPanelType targetPanel, InventoryBase inventory = null)
    {
        currentPrimaryPanel = targetPanel;

        SetPrimaryPanelBlocksRaycasts(targetPanel, true);

        switch (targetPanel)
        {
            case InGamePrimaryPanelType.Backpack:
                SwitchBackpackUI(true);
                break;

            case InGamePrimaryPanelType.Loot:
                SwitchLootUI(true, inventory);
                break;

            case InGamePrimaryPanelType.Retrieve:
                SwitchRetrieveUI(true, inventory);
                break;

            case InGamePrimaryPanelType.Map:
                SwitchMapUI(true);
                break;

            case InGamePrimaryPanelType.Merchant:
                SwitchMerchantUI(true);
                break;

            case InGamePrimaryPanelType.Intelligencer:
                SwitchIntelligencerUI(true);
                break;

            case InGamePrimaryPanelType.Pause:
                // TODO：之后实装 PauseUI 时，在这里写 pauseUI.Open();
                break;
        }

        HideToolTips();
    }

    private void CloseCurrentPrimaryPanel()
    {
        if (currentPrimaryPanel == InGamePrimaryPanelType.None)
        {
            return;
        }

        InGamePrimaryPanelType panelToClose = currentPrimaryPanel;

        switch (panelToClose)
        {
            case InGamePrimaryPanelType.Backpack:
                SwitchBackpackUI(false);
                break;

            case InGamePrimaryPanelType.Loot:
                SwitchLootUI(false, null);
                break;

            case InGamePrimaryPanelType.Retrieve:
                SwitchRetrieveUI(false, null);
                break;

            case InGamePrimaryPanelType.Map:
                SwitchMapUI(false);
                break;

            case InGamePrimaryPanelType.Merchant:
                SwitchMerchantUI(false);
                break;

            case InGamePrimaryPanelType.Intelligencer:
                SwitchIntelligencerUI(false);
                break;

            case InGamePrimaryPanelType.Pause:
                // TODO：之后实装 PauseUI 时，在这里写 pauseUI.Close();
                break;
        }

        SetPrimaryPanelBlocksRaycasts(panelToClose, true);

        currentPrimaryPanel = InGamePrimaryPanelType.None;

        HideToolTips();
    }

    private void OpenSecondaryPanel(InGameSecondaryPanelType targetPanel)
    {
        currentSecondaryPanel = targetPanel;

        // 如果当前有一级面板，则让下方一级面板看得见但点不到。
        if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        {
            SetPrimaryPanelBlocksRaycasts(currentPrimaryPanel, false);
        }

        switch (targetPanel)
        {
            case InGameSecondaryPanelType.NoteBook:
                SwitchNoteBookUI(true);
                break;
        }

        HideToolTips();
    }

    private void CloseCurrentSecondaryPanel()
    {
        if (currentSecondaryPanel == InGameSecondaryPanelType.None)
        {
            return;
        }

        if (IsCurrentSecondaryPanelBusy())
        {
            return;
        }

        InGameSecondaryPanelType panelToClose = currentSecondaryPanel;

        switch (panelToClose)
        {
            case InGameSecondaryPanelType.NoteBook:
                SwitchNoteBookUI(false);
                break;
        }

        currentSecondaryPanel = InGameSecondaryPanelType.None;

        // 如果下方还有一级面板，恢复它的点击。
        if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        {
            SetPrimaryPanelBlocksRaycasts(currentPrimaryPanel, true);
        }

        HideToolTips();
    }

    private void OpenNoteBookToArchiveUnlockRecord(ArchiveUnlockRecord unlockRecord)
    {
        if (notebookUI == null)
        {
            return;
        }

        // 如果其它二级面板已经打开，拦截
        if (currentSecondaryPanel != InGameSecondaryPanelType.None
            && currentSecondaryPanel != InGameSecondaryPanelType.NoteBook)
        {
            return;
        }

        // 如果 NoteBook 已经打开，只需要重新定位到新增条目。
        if (currentSecondaryPanel == InGameSecondaryPanelType.NoteBook)
        {
            notebookUI.OpenToUnlockedArchiveEntry(unlockRecord);
            return;
        }

        currentSecondaryPanel = InGameSecondaryPanelType.NoteBook;

        if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        {
            SetPrimaryPanelBlocksRaycasts(currentPrimaryPanel, false);
        }

        notebookUI.OpenToUnlockedArchiveEntry(unlockRecord);
    }

    private bool IsCurrentSecondaryPanelBusy()
    {
        if (currentSecondaryPanel == InGameSecondaryPanelType.NoteBook)
        {
            return notebookUI != null && notebookUI.IsBusy;
        }

        return false;
    }

    private void SetPrimaryPanelBlocksRaycasts(InGamePrimaryPanelType panelType, bool blocksRaycasts)
    {
        CanvasGroup targetCanvasGroup = GetPrimaryPanelCanvasGroup(panelType);

        if (targetCanvasGroup == null)
        {
            return;
        }

        targetCanvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private CanvasGroup GetPrimaryPanelCanvasGroup(InGamePrimaryPanelType panelType)
    {
        switch (panelType)
        {
            case InGamePrimaryPanelType.Backpack:
                return backpackCanvasGroup;

            case InGamePrimaryPanelType.Loot:
                return lootCanvasGroup;

            case InGamePrimaryPanelType.Retrieve:
                return retrieveCanvasGroup;

            case InGamePrimaryPanelType.Map:
                return mapCanvasGroup;

            case InGamePrimaryPanelType.Merchant:
                return merchantCanvasGroup;

            case InGamePrimaryPanelType.Intelligencer:
                return intelligencerCanvasGroup;

            case InGamePrimaryPanelType.Pause:
                // TODO：之后实装 PauseUI 后，给 PauseUI 也加 CanvasGroup 并在这里返回。
                return null;
        }

        return null;
    }

    private void SwitchBackpackUI(bool enabled)
    {
        if (backpackUI == null)
        {
            return;
        }

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
        if (lootUI == null)
        {
            return;
        }

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
        if (retrieveUI == null)
        {
            return;
        }

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
        if (mapUI == null)
        {
            return;
        }

        mapUI.gameObject.SetActive(enabled);
    }

    private void SwitchNoteBookUI(bool enabled)
    {
        if (notebookUI == null)
        {
            return;
        }

        if (enabled)
        {
            notebookUI.Open();
            ShowDraggedItem(false);
        }
        else
        {
            notebookUI.Close();
            ShowDraggedItem(true);
        }
    }

    private void SwitchMerchantUI(bool enabled)
    {
        if (merchantUI == null)
        {
            return;
        }

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
        if (intelligencerUI == null)
        {
            return;
        }

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

    public bool HasPrimaryPanelOpen()
    {
        return currentPrimaryPanel != InGamePrimaryPanelType.None;
    }

    public bool HasSecondaryPanelOpen()
    {
        return currentSecondaryPanel != InGameSecondaryPanelType.None;
    }

    public InGamePrimaryPanelType GetCurrentPrimaryPanel()
    {
        return currentPrimaryPanel;
    }

    public InGameSecondaryPanelType GetCurrentSecondaryPanel()
    {
        return currentSecondaryPanel;
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

    public void ShowDraggedItem(bool show)
    {
        if(!show)
        {
            draggedItemCanvasGroup.alpha = 1;
        }
        else
        {
            draggedItemCanvasGroup.alpha = 0;
        }
    }
}

public enum InGamePrimaryPanelType
{
    None,
    Backpack,
    Loot,
    Retrieve,
    Map,
    Merchant,
    Intelligencer,
    Pause
}

public enum InGameSecondaryPanelType
{
    None,
    NoteBook
}