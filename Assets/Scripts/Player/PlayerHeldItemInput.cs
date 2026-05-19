using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeldItemInput : MonoBehaviour
{
    private MainInput mainInput;
    private InventoryPlayer inventoryPlayer;

    private void Awake()
    {
        inventoryPlayer = GetComponent<InventoryPlayer>();

        if (InputManager.Instance != null)
        {
            mainInput = InputManager.Instance.mainInput;
        }
    }

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void SubscribeInput()
    {
        if (mainInput == null)
        {
            return;
        }

        mainInput.Player.EquipQuickItem_1.performed += OnEquipQuickItem1Performed;
        mainInput.Player.EquipQuickItem_2.performed += OnEquipQuickItem2Performed;
        mainInput.Player.EquipQuickItem_3.performed += OnEquipQuickItem3Performed;
        mainInput.Player.EquipQuickItem_4.performed += OnEquipQuickItem4Performed;

        mainInput.Player.UseHoldingItem.performed += OnUseHoldingItemPerformed;
    }

    private void UnsubscribeInput()
    {
        if (mainInput == null)
        {
            return;
        }

        mainInput.Player.EquipQuickItem_1.performed -= OnEquipQuickItem1Performed;
        mainInput.Player.EquipQuickItem_2.performed -= OnEquipQuickItem2Performed;
        mainInput.Player.EquipQuickItem_3.performed -= OnEquipQuickItem3Performed;
        mainInput.Player.EquipQuickItem_4.performed -= OnEquipQuickItem4Performed;

        mainInput.Player.UseHoldingItem.performed -= OnUseHoldingItemPerformed;
    }

    private void OnEquipQuickItem1Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(0);
    }

    private void OnEquipQuickItem2Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(1);
    }

    private void OnEquipQuickItem3Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(2);
    }

    private void OnEquipQuickItem4Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(3);
    }

    private void OnUseHoldingItemPerformed(InputAction.CallbackContext context)
    {
        if (inventoryPlayer == null)
        {
            return;
        }

        inventoryPlayer.UseHoldingItem();
    }

    private void TryHoldQuickItem(int quickSlotIndex)
    {
        if (inventoryPlayer == null)
        {
            return;
        }

        inventoryPlayer.TryHoldQuickItem(quickSlotIndex);
    }
}