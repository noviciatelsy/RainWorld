using UnityEngine;

public class BackpackUI : MonoBehaviour
{
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    private PlayerBackpack playerBackpack;
    private QuickItemSlots quickItemSlots;
    private DraggedItemUI draggedItemUI;

    private InventoryPlayer playerInventory;

    public CanvasGroup canvasGroup {  get; private set; }
    private void Awake()
    {
        if (draggedItemUI == null)
        {
            InGameUI inGameUI = GetComponentInParent<InGameUI>();

            if (inGameUI != null)
            {
                draggedItemUI = inGameUI.draggedItemUI;
            }
        }
        playerBackpack = GetComponentInChildren<PlayerBackpack>();
        quickItemSlots = GetComponentInChildren<QuickItemSlots>();
        canvasGroup=GetComponent<CanvasGroup>();
    }

    public void Open()
    {
        Player currentPlayer = PlayerManager.Instance != null ? PlayerManager.Instance.CurrentPlayer : null;

        if (currentPlayer == null)
        {
            Debug.LogWarning("打开 LootUI 失败：当前没有玩家。");
            return;
        }

        playerInventory = currentPlayer.GetComponent<InventoryPlayer>();

        if (playerInventory == null)
        {
            Debug.LogWarning("打开 LootUI 失败：玩家身上没有 InventoryPlayer。");
            return;
        }
        gameObject.SetActive(true);
        playerInventoryGrid.SetInventory(playerInventory);
        playerBackpack.SetInventory(playerInventory);
        quickItemSlots.SetInventory(playerInventory);
        
    }

    public void Close()
    {
        if (draggedItemUI != null && draggedItemUI.IsDragging)
        {
            draggedItemUI.TryDropItem();
        }


        playerInventoryGrid.SetInventory(null);
        playerBackpack.SetInventory(null);
        quickItemSlots.SetInventory(null);
        playerInventory = null;
        gameObject.SetActive(false);
    }

}