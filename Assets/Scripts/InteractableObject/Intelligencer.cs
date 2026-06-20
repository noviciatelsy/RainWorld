using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intelligencer : PlayerSensorTarget
{

    [SerializeField] private DialogueDataSO tutorialDialogueData;
    GameRunData gameRunData;

    protected override void Awake()
    {
        base.Awake();
        gameRunData = SaveManager.Instance.GetRunTimeGameData();
    }
    public override void Interact()
    {
        base.Interact();
        if (InGameUI.Instance != null)
        {
            InGameUI.Instance.ToggleIntelligencerUI();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (gameRunData.hasPassedIntelligencerTutorialDialogue == false)
        {
            gameRunData.hasPassedIntelligencerTutorialDialogue= true;
            InGameUI.Instance.dialogueUI.StartDialogue(tutorialDialogueData);
            AudioManager.Instance.PlaySFX("IntelligencerDialogueSFX"); 
            SaveManager.Instance.SaveGame();
        }

  
    }
}
