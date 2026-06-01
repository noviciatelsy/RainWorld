using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider BGMVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    private GlobalGameData GlobalGameData;
    private UI_PanelOpenCloseAnimation panelOpenCloseAnimation;

    private void Awake()
    {
        panelOpenCloseAnimation = GetComponent<UI_PanelOpenCloseAnimation>();
    }

    private void OnEnable()
    {
        // 更新Slider数值
        GlobalGameData = SaveManager.Instance.GetGlobalGameData();

        BGMVolumeSlider.value = GlobalGameData.bgmVolume;
        SFXVolumeSlider.value = GlobalGameData.sfxVolume;
    }


    public void OnBGMVolumeChanged(float volume)
    {
        GlobalGameData.bgmVolume = volume;
        AudioManager.Instance.LoadVolume();
    }

    public void OnSFXVolumeChanged(float volume)
    {
        GlobalGameData.sfxVolume = volume;
        AudioManager.Instance.LoadVolume();
    }


    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        SaveManager.Instance.SaveGlobalGameData();
        panelOpenCloseAnimation.PlayClose();
    }
}
