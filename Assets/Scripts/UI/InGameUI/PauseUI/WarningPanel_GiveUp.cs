using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarningPanel_GiveUp : MonoBehaviour
{
    private UI_PanelOpenCloseAnimation panelOpenCloseAnimation;

    private void Awake()
    {
        panelOpenCloseAnimation = GetComponent<UI_PanelOpenCloseAnimation>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        panelOpenCloseAnimation.PlayClose();
        AudioManager.Instance.PlayUI("ButtonClickSFX");
    }

    public void ConfirmButton()
    {
        AudioManager.Instance.PlayUI("ButtonClickSFX");
        Player player=PlayerManager.Instance.TryGetCurrentPlayer();
        if (player != null)
        {
            player.GetComponent <PlayerVitals>().KillPlayer();
        }
    }
}
