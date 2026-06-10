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
    private CanvasGroup storageCanvasGroup;
    private CanvasGroup retrieveCanvasGroup;
    private CanvasGroup mapCanvasGroup;
    private CanvasGroup merchantCanvasGroup;
    private CanvasGroup intelligencerCanvasGroup;

    [Header("Secondary Panel CanvasGroups")]
    private CanvasGroup noteBookCanvasGroup;

    [Header("Special Panel CanvasGroups")]
    private CanvasGroup pauseCanvasGroup;

    public DraggedItemUI draggedItemUI { get; private set; }
    public BackpackUI backpackUI { get; private set; }
    public LootUI lootUI { get; private set; }
    public StorageUI storageUI { get; private set; }
    public RetrieveUI retrieveUI { get; private set; }
    public MapUI mapUI { get; private set; }
    public NoteBookUI notebookUI { get; private set; }
    public MerchantUI merchantUI { get; private set; }
    public IntelligencerUI intelligencerUI { get; private set; }
    public PauseUI pauseUI { get; private set; }
    public DialogueUI dialogueUI { get; private set; }
    public ItemToolTip itemToolTip { get; private set; }

    public DarknessMaskController darknessMaskController { get; private set; }

    private InGamePrimaryPanelType currentPrimaryPanel = InGamePrimaryPanelType.None;
    private InGameSecondaryPanelType currentSecondaryPanel = InGameSecondaryPanelType.None;

    // PauseUI ��������壬���Ž�һ��/����ö����
    private bool pauseUIEnabled = false;

    private bool hasSubscribedArchiveManager = false;

    private MainInput mainInput;
    private InventoryBase retrieveInventory;
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
        storageUI = GetComponentInChildren<StorageUI>(true);
        retrieveUI = GetComponentInChildren<RetrieveUI>(true);
        mapUI = GetComponentInChildren<MapUI>(true);
        notebookUI = GetComponentInChildren<NoteBookUI>(true);
        merchantUI = GetComponentInChildren<MerchantUI>(true);
        intelligencerUI = GetComponentInChildren<IntelligencerUI>(true);
        pauseUI = GetComponentInChildren<PauseUI>(true);
        dialogueUI = GetComponentInChildren<DialogueUI>(true);
        itemToolTip = GetComponentInChildren<ItemToolTip>(true);
        darknessMaskController=GetComponentInChildren<DarknessMaskController>(true);

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

        if(storageCanvasGroup == null && storageUI != null)
        {
            storageCanvasGroup = storageUI.GetComponent<CanvasGroup>();
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

        if (pauseCanvasGroup == null && pauseUI != null)
        {
            pauseCanvasGroup = pauseUI.GetComponent<CanvasGroup>();
        }
    }

    private void InitializePanelStateFromHierarchy()
    {
        currentPrimaryPanel = InGamePrimaryPanelType.None;
        currentSecondaryPanel = InGameSecondaryPanelType.None;
        pauseUIEnabled = false;

        // PauseUI ������㡣
        // �����ʼ״̬ PauseUI ���ţ����� PauseUI Ϊ������ȼ���
        if (pauseUI != null && pauseUI.gameObject.activeSelf)
        {
            pauseUIEnabled = true;
            return;
        }

        if (backpackUI != null && backpackUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Backpack;
        }
        else if (lootUI != null && lootUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Loot;
        }
        else if(storageUI!=null&&storageUI.gameObject.activeSelf)
        {
            currentPrimaryPanel = InGamePrimaryPanelType.Storage;
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

        // PauseUI ��ʱ����������Ϊ�鱨�����Զ��򿪱ʼǱ���
        if (pauseUIEnabled)
        {
            return;
        }

        OpenNoteBookToArchiveUnlockRecord(unlockRecord);

        HideToolTips();
    }

    private void HandleEscape()
    {

        // 1. ���ȴ����������
        if (currentSecondaryPanel != InGameSecondaryPanelType.None)
        {
            if (IsCurrentSecondaryPanelBusy())
            {
                return;
            }

            CloseCurrentSecondaryPanel();
            return;
        }

        // 2. ��δ���һ�����
        if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        {
            CloseCurrentPrimaryPanel();
            return;
        }

        // 3. ����� PauseUI
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

    public void ToggleStorageUI(InventoryBase storageInventory)
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Storage, storageInventory);
    }

    public void ToggleRetrieveUI(InventoryBase retrieveInventory)
    {
        TryTogglePrimaryPanel(InGamePrimaryPanelType.Retrieve, retrieveInventory);
        this.retrieveInventory = retrieveInventory;
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

    public void TogglePauseUI()
    {
        // PauseUI ����ʱ���ٴ� Toggle ���ǹر��Լ���
        if (pauseUIEnabled)
        {
            ClosePausePanel();
            return;
        }

        // ֻҪһ���������廹���ţ��Ͳ������� PauseUI��
        if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        {
            return;
        }

        if (currentSecondaryPanel != InGameSecondaryPanelType.None)
        {
            return;
        }

        OpenPausePanel();
    }

    private bool TryTogglePrimaryPanel(InGamePrimaryPanelType targetPanel, InventoryBase inventory = null)
    {
        if (targetPanel == InGamePrimaryPanelType.None)
        {
            return false;
        }

        // PauseUI ����ʱ�������������κ�һ����塣
        if (pauseUIEnabled)
        {
            return false;
        }

        // �������������������߸���һ������Ϸ�ʱ�������������� / �л�һ����塣
        if (currentSecondaryPanel != InGameSecondaryPanelType.None)
        {
            return false;
        }

        // ��ǰ�Ѿ��򿪵ľ������һ����壬�򱾴� Toggle ��ʾ�ر�����
        if (currentPrimaryPanel == targetPanel)
        {
            CloseCurrentPrimaryPanel();
            return true;
        }

        // �Ѿ�������һ�����ʱ��������һ����忪����
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

        // PauseUI ����ʱ�������������κζ�����塣
        if (pauseUIEnabled)
        {
            return false;
        }

        if (IsCurrentSecondaryPanelBusy())
        {
            return false;
        }

        // ��ǰ�Ѿ��򿪵ľ������������壬�򱾴� Toggle ��ʾ�ر�����
        if (currentSecondaryPanel == targetPanel)
        {
            CloseCurrentSecondaryPanel();
            return true;
        }

        // �Ѿ��������������ʱ�������¶�����忪����
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

            case InGamePrimaryPanelType.Storage:
                SwitchStorageUI(true,inventory);
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

            case InGamePrimaryPanelType.Storage:
                SwitchStorageUI(false, null);
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
        }

        SetPrimaryPanelBlocksRaycasts(panelToClose, true);

        currentPrimaryPanel = InGamePrimaryPanelType.None;

        HideToolTips();
    }

    private void OpenSecondaryPanel(InGameSecondaryPanelType targetPanel)
    {
        currentSecondaryPanel = targetPanel;

        // �����ǰ��һ����壬�����·�һ����忴�ü����㲻����
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

        // ����·�����һ����壬�ָ����ĵ����
        if (currentPrimaryPanel != InGamePrimaryPanelType.None)
        {
            SetPrimaryPanelBlocksRaycasts(currentPrimaryPanel, true);
        }

        HideToolTips();
    }

    private void OpenPausePanel()
    {
        pauseUIEnabled = true;

        SwitchPauseUI(true);

        HideToolTips();
    }

    private void ClosePausePanel()
    {
        pauseUIEnabled = false;

        SwitchPauseUI(false);

        HideToolTips();
    }

    private void OpenNoteBookToArchiveUnlockRecord(ArchiveUnlockRecord unlockRecord)
    {
        if (notebookUI == null)
        {
            return;
        }

        if (pauseUIEnabled)
        {
            return;
        }

        // ���������������Ѿ��򿪣����ء�
        if (currentSecondaryPanel != InGameSecondaryPanelType.None
            && currentSecondaryPanel != InGameSecondaryPanelType.NoteBook)
        {
            return;
        }

        // ��� NoteBook �Ѿ��򿪣�ֻ��Ҫ���¶�λ��������Ŀ��
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
        ShowDraggedItem(false);
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

            case InGamePrimaryPanelType.Storage:
                return storageCanvasGroup;

            case InGamePrimaryPanelType.Retrieve:
                return retrieveCanvasGroup;

            case InGamePrimaryPanelType.Map:
                return mapCanvasGroup;

            case InGamePrimaryPanelType.Merchant:
                return merchantCanvasGroup;

            case InGamePrimaryPanelType.Intelligencer:
                return intelligencerCanvasGroup;
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

    private void SwitchStorageUI(bool enabled,InventoryBase storageInventory)
    {
        if (storageUI == null)
        {
            return ;
        }
        if (enabled)
        {
            storageUI.Open(storageInventory);
        }
        else
        {
            storageUI.Close();
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
            this.retrieveInventory.SaveData();
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

    private void SwitchPauseUI(bool enabled)
    {
        if (pauseUI == null)
        {
            return;
        }

        if (enabled)
        {
            pauseUI.Open();
        }
        else
        {
            pauseUI.Close();
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

    public bool IsPauseUIOpen()
    {
        return pauseUIEnabled;
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
        if (draggedItemCanvasGroup == null)
        {
            return;
        }

        if (show)
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
    Storage,
    Retrieve,
    Map,
    Merchant,
    Intelligencer
}

public enum InGameSecondaryPanelType
{
    None,
    NoteBook
}