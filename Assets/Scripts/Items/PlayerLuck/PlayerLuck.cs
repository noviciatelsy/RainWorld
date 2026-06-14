using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLuck : MonoBehaviour
{
    public int bonusItemLootAmount { get; private set; }
    public int bonusLuck { get; private set; }

    public void AddBonusItemLootAmount(int AmountToAdd)
    {
        bonusItemLootAmount += Mathf.Max(0, AmountToAdd);
    }

    public void ReduceBonusItemLootAmount(int AmountToReduce)
    {
        bonusItemLootAmount -= Mathf.Max(0, AmountToReduce);
        bonusItemLootAmount = Mathf.Max(0, bonusItemLootAmount);
    }

    public void AddBonusLuck(int LuckToAdd)
    {
        bonusLuck += Mathf.Max(0, LuckToAdd);
    }

    public void ReduceBonusLuck(int LuckToReduce)
    {
        bonusLuck -= Mathf.Max(0, LuckToReduce);
        bonusLuck = Mathf.Max(0, bonusLuck);
    }
}