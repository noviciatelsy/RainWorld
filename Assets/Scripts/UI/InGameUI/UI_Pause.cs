using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Pause : MonoBehaviour
{
    private void OnEnable()
    {
        GlobalUI.Instance.PauseGame(true);
    }

    private void OnDisable()
    {
        GlobalUI.Instance.PauseGame(false);
    }
}
