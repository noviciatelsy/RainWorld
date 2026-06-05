using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseSceneManager : MonoBehaviour
{
    private void Awake()
    {
        InputManager.Instance.mainInput.Enable();
        GameStateManager.Instance.SetCurrentGameState(GameState.Base);
    }
}
