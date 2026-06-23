using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbInputTutorialTrigger : MonoBehaviour
{
    private bool isShowHint=false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<Player>() != null)
        {
            if(SaveManager.Instance.GetRunTimeGameData().hasShowClimbInputTutorial==false)
            {
                GlobalUI.Instance.hintMessageUI.ShowLongTimeMessage("按s+空格可从平台落下\n按s和w可在绳子上攀爬");
                isShowHint = true;
                SaveManager.Instance.GetRunTimeGameData().hasShowClimbInputTutorial=true;
                SaveManager.Instance.SaveGame();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            if(isShowHint)
            {
                GlobalUI.Instance.hintMessageUI.StopLongTimeMessage();
                isShowHint= false;
            }
        }
    }
}
