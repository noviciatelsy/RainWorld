using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveInputTutorialTrigger : MonoBehaviour
{
    private bool isShowHint = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            if (SaveManager.Instance.GetRunTimeGameData().hasShowMoveInputTutorial == false)
            {
                GlobalUI.Instance.hintMessageUI.ShowLongTimeMessage("∞¥AD◊Û”““∆∂Ø£¨ø’∏ÒÃ¯‘æ");
                isShowHint = true;
                SaveManager.Instance.GetRunTimeGameData().hasShowMoveInputTutorial = true;
                SaveManager.Instance.SaveGame();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            if (isShowHint)
            {
                GlobalUI.Instance.hintMessageUI.StopLongTimeMessage();
                isShowHint = false;
            }
        }
    }
}
