using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public GameState currentGameState {  get; private set; }

    private void Awake()
    {
        if(Instance!=null&&Instance!=this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentGameState=GameState.None;
    }

    public void SetCurrentGameState(GameState newGameState)
    {
        if (newGameState != currentGameState)
        {
            currentGameState=newGameState;
        }
    }
}

public enum GameState
{
    None,
    MainMenu,
    Base,
    Game
}
