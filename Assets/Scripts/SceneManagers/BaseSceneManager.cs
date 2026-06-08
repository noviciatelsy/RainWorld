using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseSceneManager : MonoBehaviour
{
    [SerializeField] private RetrieveBackpack retrieveBackpack;
    private void Awake()
    {
        InputManager.Instance.mainInput.Enable();
        GameStateManager.Instance.SetCurrentGameState(GameState.Base);
        SpawnBackpack();
    }

    private void SpawnBackpack()
    {
        if (retrieveBackpack != null)
        {
            Vector3 spawnPosition = SaveManager.Instance.GetRunTimeGameData().retrieveBackpackSpawnPosition;
            if (spawnPosition != Vector3.zero)
            {
                Instantiate(retrieveBackpack, spawnPosition, Quaternion.identity);
            }
        }
    }
}
