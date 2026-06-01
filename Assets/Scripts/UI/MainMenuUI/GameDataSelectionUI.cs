using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataSelectionUI : MonoBehaviour
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
    }
}
