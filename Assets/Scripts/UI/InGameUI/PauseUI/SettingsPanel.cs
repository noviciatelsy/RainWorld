using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider BGMVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    private GameData gameData;
    private UI_PanelOpenCloseAnimation panelOpenCloseAnimation;

    private void Awake()
    {
        panelOpenCloseAnimation = GetComponent<UI_PanelOpenCloseAnimation>();
    }

    private void OnEnable()
    {
        // 更新Slider数值
        gameData = SaveManager.Instance.GetRunTimeGameData();

        BGMVolumeSlider.value = gameData.bgmVolume;
        SFXVolumeSlider.value = gameData.sfxVolume;
    }


    public void OnBGMVolumeChanged(float volume)
    {
        gameData.bgmVolume = volume;
        AudioManager.Instance.LoadVolume();
    }

    public void OnSFXVolumeChanged(float volume)
    {
        gameData.sfxVolume = volume;
        AudioManager.Instance.LoadVolume();
    }


    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        SaveManager.Instance.SaveGame();
        panelOpenCloseAnimation.PlayClose();
    }
}
