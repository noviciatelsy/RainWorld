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
    }

    public void SettingsButton()
    {
        settingsPanel.Open();
    }


    public void QuitGameButton()
    {
        Application.Quit();
    }
}
