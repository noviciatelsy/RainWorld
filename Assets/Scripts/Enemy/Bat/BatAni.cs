using UnityEngine;

/// <summary>
/// 蝙蝠动画：移动时 Batfly，攻击序列时 BatAttack；bodyVisual 上做正弦挤压。
/// </summary>
[DisallowMultipleComponent]
public class BatAni : MonoBehaviour
{
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    [Header("Animator States")]
    [Tooltip("Animator 中飞行状态名")]
    public string flyStateName = "Batfly";

    [Tooltip("Animator 中攻击状态名")]
    public string attackStateName = "BatAttack";

    [Header("References")]
    [Tooltip("可选：同物体或父物体上的蝙蝠")]
    public Bat2D bat;

    [Tooltip("驱动精灵帧的 Animator（贴图子物体）")]
    public Animator animator;

    [Tooltip("做 scale 挤压的视觉根（通常为 bodyVisual / Texture）")]
    public Transform visualTransform;

    [Header("Sprite Animation")]
    [Tooltip("与 Animation Clip Sample Rate 对齐的参考值（逻辑仍按 cycleDuration 驱动）")]
    public int sampleRate = 24;

    [Header("Fly Squish")]
    [Tooltip("静止基准 localScale")]
    public Vector3 baseScale = Vector3.one;

    [Tooltip("完整循环周期（秒）")]
    public float cycleDuration = 0.5f;

    [Tooltip("飞行挤压幅度：0.05 → (0.95,1.05)")]
    [Range(0f, 0.35f)]
    public float squishAmount = 0.05f;

    [Header("Attack Squish")]
    [Tooltip("攻击挤压幅度：0.1 → (1.1,0.9)")]
    [Range(0f, 0.35f)]
    public float attackSquishAmount = 0.1f;

    [Tooltip("停止移动后回到基准 scale 的插值速度")]
    public float restoreSpeed = 12f;

    [Tooltip("位移低于此值视为未移动")]
    public float minMoveDelta = 0.002f;

    private float phaseElapsed;
    private Vector3 currentScale;
    private Vector3 lastWorldPosition;
    private bool wasMoving;
    private bool lastAttackAnim;
    private bool lastFlyAnim;

    private void Awake()
    {
        ConfigureAnimationDefaults();

        if (bat == null)
        {
            bat = GetComponent<Bat2D>();
        }

        if (bat == null)
        {
            bat = GetComponentInParent<Bat2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (visualTransform == null && bat != null && bat.bodyVisual != null)
        {
            visualTransform = bat.bodyVisual;
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
        ApplyVisualScale(currentScale);
    }

    protected virtual void ConfigureAnimationDefaults()
    {
    }

    private void OnEnable()
    {
        phaseElapsed = 0f;
        lastAttackAnim = false;
        lastFlyAnim = false;
        currentScale = baseScale;
        lastWorldPosition = transform.position;
        ApplySpriteAnimation(true);
        ApplyVisualScale(currentScale);
    }

    private void OnDisable()
    {
        if (visualTransform != null)
        {
            visualTransform.localScale = baseScale;
        }
    }

    private void LateUpdate()
    {
        if (bat == null)
        {
            return;
        }

        if (bat.IsStompPaused)
        {
            return;
        }

        ApplySpriteAnimation(false);
        UpdateSquish(bat.IsInAttackSequence);
        lastWorldPosition = transform.position;
    }

    private void ApplySpriteAnimation(bool force)
    {
        if (animator == null)
        {
            return;
        }

        bool attackAnim = bat.IsInAttackSequence;
        bool flyAnim = !attackAnim && IsMoving();

        if (!force && attackAnim == lastAttackAnim && flyAnim == lastFlyAnim)
        {
            return;
        }

        lastAttackAnim = attackAnim;
        lastFlyAnim = flyAnim;

        animator.SetBool(IsAttackingHash, attackAnim);

        if (attackAnim)
        {
            animator.CrossFade(attackStateName, 0.05f, 0, 0f);
            return;
        }

        if (flyAnim)
        {
            animator.CrossFade(flyStateName, 0.05f, 0, 0f);
        }
    }

    private void UpdateSquish(bool attacking)
    {
        if (visualTransform == null)
        {
            return;
        }

        bool moving = IsMoving();

        if (moving || attacking)
        {
            float period = Mathf.Max(1f / Mathf.Max(1, sampleRate), cycleDuration);
            phaseElapsed += Time.deltaTime;
            float wave = Mathf.Sin(phaseElapsed * Mathf.PI * 2f / period);

            if (attacking)
            {
                float squish = attackSquishAmount * wave;
                currentScale = new Vector3(
                    baseScale.x * (1f + squish),
                    baseScale.y * (1f - squish),
                    baseScale.z
                );
            }
            else
            {
                float squish = squishAmount * wave;
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

        ApplyVisualScale(currentScale);
    }

    private void ApplyVisualScale(Vector3 scale)
    {
        if (bat == null)
        {
            visualTransform.localScale = scale;
            return;
        }

        float faceSign = ResolveFacingSign();
        scale.x = Mathf.Abs(scale.x) * faceSign;
        visualTransform.localScale = scale;
    }

    private float ResolveFacingSign()
    {
        Vector2 moveDir = bat.LastMoveDirection;

        if (Mathf.Abs(moveDir.x) >= minMoveDelta)
        {
            return moveDir.x >= 0f ? 1f : -1f;
        }

        if (visualTransform != null && Mathf.Abs(visualTransform.localScale.x) > 0.0001f)
        {
            return Mathf.Sign(visualTransform.localScale.x);
        }

        return 1f;
    }

    private bool IsMoving()
    {
        if (bat.IsInAttackSequence || bat.IsCoolingDown)
        {
            return false;
        }

        if (bat.CurrentBehavior == BatBehavior.Hunt)
        {
            return !bat.Arrived;
        }

        if (bat.CurrentBehavior == BatBehavior.Idle && !bat.Arrived)
        {
            return true;
        }

        Vector3 delta = transform.position - lastWorldPosition;
        return delta.sqrMagnitude >= minMoveDelta * minMoveDelta;
    }
}
