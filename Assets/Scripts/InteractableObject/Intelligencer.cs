using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intelligencer : PlayerSensorTarget
{
    [SerializeField] private DialogueDataSO dialogueData;

    public override void Interact()
    {
        base.Interact();
        if (InGameUI.Instance != null)
        {
            InGameUI.Instance.dialogueUI.StartDialogue(dialogueData, InGameUI.Instance.ToggleIntelligencerUI);
        }
    }
}
