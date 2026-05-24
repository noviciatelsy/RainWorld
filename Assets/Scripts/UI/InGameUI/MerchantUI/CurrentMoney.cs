using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrentMoney : MonoBehaviour
{
    private TextMeshProUGUI moneyText;
    private InventoryPlayer playerInventory; 

    private void Awake()
    {
        moneyText = GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        TrySubscribe(PlayerManager.Instance.TryGetCurrentPlayer());
        PlayerManager.Instance.OnPlayerRegistered += TrySubscribe;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerRegistered -= TrySubscribe;

        if (playerInventory != null)
        {
            playerInventory.onMoneyChanged -= UpdateMoneyText;
        }
    }

    private void TrySubscribe(Player player)
    {
        if (player == null)
        {
            return;
        }

        playerInventory=player.GetComponent<InventoryPlayer>();

        if (playerInventory != null)
        {
            playerInventory.onMoneyChanged += UpdateMoneyText;
        }

        moneyText.text=playerInventory.money.ToString();
    }

    private void UpdateMoneyText(int currentMoney)
    {
        moneyText.text = currentMoney.ToString();
    }
}
