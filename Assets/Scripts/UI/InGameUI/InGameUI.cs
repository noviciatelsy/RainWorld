using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public DraggedItemUI draggedItemUI {  get; private set; }
    public BackpackUI backpackUI { get; private set; }
    public LootUI lootUI { get; private set; }
    public RetrieveUI retrieveUI { get; private set; }

    private bool backpackUIEnabled;
    private bool lootUIEnabled;
    private bool retrieveUIEnabled;
    private void Awake()
    {
        draggedItemUI = GetComponentInChildren<DraggedItemUI>(true);
        backpackUI = GetComponentInChildren<BackpackUI>(true);
        lootUI = GetComponentInChildren<LootUI>(true);
        retrieveUI= GetComponentInChildren<RetrieveUI>(true);   

        backpackUIEnabled=backpackUI.gameObject.activeSelf;
        lootUIEnabled=lootUI.gameObject.activeSelf;
        retrieveUIEnabled=retrieveUI.gameObject.activeSelf;
    }
}

public enum InGamePanelType
{
    None,
    Backpack,
    Loot,
    Retrieve
}