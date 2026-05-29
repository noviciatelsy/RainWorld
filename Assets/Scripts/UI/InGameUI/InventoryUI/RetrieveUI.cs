using UnityEngine;

public class RetrieveUI : MonoBehaviour
{
    [Header("Grids")]
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    [SerializeField] private InventoryGridUI lostInventoryGrid;
    private PlayerBackpack playerBackpack;
    private QuickItemSlots quickItemSlots;

    [Header("Refs")]
    private DraggedItemUI draggedItemUI;

    private InventoryPlayer playerInventory;
    private InventoryBase currentLostInventory;
    public CanvasGroup canvasGroup;

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

    public void Open(InventoryBase lostInventory)
    {
        if (lostInventory == null)
        {
            Debug.LogWarning("打开 RetrieveUI 失败：lostInventory 为空。");
            return;
        }

        Player currentPlayer = PlayerManager.Instance != null ? PlayerManager.Instance.CurrentPlayer : null;

        if (currentPlayer == null)
        {
            Debug.LogWarning("打开 RetrieveUI 失败：当前没有玩家。");
            return;
        }

        playerInventory = currentPlayer.GetComponent<InventoryPlayer>();

        if (playerInventory == null)
        {
            Debug.LogWarning("打开 RetrieveUI 失败：玩家身上没有 InventoryPlayer。");
            return;
        }

        currentLostInventory = lostInventory;

        gameObject.SetActive(true);

        playerInventoryGrid.SetInventory(playerInventory);
        lostInventoryGrid.SetInventory(currentLostInventory);
        playerBackpack.SetInventory(playerInventory);
        quickItemSlots.SetInventory(playerInventory);
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

        if (lostInventoryGrid != null)
        {
            lostInventoryGrid.SetInventory(null);
        }
        playerBackpack.SetInventory(null);
        quickItemSlots.SetInventory(null);

        playerInventory = null;
        currentLostInventory = null;

        gameObject.SetActive(false);
    }
}