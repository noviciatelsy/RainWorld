using UnityEngine;

public class NPCSpringIdle2D : MonoBehaviour
{
    [Header("弹簧待机参数")]

    [Tooltip("每秒弹动次数")]
    [SerializeField] private float frequency = 1.5f;

    [Tooltip("Y方向最大压缩/拉伸比例")]
    [SerializeField] private float squashAmount = 0.02f;

    [Tooltip("是否在启用时随机起始动画时间，避免一群NPC同步弹")]
    [SerializeField] private bool randomStartTime = true;

    // 初始缩放，用于在原本大小基础上做动画
    private Vector3 originalScale;

    // 当前动画时间
    private float timer;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (randomStartTime)
        {
            timer = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        // 使用deltaTime推进动画，让效果不受帧率影响
        timer += Time.deltaTime * frequency * Mathf.PI * 2f;

        // sin范围是 -1 到 1
        // 为正时：Y变小，X变大，表现为向下挤压
        // 为负时：Y变大，X变小，表现为弹起拉伸
        float wave = Mathf.Sin(timer);

        // Y缩放比例
        float yRatio = 1f - wave * squashAmount;

        // X缩放比例
        // 这里让 x * y 基本保持不变，也就是2D里的“面积守恒”
        float xRatio = 1f / yRatio;

        transform.localScale = new Vector3
        (
            originalScale.x * xRatio,
            originalScale.y * yRatio,
            originalScale.z
        );
    }

    private void OnDisable()
    {
        // 禁用时还原，避免对象池复用时缩放残留
        transform.localScale = originalScale;
    }
}