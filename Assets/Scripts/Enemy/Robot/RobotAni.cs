using UnityEngine;

/// <summary>
/// 机器人动画：移动时双轮旋转 + 整体 scale 正弦挤压。
/// 游荡：(1,1,1)→(0.9,1.1,1)；冲刺：(1,1,1)→(1.1,0.9,1)。转速与当前移动速度挂钩。
/// </summary>
public class RobotAni : MonoBehaviour
{
    [Header("References")]
    [Tooltip("左轮贴图 Transform")]
    public Transform leftWheel;

    [Tooltip("右轮贴图 Transform")]
    public Transform rightWheel;

    [Tooltip("要变形的贴图 / 视觉 Transform")]
    public Transform visualTransform;

    [Tooltip("可选：同物体上的机器人，用于移动状态与速度")]
    public Robot2D robot;

    [Header("Wheel Rotation")]
    [Tooltip("转速系数：角速度（度/秒）= speed × 当前移动速度")]
    public float speed = 120f;

    [Header("Squish (same as MoleMoveAni)")]
    [Tooltip("静止基准 localScale")]
    public Vector3 baseScale = Vector3.one;

    [Tooltip("完整循环周期（秒）")]
    public float cycleDuration = 0.5f;

    [Tooltip("挤压幅度：0.1 表示 ±10%（0.9 / 1.1）")]
    [Range(0f, 0.35f)]
    public float squishAmount = 0.1f;

    [Tooltip("停止移动后回到基准 scale 的插值速度")]
    public float restoreSpeed = 12f;

    [Tooltip("位移低于此值视为未移动（无 Robot2D 时）")]
    public float minMoveDelta = 0.002f;

    private float phaseElapsed;
    private Vector3 currentScale;
    private Vector3 lastWorldPosition;
    private bool wasMoving;

    private void Awake()
    {
        if (robot == null)
        {
            robot = GetComponent<Robot2D>();
        }

        if (visualTransform == null && robot != null && robot.bodyVisual != null)
        {
            visualTransform = robot.bodyVisual;
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
        bool moving = IsMoving();
        Vector3 delta = transform.position - lastWorldPosition;

        UpdateWheelRotation(delta.x, moving);
        UpdateSquish(moving);

        lastWorldPosition = transform.position;
    }

    private void UpdateWheelRotation(float deltaX, bool moving)
    {
        if (!moving)
        {
            return;
        }

        float moveSpeedNow = GetCurrentMoveSpeed();
        if (moveSpeedNow <= 0f)
        {
            return;
        }

        float moveDir = ResolveMoveDirection(deltaX);
        if (moveDir == 0f)
        {
            return;
        }

        float angleDelta = speed * moveSpeedNow * Time.deltaTime;
        float zDelta = -moveDir * angleDelta;

        if (leftWheel != null)
        {
            leftWheel.Rotate(0f, 0f, zDelta, Space.Self);
        }

        if (rightWheel != null)
        {
            rightWheel.Rotate(0f, 0f, zDelta, Space.Self);
        }
    }

    private float ResolveMoveDirection(float deltaX)
    {
        if (Mathf.Abs(deltaX) >= minMoveDelta)
        {
            return Mathf.Sign(deltaX);
        }

        if (robot == null)
        {
            return 0f;
        }

        float toTargetX = robot.CurrentTarget.x - robot.Position.x;
        if (Mathf.Abs(toTargetX) < minMoveDelta)
        {
            return 0f;
        }

        return Mathf.Sign(toTargetX);
    }

    private void UpdateSquish(bool moving)
    {
        if (visualTransform == null)
        {
            return;
        }

        if (moving)
        {
            float period = Mathf.Max(0.01f, cycleDuration);
            phaseElapsed += Time.deltaTime;
            float wave = Mathf.Sin(phaseElapsed * Mathf.PI * 2f / period);
            float squish = squishAmount * wave;
            bool chargeSquish = robot != null && robot.CurrentBehavior == RobotBehavior.Charge;

            if (chargeSquish)
            {
                currentScale = new Vector3(
                    baseScale.x * (1f + squish),
                    baseScale.y * (1f - squish),
                    baseScale.z
                );
            }
            else
            {
                currentScale = new Vector3(
                    baseScale.x * (1f - squish),
                    baseScale.y * (1f + squish),
                    baseScale.z
                );
            }

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
    }

    private float GetCurrentMoveSpeed()
    {
        if (robot == null)
        {
            return 0f;
        }

        if (robot.CurrentBehavior == RobotBehavior.Charge)
        {
            return robot.chargeSpeed;
        }

        return robot.moveSpeed;
    }

    private bool IsMoving()
    {
        if (robot != null)
        {
            if (robot.CurrentBehavior == RobotBehavior.Recover)
            {
                return false;
            }

            if (robot.CurrentBehavior == RobotBehavior.Charge && !robot.Arrived)
            {
                return true;
            }

            if (robot.Arrived)
            {
                return false;
            }

            return Vector2.Distance(robot.Position, robot.CurrentTarget) > robot.arriveThreshold;
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
