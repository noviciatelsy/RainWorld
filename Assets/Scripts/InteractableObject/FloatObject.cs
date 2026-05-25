using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatObject : MonoBehaviour
{
    [Header("Floaty movement")]
    [SerializeField] private float floatSpeed = 1; // 上限浮动速度
    [SerializeField] private float floatRange = 0.1f; // 上下浮动范围
    private Vector3 startPosition; // 初始位置

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange; // y方向位置随时间做简谐浮动
        transform.position = startPosition + new Vector3(0, yOffset); // 更新位置
    }
}
