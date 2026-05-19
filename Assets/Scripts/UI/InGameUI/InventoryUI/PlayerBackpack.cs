using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBackpack : MonoBehaviour
{
    private DraggedItemUI draggedItemUI;
    private MainInput mainInput;
    private InventoryPlayer playerInventory;

    private int heldQuickItemIndex = -1;

    public bool holdQuickItemIndex_1
    {
        get
        {
            return heldQuickItemIndex == 0;
        }
    }

    public bool holdQuickItemIndex_2
    {
        get
        {
            return heldQuickItemIndex == 1;
        }
    }

    public bool holdQuickItemIndex_3
    {
        get
        {
            return heldQuickItemIndex == 2;
        }
    }

    public bool holdQuickItemIndex_4
    {
        get
        {
            return heldQuickItemIndex == 3;
        }
    }

    private void Awake()
    {
        if (InputManager.Instance != null)
        {
            mainInput = InputManager.Instance.mainInput;
        }

        if (draggedItemUI == null)
        {
            InGameUI inGameUI = GetComponentInParent<InGameUI>();

            if (inGameUI != null)
            {
                draggedItemUI = inGameUI.draggedItemUI;
            }
        }
    }

    private void OnEnable()
    {
        SubscribeInput();
        SubscribeDraggedItemUI();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        UnsubscribeDraggedItemUI();
    }

    public void SetInventory(InventoryPlayer newPlayerInventory)
    {
        if (playerInventory == newPlayerInventory)
        {
            return;
        }

        if (playerInventory != null)
        {
            playerInventory.onInventoryChange -= HandlePlayerInventoryChanged;
        }

        playerInventory = newPlayerInventory;

        if (playerInventory != null)
        {
            playerInventory.onInventoryChange += HandlePlayerInventoryChanged;
            ValidateQuickItemsWithCurrentDrag();
        }
    }

    public int GetHeldQuickItemIndex()
    {
        return heldQuickItemIndex;
    }

    public bool SetQuickItem(InventoryItem itemToSet, int quickSlotIndex)
    {
        if (playerInventory == null)
        {
            return false;
        }

        return playerInventory.SetQuickItem(itemToSet, quickSlotIndex);
    }

    public bool TrySetQuickItemToHeldSlot(InventoryItem itemToSet)
    {
        if (heldQuickItemIndex < 0)
        {
            return false;
        }

        if (itemToSet == null || itemToSet.ItemData == null)
        {
            return false;
        }

        if (itemToSet.ItemData.itemType != ItemType.Active)
        {
            return false;
        }

        return SetQuickItem(itemToSet, heldQuickItemIndex);
    }

    private void HandlePlayerInventoryChanged()
    {
        ValidateQuickItemsWithCurrentDrag();
    }

    private void HandleBeginDraggingItem(InventoryItem item)
    {
        ValidateQuickItemsWithCurrentDrag();
    }

    private void HandleEndDraggingItem(InventoryItem item)
    {
        ValidateQuickItemsWithCurrentDrag();
    }

    private void ValidateQuickItemsWithCurrentDrag()
    {
        if (playerInventory == null)
        {
            return;
        }

        InventoryItem temporarilyAllowedItem = null;

        if (draggedItemUI != null && draggedItemUI.IsDragging)
        {
            temporarilyAllowedItem = draggedItemUI.draggedItem;
        }

        playerInventory.ValidateQuickItems(temporarilyAllowedItem);
        playerInventory.ValidateHoldingItem(temporarilyAllowedItem);
    }
    private void SubscribeDraggedItemUI()
    {
        if (draggedItemUI == null)
        {
            return;
        }

        draggedItemUI.OnBeginDraggingItem -= HandleBeginDraggingItem;
        draggedItemUI.OnEndDraggingItem -= HandleEndDraggingItem;

        draggedItemUI.OnBeginDraggingItem += HandleBeginDraggingItem;
        draggedItemUI.OnEndDraggingItem += HandleEndDraggingItem;
    }

    private void UnsubscribeDraggedItemUI()
    {
        if (draggedItemUI == null)
        {
            return;
        }

        draggedItemUI.OnBeginDraggingItem -= HandleBeginDraggingItem;
        draggedItemUI.OnEndDraggingItem -= HandleEndDraggingItem;
    }

    private void SubscribeInput()
    {
        if (mainInput == null)
        {
            return;
        }

        mainInput.UI.QuickItemSlotUI_1.performed += OnQuickItemSlot1Performed;
        mainInput.UI.QuickItemSlotUI_1.canceled += OnQuickItemSlot1Canceled;

        mainInput.UI.QuickItemSlotUI_2.performed += OnQuickItemSlot2Performed;
        mainInput.UI.QuickItemSlotUI_2.canceled += OnQuickItemSlot2Canceled;

        mainInput.UI.QuickItemSlotUI_3.performed += OnQuickItemSlot3Performed;
        mainInput.UI.QuickItemSlotUI_3.canceled += OnQuickItemSlot3Canceled;

        mainInput.UI.QuickItemSlotUI_4.performed += OnQuickItemSlot4Performed;
        mainInput.UI.QuickItemSlotUI_4.canceled += OnQuickItemSlot4Canceled;
    }

    private void UnsubscribeInput()
    {
        if (mainInput == null)
        {
            return;
        }

        mainInput.UI.QuickItemSlotUI_1.performed -= OnQuickItemSlot1Performed;
        mainInput.UI.QuickItemSlotUI_1.canceled -= OnQuickItemSlot1Canceled;

        mainInput.UI.QuickItemSlotUI_2.performed -= OnQuickItemSlot2Performed;
        mainInput.UI.QuickItemSlotUI_2.canceled -= OnQuickItemSlot2Canceled;

        mainInput.UI.QuickItemSlotUI_3.performed -= OnQuickItemSlot3Performed;
        mainInput.UI.QuickItemSlotUI_3.canceled -= OnQuickItemSlot3Canceled;

        mainInput.UI.QuickItemSlotUI_4.performed -= OnQuickItemSlot4Performed;
        mainInput.UI.QuickItemSlotUI_4.canceled -= OnQuickItemSlot4Canceled;
    }

    private void OnQuickItemSlot1Performed(InputAction.CallbackContext context)
    {
        heldQuickItemIndex = 0;
    }

    private void OnQuickItemSlot1Canceled(InputAction.CallbackContext context)
    {
        if (heldQuickItemIndex == 0)
        {
            heldQuickItemIndex = -1;
        }
    }

    private void OnQuickItemSlot2Performed(InputAction.CallbackContext context)
    {
        heldQuickItemIndex = 1;
    }

    private void OnQuickItemSlot2Canceled(InputAction.CallbackContext context)
    {
        if (heldQuickItemIndex == 1)
        {
            heldQuickItemIndex = -1;
        }
    }

    private void OnQuickItemSlot3Performed(InputAction.CallbackContext context)
    {
        heldQuickItemIndex = 2;
    }

    private void OnQuickItemSlot3Canceled(InputAction.CallbackContext context)
    {
        if (heldQuickItemIndex == 2)
        {
            heldQuickItemIndex = -1;
        }
    }

    private void OnQuickItemSlot4Performed(InputAction.CallbackContext context)
    {
        heldQuickItemIndex = 3;
    }

    private void OnQuickItemSlot4Canceled(InputAction.CallbackContext context)
    {
        if (heldQuickItemIndex == 3)
        {
            heldQuickItemIndex = -1;
        }
    }

    public bool TryToggleHoldingItem(InventoryItem itemToHold)
    {
        if (playerInventory == null)
        {
            return false;
        }

        return playerInventory.TryToggleHoldingItem(itemToHold);
    }
}