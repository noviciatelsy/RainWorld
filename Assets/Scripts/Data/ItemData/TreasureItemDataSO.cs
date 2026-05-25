using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Treasure Item", fileName = "TreasureItemData - ")]
public class TreasureItemDataSO : ItemDataSO
{
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Treasure;
    }
}
