using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Note Item", fileName = "NoteItemData - ")]
public class NoteItemDataSO : ItemDataSO
{
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Note;
    }
}
