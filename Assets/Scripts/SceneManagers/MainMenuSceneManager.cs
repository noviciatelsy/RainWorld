using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuSceneManager : MonoBehaviour
{
    private void Awake()
    {
        InputManager.Instance.mainInput.Disable();
        GameStateManager.Instance.SetCurrentGameState(GameState.MainMenu);
        SaveManager.Instance.SaveGame();
    }
}
