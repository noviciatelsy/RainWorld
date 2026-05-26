using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Merchant : PlayerSensorTarget
{
    [SerializeField] private DialogueDataSO dialogueData;   

    public override void Interact()
    {
        base.Interact();
        if(InGameUI.Instance!=null)
        {
            InGameUI.Instance.dialogueUI.StartDialogue(dialogueData, InGameUI.Instance.ToggleMerchantUI);
        }
    }


}
