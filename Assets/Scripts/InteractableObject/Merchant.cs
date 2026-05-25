using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Merchant : PlayerSensorTarget
{
    [SerializeField] private TextMeshPro interactText;
    [SerializeField] private DialogueDataSO dialogueData;   

    private void Awake()
    {
        interactText.gameObject.SetActive(false);
    }

    public override void Interact()
    {
        base.Interact();
        if(InGameUI.Instance!=null)
        {
            InGameUI.Instance.dialogueUI.StartDialogue(dialogueData, InGameUI.Instance.ToggleMerchantUI);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        interactText.gameObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        interactText.gameObject.SetActive(false);
    }
}
