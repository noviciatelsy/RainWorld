using UnityEngine;

public class MerchantUI : MonoBehaviour
{
    [Header("Grids")]
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    private PlayerBackpack playerBackpack;
    private QuickItemSlots quickItemSlots;

    [Header("Refs")]
    private DraggedItemUI draggedItemUI;
    private InventoryPlayer playerInventory;
    private GoodsShelfUI goodsShelfUI;

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

        playerBackpack = GetComponentInChildren<PlayerBackpack>(true);
        quickItemSlots = GetComponentInChildren<QuickItemSlots>();
        goodsShelfUI = GetComponentInChildren<GoodsShelfUI>(true);
        canvasGroup=GetComponent<CanvasGroup>();
    }

    public void Open()
    {
        Player currentPlayer = PlayerManager.Instance != null ? PlayerManager.Instance.CurrentPlayer : null;

        if (currentPlayer == null)
        {
            return;
        }

        playerInventory = currentPlayer.GetComponent<InventoryPlayer>();

        if (playerInventory == null)
        {
            return;
        }

        gameObject.SetActive(true);

        playerInventoryGrid.SetInventory(playerInventory);
        playerBackpack.SetInventory(playerInventory);
        quickItemSlots.SetInventory(playerInventory);
        goodsShelfUI.SetInventory(playerInventory);
    }

    public void Close()
    {
        if (draggedItemUI != null && draggedItemUI.IsDragging)
        {
            draggedItemUI.TryDropItem();
        }

        if (playerInventoryGrid != null)
        {
            playerInventoryGrid.SetInventory(null);
        }

        if (playerBackpack != null)
        {
            playerBackpack.SetInventory(null);
        }

        if (goodsShelfUI != null)
        {
            goodsShelfUI.SetInventory(null);
        }

        if(quickItemSlots != null)
        {
            quickItemSlots.SetInventory(null);  
        }

        playerInventory = null;

        gameObject.SetActive(false);
    }
}