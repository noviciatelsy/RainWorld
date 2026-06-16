using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarningPanel_ReturnToMainMenu : MonoBehaviour
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
        AudioManager.Instance.PlayUI("ButtonClickSFX");
        panelOpenCloseAnimation.PlayClose();
    }

    public void ConfirmButton()
    {
        AudioManager.Instance.PlayUI("ButtonClickSFX");
        SaveManager.Instance.OverwriteCurrentGameRunData(); // 还原局内存档
        GlobalUI.Instance.fadeScreenUI.PlaySceneSwitchFade(() =>
        {
            SceneSwitchManager.Instance.SwitchToScene(SceneType.MainMenu);
        });
    }
}
