using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_DisableUIInput : MonoBehaviour
{
    private MainInput mainInput;
    private void Awake()
    {
        mainInput=InputManager.Instance.mainInput;
    }

    private void OnEnable()
    {
        mainInput.UI.Disable();
    }

    private void OnDisable()
    {
        mainInput.UI.Enable();
    }
}
