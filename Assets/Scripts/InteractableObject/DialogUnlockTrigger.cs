using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogUnlockTrigger : MonoBehaviour
{
    [SerializeField] private string triggerID;

    [SerializeField] private DialogueDataSO dialogueData;
    [SerializeField] private IntelligenceDataSO normalIntelligenceData;

    private bool hasTriggered;

    private void Start()
    {
        GameRunData data = SaveManager.Instance.GetRunTimeGameData();

        if (data == null)
            return;

        if (data.triggeredDialogues.Contains(triggerID))
        {
            hasTriggered = true;
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<Player>()!=null)
        {
            if (hasTriggered)
                return;

            if (collision.GetComponent<Player>() == null)
                return;

            hasTriggered = true;

            if (dialogueData != null)
            {
                InGameUI.Instance.dialogueUI.StartDialogue(dialogueData, UnlockIntelligence);
            }

        }
    }

    private void UnlockIntelligence()
    {
        if (normalIntelligenceData != null)
        {
            IntelligenceArchiveManager.Instance.UnlockIntelligence(normalIntelligenceData);
        }

        GameRunData data = SaveManager.Instance.GetRunTimeGameData();

        if (data != null &&
            !data.triggeredDialogues.Contains(triggerID))
        {
            data.triggeredDialogues.Add(triggerID);

            SaveManager.Instance.SaveGame();
        }

        gameObject.SetActive(false);
    }
}
