using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Note Item", fileName = "NoteItemData - ")]
public class NoteItemDataSO : ItemDataSO
{
    private void OnValidate()
    {
        itemType = ItemType.Note;
    }
}
