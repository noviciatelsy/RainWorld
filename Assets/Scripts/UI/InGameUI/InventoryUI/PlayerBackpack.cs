using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBackpack : MonoBehaviour
{
    private MainInput mainInput;
    public bool holdQuickItemIndex_1 { get; private set; } = false;
    public bool holdQuickItemIndex_2 { get; private set; } = false;
    public bool holdQuickItemIndex_3 { get; private set; } = false;
    public bool holdQuickItemIndex_4 { get; private set; } = false;

    private InventoryPlayer playerInventory;

    private void Awake()
    {
        mainInput = InputManager.Instance.mainInput;
    }

    private void OnEnable()
    {
        mainInput.UI.QuickItemSlotUI_1.performed += ctx => holdQuickItemIndex_1 = true;
        mainInput.UI.QuickItemSlotUI_1.canceled += ctx => holdQuickItemIndex_1 = false;
        mainInput.UI.QuickItemSlotUI_2.performed += ctx => holdQuickItemIndex_2 = true;
        mainInput.UI.QuickItemSlotUI_2.canceled += ctx => holdQuickItemIndex_2 = false;
        mainInput.UI.QuickItemSlotUI_3.performed += ctx => holdQuickItemIndex_3 = true;
        mainInput.UI.QuickItemSlotUI_3.canceled += ctx => holdQuickItemIndex_3 = false;
        mainInput.UI.QuickItemSlotUI_4.performed += ctx => holdQuickItemIndex_4 = true;
        mainInput.UI.QuickItemSlotUI_4.canceled += ctx => holdQuickItemIndex_4 = false;
    }

    private void OnDisable()
    {
        mainInput.UI.QuickItemSlotUI_1.performed -= ctx => holdQuickItemIndex_1 = true;
        mainInput.UI.QuickItemSlotUI_1.canceled -= ctx => holdQuickItemIndex_1 = false;
        mainInput.UI.QuickItemSlotUI_2.performed -= ctx => holdQuickItemIndex_2 = true;
        mainInput.UI.QuickItemSlotUI_2.canceled -= ctx => holdQuickItemIndex_2 = false;
        mainInput.UI.QuickItemSlotUI_3.performed -= ctx => holdQuickItemIndex_3 = true;
        mainInput.UI.QuickItemSlotUI_3.canceled -= ctx => holdQuickItemIndex_3 = false;
        mainInput.UI.QuickItemSlotUI_4.performed -= ctx => holdQuickItemIndex_4 = true;
        mainInput.UI.QuickItemSlotUI_4.canceled -= ctx => holdQuickItemIndex_4 = false;
    }

    public void SetInventory(InventoryPlayer newPlayerInventory)
    {
        if (playerInventory == newPlayerInventory)
        {
            return;
        }
        playerInventory = newPlayerInventory;
    }

    public void SetQuickItem(InventoryItem itemToSet,int quickSlotIndex)
    {

    }
}
