using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_NonRectButtonSetup :  MonoBehaviour
{
    private void Awake()
    {
        Image image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = 0.1f;
    }
}
