using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Passive Item", fileName = "PassiveItemData - ")]
public class PassiveItemDataSO : ItemDataSO
{
    private void OnValidate()
    {
        itemType = ItemType.Passive;
    }
}
