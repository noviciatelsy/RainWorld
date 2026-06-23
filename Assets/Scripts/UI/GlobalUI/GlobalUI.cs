using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalUI : MonoBehaviour
{
    public static GlobalUI Instance { get; private set; }

    public FadeScreenUI fadeScreenUI { get; private set; }
    public HintMessageUI hintMessageUI { get; private set; }

    private bool gameIsPaused = false;

    private int pauseCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;
        fadeScreenUI = GetComponentInChildren<FadeScreenUI>();
        hintMessageUI = GetComponentInChildren<HintMessageUI>();
        pauseCount = 0;
    }

    public void PlayButtonClick()
    {

    }

    public void PauseGame(bool pause)
    {
        if (pause)
        {
            pauseCount++;
            if (gameIsPaused)
            {
                return;
            }
            if(pauseCount>0)
            {
                Time.timeScale = 0f;
                InputManager.Instance.mainInput.Player.Disable();
                Player player = PlayerManager.Instance.TryGetCurrentPlayer();
                if (player!= null)
                {
                    AudioSource footStepsAudioSource = player.GetComponent<PlayerControl>().moveState.footStepsAudioSource;
                    if (footStepsAudioSource != null&& footStepsAudioSource.isPlaying)
                    {
                        footStepsAudioSource.Stop();
                    }
                }
                gameIsPaused = true;
            }
        }
        else
        {
            pauseCount--;
            if (!gameIsPaused)
            {
                return;
            }
            if(pauseCount==0)
            {
                Time.timeScale = 1;
                InputManager.Instance.mainInput.Player.Enable();
                gameIsPaused = false;
            }
        }
    }



}
