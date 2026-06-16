using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    private SettingsPanel settingsPanel;
    private GameDataSelectionUI gameDataSelectionUI;
    private void Awake()
    {
        settingsPanel=GetComponentInChildren<SettingsPanel>(true);
        gameDataSelectionUI=GetComponentInChildren<GameDataSelectionUI>(true);
    }

    public void StartGameButton()
    {
        gameDataSelectionUI.Open();
        AudioManager.Instance.PlayUI("ButtonClickSFX");
    }

    public void SettingsButton()
    {
        settingsPanel.Open();
        AudioManager.Instance.PlayUI("ButtonClickSFX");
    }


    public void QuitGameButton()
    {
        AudioManager.Instance.PlayUI("ButtonClickSFX");
        Application.Quit();
    }
}
