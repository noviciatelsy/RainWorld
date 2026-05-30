using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseUI : MonoBehaviour
{
    private InGameUI inGameUI;

    private void Awake()
    {
        inGameUI = GetComponentInParent<InGameUI>();
    }
    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void ResumeGameButton()
    {
        inGameUI.TogglePauseUI();
    }
}
