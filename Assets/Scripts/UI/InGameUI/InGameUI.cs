using System;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public DraggedItemUI draggedItemUI { get; private set; }
    public BackpackUI backpackUI { get; private set; }
    public LootUI lootUI { get; private set; }
    public RetrieveUI retrieveUI { get; private set; }
    public MapUI mapUI { get; private set; }
    public NoteBookUI notebookUI { get; private set; }
    public ItemToolTip itemToolTip { get; private set; }
    private bool backpackUIEnabled;
    private bool lootUIEnabled;
    private bool retrieveUIEnabled;
    private bool mapUIEnabled;
    private bool notebookUIEnabled;

    private bool canReturnByESC = true;
    private bool hasSubscribedArchiveManager = false;
    private MainInput mainInput;
    private void Awake()
    {
        mainInput=InputManager.Instance.mainInput;
        draggedItemUI = GetComponentInChildren<DraggedItemUI>(true);
        backpackUI = GetComponentInChildren<BackpackUI>(true);
        lootUI = GetComponentInChildren<LootUI>(true);
        retrieveUI = GetComponentInChildren<RetrieveUI>(true);
        mapUI = GetComponentInChildren<MapUI>(true);
        notebookUI=GetComponentInChildren<NoteBookUI>(true);
        backpackUIEnabled = backpackUI.gameObject.activeSelf;
        lootUIEnabled = lootUI.gameObject.activeSelf;
        retrieveUIEnabled = retrieveUI.gameObject.activeSelf;
        mapUIEnabled = mapUI.gameObject.activeSelf;
        notebookUIEnabled = notebookUI.gameObject.activeSelf;

        itemToolTip=GetComponentInChildren<ItemToolTip>(true);
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

        CloseAllPanelsBeforeOpening(InGamePanelType.NoteBook);

        notebookUIEnabled = true;
        notebookUI.OpenToUnlockedArchiveEntry(unlockRecord);

        HideToolTips();
    }

    private void HandleEscape()
    {
        if (!canReturnByESC) // 如果此刻禁用ESC返回
        {
            return;
        }

        if (notebookUIEnabled && notebookUI != null && notebookUI.IsBusy) // 如果正在自动翻页
        {
            return;
        }

        // 如果已经有面板开着：只关闭“当前最上层”的那个（按优先级）
        if (AnyPanelOpen())
        {
            //// 选项（最高优先级）
            //if (pauseUIEnabled)
            //{
            //    TogglePauseUI();
            //    return;
            //}
            
            if (backpackUIEnabled)
            {
                ToggleBackpackUI();
                return;
            }

            if(lootUIEnabled)
            {
                ToggleLootUI(null);
                return;
            }

            if (retrieveUIEnabled)
            {
                ToggleRetrieveUI(null);
                return;
            }
            if(mapUIEnabled)
            {
                ToggleMapUI();
                return;
            }
            if (notebookUIEnabled)
            {
                ToggleNoteBookUI();
                return;
            }
            return;
        }
        //if (!pauseUIEnabled)
        //{
        //    TogglePauseUI();
        //}
    }

    public void ToggleBackpackUI()
    {
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
        bool willOpen=!lootUIEnabled;
        if(willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Loot);
        }
        lootUIEnabled = willOpen;
        SwitchLootUI(lootUIEnabled,lootInventory);
        HideToolTips() ;
    }

    public void ToggleRetrieveUI(InventoryBase retrieveInventory)
    {
        bool willOpen=!retrieveUIEnabled;
        if(willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.Retrieve);
        }
        retrieveUIEnabled = willOpen;
        SwitchRetrieveUI(retrieveUIEnabled,retrieveInventory);
        HideToolTips();
    }

    public void ToggleMapUI()
    {
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

        bool willOpen = !notebookUIEnabled;

        if (willOpen)
        {
            CloseAllPanelsBeforeOpening(InGamePanelType.NoteBook);
        }

        notebookUIEnabled = willOpen;
        SwitchNoteBookUI(notebookUIEnabled);
        HideToolTips();
    }



    // 开新面板前，关闭其它所有已开启面板
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

        if(retrieveUIEnabled&&panelToOpen != InGamePanelType.Retrieve)
        {
            ToggleRetrieveUI(null);
        }

        if(mapUIEnabled&& panelToOpen != InGamePanelType.Map)
        {
            ToggleMapUI();
        }
        if(notebookUIEnabled&&panelToOpen!=InGamePanelType.NoteBook)
        {
            ToggleNoteBookUI();
        }
        // 统一收起各种 ToolTip
        HideToolTips();

    }

    private void SwitchBackpackUI(bool enabled)
    {
        if(enabled)
        {
            backpackUI.Open();
        }
        else
        {
            backpackUI.Close();
        }
    }

    private void SwitchLootUI(bool enabled,InventoryBase lootInventory)
    {
        if(enabled)
        {
            lootUI.Open(lootInventory);
        }
        else
        {
            lootUI.Close();
        }
    }

    private void SwitchRetrieveUI(bool enabled,InventoryBase retrieveInventory)
    {
        if(enabled)
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
        if(enabled)
        {
            notebookUI.Open();
        }
        else
        {
            notebookUI.Close();
        }
    }

    private void HideToolTips()
    {
        itemToolTip.HideItemToolTip();
    }

    private bool AnyPanelOpen()
    {
        return backpackUIEnabled
            ||lootUIEnabled
            ||retrieveUIEnabled
            ||mapUIEnabled
            ||notebookUIEnabled
            ;

    }
}

public enum InGamePanelType
{
    None,
    Backpack,
    Loot,
    Retrieve,
    Map,
    NoteBook
}