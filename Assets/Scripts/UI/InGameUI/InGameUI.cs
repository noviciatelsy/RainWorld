using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public DraggedItemUI draggedItemUI { get; private set; }
    public BackpackUI backpackUI { get; private set; }
    public LootUI lootUI { get; private set; }
    public RetrieveUI retrieveUI { get; private set; }

    private bool backpackUIEnabled;
    private bool lootUIEnabled;
    private bool retrieveUIEnabled;
    private MainInput mainInput;
    private void Awake()
    {
        mainInput=InputManager.Instance.mainInput;
        draggedItemUI = GetComponentInChildren<DraggedItemUI>(true);
        backpackUI = GetComponentInChildren<BackpackUI>(true);
        lootUI = GetComponentInChildren<LootUI>(true);
        retrieveUI = GetComponentInChildren<RetrieveUI>(true);

        backpackUIEnabled = backpackUI.gameObject.activeSelf;
        lootUIEnabled = lootUI.gameObject.activeSelf;
        retrieveUIEnabled = retrieveUI.gameObject.activeSelf;
    }

    private void OnEnable()
    {
        mainInput.UI.CheckBackpack.performed += ctx=> ToggleBackpackUI();
    }

    private void OnDisable()
    {
        mainInput.UI.CheckBackpack.performed -= ctx => ToggleBackpackUI();
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

    private void HideToolTips()
    {

    }
}

public enum InGamePanelType
{
    None,
    Backpack,
    Loot,
    Retrieve
}