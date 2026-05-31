using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_DisableParentBlocksRaycasts : MonoBehaviour
{
    private CanvasGroup parentCanvasGroup;

    private void Awake()
    {
        parentCanvasGroup =transform.parent.GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        parentCanvasGroup.blocksRaycasts=false;
    }

    private void OnDisable()
    {
        parentCanvasGroup.blocksRaycasts = true;
    }
}
