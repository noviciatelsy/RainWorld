using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public DraggedItemUI draggedItemUI {  get; private set; }

    private void Awake()
    {
        draggedItemUI = GetComponentInChildren<DraggedItemUI>(true);
    }
}
