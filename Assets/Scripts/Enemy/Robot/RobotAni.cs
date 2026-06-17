using UnityEngine;

/// <summary>
/// 机器人动画：移动时双轮旋转 + 整体 scale 挤压；移动方向翻转贴图。
/// 游荡：(1,1,1)→(0.9,1.1,1)；冲刺：(1,1,1)→(1.2,0.8,1)→(1,1,1)。转速与当前移动速度挂钩。
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

    [Tooltip("左右翻转根节点（默认 visualTransform，仅改 X 符号）")]
    public Transform flipTransform;

    [Tooltip("躯干挤压节点（默认自动查找「躯干」子物体，形变更明显）")]
    public Transform squashTransform;

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

    [Header("Charge Squash")]
    [Tooltip("冲刺横向拉伸比例（相对躯干 baseScale.x）")]
    public float chargeSquashScaleX = 1.45f;

    [Tooltip("冲刺纵向压缩比例（相对躯干 baseScale.y）")]
    public float chargeSquashScaleY = 0.55f;

    [Tooltip("冲刺开始时压到峰值的过渡时间（秒）")]
    public float chargeSquashAttackDuration = 0.1f;

    [Tooltip("停止移动后回到基准 scale 的插值速度")]
    public float restoreSpeed = 12f;

    [Tooltip("位移低于此值视为未移动（无 Robot2D 时）")]
    public float minMoveDelta = 0.002f;

    private float phaseElapsed;
    private Vector3 currentSquashScale;
    private Vector3 squashBaseScale = Vector3.one;
    private Vector3 flipBaseScale = Vector3.one;
    private Vector3 lastWorldPosition;
    private bool wasMoving;
    private bool wasChargeSquish;
    private float chargeSquashFactor;
    private int facingSign = 1;

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

        if (flipTransform == null)
        {
            flipTransform = visualTransform;
        }

        if (squashTransform == null)
        {
            Transform torso = flipTransform.Find("躯干");
            squashTransform = torso != null ? torso : flipTransform;
        }

        if (baseScale == Vector3.one && squashTransform != null)
        {
            baseScale = squashTransform.localScale;
        }

        squashBaseScale = squashTransform != null ? squashTransform.localScale : baseScale;
        squashBaseScale.x = Mathf.Abs(squashBaseScale.x);
        squashBaseScale.y = Mathf.Abs(squashBaseScale.y);
        squashBaseScale.z = Mathf.Abs(squashBaseScale.z);

        flipBaseScale = flipTransform != null ? flipTransform.localScale : Vector3.one;
        flipBaseScale.x = Mathf.Abs(flipBaseScale.x);
        if (flipBaseScale.x < 0.001f)
        {
            flipBaseScale.x = 1f;
        }

        if (flipTransform != null && flipTransform.localScale.x < 0f)
        {
            facingSign = -1;
        }

        currentSquashScale = squashBaseScale;
        lastWorldPosition = transform.position;
        ApplyVisuals();
    }

    private void OnEnable()
    {
        phaseElapsed = 0f;
        chargeSquashFactor = 0f;
        currentSquashScale = squashBaseScale;
        lastWorldPosition = transform.position;
        ApplyVisuals();
    }

    private void OnDisable()
    {
        ApplyFacingOnly();
        if (squashTransform != null)
        {
            squashTransform.localScale = squashBaseScale;
        }
    }

    private void Update()
    {
        if (robot != null && robot.IsStompPaused)
        {
            return;
        }

        if (robot != null && robot.IsDrinkFrozen)
        {
            UpdateFrozenVisualRestore();
            return;
        }

        bool moving = IsMoving();
        Vector3 delta = transform.position - lastWorldPosition;

        UpdateFacing(delta.x, moving);
        UpdateWheelRotation(delta.x, moving);
        UpdateSquish(moving);

        lastWorldPosition = transform.position;
    }

    private void UpdateFrozenVisualRestore()
    {
        wasChargeSquish = false;
        wasMoving = false;

        chargeSquashFactor = Mathf.MoveTowards(chargeSquashFactor, 0f, restoreSpeed * Time.deltaTime);
        currentSquashScale = Vector3.Lerp(
            currentSquashScale,
            squashBaseScale,
            Mathf.Clamp01(restoreSpeed * Time.deltaTime)
        );

        ApplyVisuals();
    }

    private void UpdateFacing(float deltaX, bool moving)
    {
        if (!moving)
        {
            return;
        }

        float moveDir = ResolveMoveDirection(deltaX);
        if (moveDir == 0f)
        {
            return;
        }

        facingSign = moveDir > 0f ? -1 : 1;
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
        if (squashTransform == null)
        {
            ApplyFacingOnly();
            return;
        }

        bool chargeSquish = robot != null && robot.CurrentBehavior == RobotBehavior.Charge;

        if (chargeSquish && !wasChargeSquish)
        {
            phaseElapsed = 0f;
            chargeSquashFactor = 0f;
        }

        wasChargeSquish = chargeSquish;

        if (chargeSquish)
        {
            float attack = Mathf.Max(0.01f, chargeSquashAttackDuration);
            chargeSquashFactor = Mathf.MoveTowards(chargeSquashFactor, 1f, Time.deltaTime / attack);

            currentSquashScale = new Vector3(
                squashBaseScale.x * Mathf.Lerp(1f, chargeSquashScaleX, chargeSquashFactor),
                squashBaseScale.y * Mathf.Lerp(1f, chargeSquashScaleY, chargeSquashFactor),
                squashBaseScale.z
            );

            wasMoving = true;
        }
        else if (moving)
        {
            chargeSquashFactor = Mathf.MoveTowards(chargeSquashFactor, 0f, restoreSpeed * Time.deltaTime);

            float period = Mathf.Max(0.01f, cycleDuration);
            phaseElapsed += Time.deltaTime;
            float wave = Mathf.Sin(phaseElapsed * Mathf.PI * 2f / period);
            float squish = squishAmount * wave;

            currentSquashScale = new Vector3(
                squashBaseScale.x * (1f - squish),
                squashBaseScale.y * (1f + squish),
                squashBaseScale.z
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

            chargeSquashFactor = Mathf.MoveTowards(chargeSquashFactor, 0f, restoreSpeed * Time.deltaTime);

            Vector3 targetScale = squashBaseScale;
            if (chargeSquashFactor > 0.001f)
            {
                targetScale = new Vector3(
                    squashBaseScale.x * Mathf.Lerp(1f, chargeSquashScaleX, chargeSquashFactor),
                    squashBaseScale.y * Mathf.Lerp(1f, chargeSquashScaleY, chargeSquashFactor),
                    squashBaseScale.z
                );
            }

            currentSquashScale = Vector3.Lerp(
                currentSquashScale,
                targetScale,
                Mathf.Clamp01(restoreSpeed * Time.deltaTime)
            );
        }

        ApplyVisuals();
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
            if (robot.IsDrinkFrozen || robot.CurrentBehavior == RobotBehavior.Recover)
            {
                return false;
            }

            if (robot.CurrentBehavior == RobotBehavior.Charge)
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

    private void ApplyVisuals()
    {
        ApplyFacingOnly();

        if (squashTransform != null)
        {
            squashTransform.localScale = currentSquashScale;
        }
    }

    private void ApplyFacingOnly()
    {
        if (flipTransform == null)
        {
            return;
        }

        flipTransform.localScale = new Vector3(
            flipBaseScale.x * facingSign,
            flipBaseScale.y,
            flipBaseScale.z);
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
            chargeSquashFactor = 0f;
        }
    }
}
