using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseUI : MonoBehaviour
{
    private InGameUI inGameUI;
    private SettingsPanel settingsPanel;
    private WarningPanel_GiveUp warningPanel_GiveUp;
    private WarningPanel_ReturnToMainMenu warningPanel_ReturnToMainMenu;
    private void Awake()
    {
        inGameUI = GetComponentInParent<InGameUI>();
        settingsPanel = GetComponentInChildren<SettingsPanel>(true);
        warningPanel_GiveUp=GetComponentInChildren<WarningPanel_GiveUp>(true);
        warningPanel_ReturnToMainMenu=GetComponentInChildren<WarningPanel_ReturnToMainMenu>(true);
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

    public void SettingsButton()
    {
        settingsPanel.Open();
    }

    public void GiveUpButton()
    {
        warningPanel_GiveUp.Open();
    }

    public void ReturnToMainMenuButton()
    {
        warningPanel_ReturnToMainMenu.Open();
    }
}
