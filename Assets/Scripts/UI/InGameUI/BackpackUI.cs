using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackpackUI : MonoBehaviour
{
    private InventoryPlayer inventoryPlayer;

    private void OnEnable()
    {
        GetInventoryPlayer(PlayerManager.Instance.CurrentPlayer);
        PlayerManager.Instance.OnPlayerRegistered += GetInventoryPlayer;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerRegistered -= GetInventoryPlayer;
    }

    private void GetInventoryPlayer(Player player)
    {
        Player currentPlayer = player;
        if (currentPlayer!=null)
        {
            inventoryPlayer=currentPlayer.GetComponent<InventoryPlayer>();
        }
    }
}
