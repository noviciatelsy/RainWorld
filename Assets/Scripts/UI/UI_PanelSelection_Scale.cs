using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_PanelSelection_Scale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("真正执行缩放的目标，不填则默认缩放自己")]
    private RectTransform scaleTarget;

    [Header("悬浮缩放倍率")]
    [SerializeField] private float selectedScaleMultiPlier = 1.05f;

    [Header("缩放平滑时间")]
    [SerializeField] private float smoothTime = 0.05f;


    private Vector3 originalScale;
    private Vector3 currentVelocity;

    // 当前是否处于悬浮状态
    private bool isHovered;


    private void Awake()
    {
        if (scaleTarget == null)
        {
            scaleTarget = GetComponent<RectTransform>();
        }

        originalScale = scaleTarget.localScale;
    }

    private void OnEnable()
    {
        isHovered = false;
    }


    private void OnDisable()
    {
        scaleTarget.localScale = originalScale;
    }

    private void Update()
    {
        Vector3 baseTargetScale = isHovered
            ? originalScale * selectedScaleMultiPlier
            : originalScale;

        Vector3 finalTargetScale = baseTargetScale;

        scaleTarget.localScale = Vector3.SmoothDamp
        (
            scaleTarget.localScale,
            finalTargetScale,
            ref currentVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

 
}
