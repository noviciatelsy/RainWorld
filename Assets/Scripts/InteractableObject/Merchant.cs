using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Merchant : PlayerSensorTarget
{

    [SerializeField] private DialogueDataSO tutorialDialogueData;
    [SerializeField] private DialogueDataSO firstDeathDialogueData;

    GameRunData gameRunData;

    protected override void Awake()
    {
        base.Awake();
        gameRunData=SaveManager.Instance.GetRunTimeGameData();
    }

    public override void Interact()
    {
        base.Interact();
        InGameUI.Instance.ToggleMerchantUI();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if(gameRunData.hasPassedMerchantTutorialDialogue==false)
        {
            gameRunData.hasPassedMerchantTutorialDialogue = true;
            InGameUI.Instance.dialogueUI.StartDialogue(tutorialDialogueData);
            AudioManager.Instance.PlaySFX("MerchantDialogueSFX");
            SaveManager.Instance.SaveGame();
        }

        if(gameRunData.hasFirstDeath==true&&gameRunData.hasPassedMerchantFirstDeathDialogue==false)
        {
            gameRunData.hasPassedMerchantFirstDeathDialogue = true;
            InGameUI.Instance.dialogueUI.StartDialogue(firstDeathDialogueData);
            AudioManager.Instance.PlaySFX("MerchantDialogueSFX");
            SaveManager.Instance.SaveGame();
        }
    }

}
