using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Passive Item", fileName = "PassiveItemData - ")]
public class PassiveItemDataSO : ItemDataSO
{
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Passive;
    }
}
