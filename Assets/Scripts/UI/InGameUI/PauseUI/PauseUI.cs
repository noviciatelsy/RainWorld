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
        AudioManager.Instance.PlayUI("ButtonClickSFX");
    }

    public void SettingsButton()
    {
        settingsPanel.Open();
        AudioManager.Instance.PlayUI("ButtonClickSFX");
    }

    public void GiveUpButton()
    {
        AudioManager.Instance.PlayUI("ButtonClickSFX");
        if (GameStateManager.Instance.currentGameState==GameState.Base)
        {
            return;
        }
        else
        {
            warningPanel_GiveUp.Open();
        }

    }

    public void ReturnToMainMenuButton()
    {
        AudioManager.Instance.PlayUI("ButtonClickSFX");
        if (GameStateManager.Instance.currentGameState==GameState.Base)
        {
            GlobalUI.Instance.fadeScreenUI.PlaySceneSwitchFade(() =>
            {
                SceneSwitchManager.Instance.SwitchToScene(SceneType.MainMenu);
            });

        }
        else
        {
            warningPanel_ReturnToMainMenu.Open();
        }

    }
}
