using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Active Item", fileName = "ActiveItemData - ")]
public class ActiveItemDataSO : ItemDataSO
{
    [Header("是否为消耗品")]
    public bool isConsumable=true;

    private void OnValidate()
    {
        itemType=ItemType.Active;
    }
}
