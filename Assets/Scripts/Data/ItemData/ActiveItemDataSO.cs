using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Active Item", fileName = "ActiveItemData - ")]
public class ActiveItemDataSO : ItemDataSO
{
    [Header("是否为消耗品")]
    public bool isConsumable=true;

    [Header("是否为蘑菇")]
    public bool isMushroom=false;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Active;
    }
#endif

}
