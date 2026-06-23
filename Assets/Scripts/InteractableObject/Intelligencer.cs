using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intelligencer : PlayerSensorTarget
{

    [SerializeField] private DialogueDataSO tutorialDialogueData;
    [SerializeField] private DialogueDataSO hintDialogueData;
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
        if(gameRunData.tryTime>=2&&gameRunData.hasShowIntelligencerHint==false)
        {
            gameRunData.hasShowIntelligencerHint= true;
            InGameUI.Instance.dialogueUI.StartDialogue(hintDialogueData);
            AudioManager.Instance.PlaySFX("IntelligencerDialogueSFX");
            SaveManager.Instance.SaveGame();
        }
  
    }
}
