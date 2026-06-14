using UnityEngine;

/// <summary>
/// 鼹鼠移动时视觉 Transform 的 scale 正弦挤压动画（类似 DOTween 循环变形）。
/// X/Y 反相： (1,1,1) → (0.9,1.1,1) → (1,1,1) → (1.1,0.9,1) → …
/// </summary>
public class MoleMoveAni : MonoBehaviour
{
    [Tooltip("要变形的贴图 / 视觉 Transform")]
    public Transform visualTransform;

    [Tooltip("可选：同物体上的鼹鼠，用于判断是否在移动")]
    public Mole2D mole;

    [Tooltip("静止基准 localScale")]
    public Vector3 baseScale = Vector3.one;

    [Tooltip("完整循环周期（秒）")]
    public float cycleDuration = 0.5f;

    [Tooltip("挤压幅度：0.1 表示 ±10%（0.9 / 1.1）")]
    [Range(0f, 0.35f)]
    public float squishAmount = 0.1f;

    [Tooltip("停止移动后回到基准 scale 的插值速度")]
    public float restoreSpeed = 12f;

    [Tooltip("位移低于此值视为未移动（无 Mole2D 时）")]
    public float minMoveDelta = 0.002f;

    private float phaseElapsed;
    private Vector3 currentScale;
    private Vector3 lastWorldPosition;
    private bool wasMoving;

    private void Awake()
    {
        if (mole == null)
        {
            mole = GetComponent<Mole2D>();
        }

        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        if (baseScale == Vector3.one && visualTransform != null)
        {
            baseScale = visualTransform.localScale;
        }

        currentScale = baseScale;
        lastWorldPosition = transform.position;
        ApplyScale(currentScale);
    }

    private void OnEnable()
    {
        phaseElapsed = 0f;
        currentScale = baseScale;
        lastWorldPosition = transform.position;
        ApplyScale(currentScale);
    }

    private void OnDisable()
    {
        if (visualTransform != null)
        {
            visualTransform.localScale = baseScale;
        }
    }

    private void Update()
    {
        if (visualTransform == null)
        {
            return;
        }

        bool moving = IsMoving();

        if (moving)
        {
            float period = Mathf.Max(0.01f, cycleDuration);
            phaseElapsed += Time.deltaTime;
            float wave = Mathf.Sin(phaseElapsed * Mathf.PI * 2f / period);
            float squish = squishAmount * wave;

            currentScale = new Vector3(
                baseScale.x * (1f - squish),
                baseScale.y * (1f + squish),
                baseScale.z
            );
            wasMoving = true;
        }
        else
        {
            if (wasMoving)
            {
                phaseElapsed = 0f;
                wasMoving = false;
            }

            currentScale = Vector3.Lerp(
                currentScale,
                baseScale,
                Mathf.Clamp01(restoreSpeed * Time.deltaTime)
            );
        }

        ApplyScale(currentScale);
        lastWorldPosition = transform.position;
    }

    private bool IsMoving()
    {
        if (mole != null)
        {
            if (mole.Arrived)
            {
                return false;
            }

            return Vector2.Distance(mole.Position, mole.CurrentTarget) > 0.05f;
        }

        Vector3 delta = transform.position - lastWorldPosition;
        return delta.sqrMagnitude >= minMoveDelta * minMoveDelta;
    }

    private void ApplyScale(Vector3 scale)
    {
        visualTransform.localScale = scale;
    }

    public void SetMovingOverride(bool moving)
    {
        if (moving)
        {
            wasMoving = true;
        }
        else
        {
            wasMoving = false;
            phaseElapsed = 0f;
        }
    }
}
