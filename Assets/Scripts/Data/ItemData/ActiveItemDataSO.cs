using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Active Item", fileName = "ActiveItemData - ")]
public class ActiveItemDataSO : ItemDataSO
{
    private void OnValidate()
    {
        itemType=ItemType.Active;
    }
}
