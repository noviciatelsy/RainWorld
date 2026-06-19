using UnityEngine;

/// <summary>
/// 大机器人动画：Idle / 攻击切换 sprite；scale/position 与 Animator 当前状态同步，避免过渡末帧错位。
/// 电池损坏后切换为静态关机贴图并停止 Animator。
/// </summary>
[DisallowMultipleComponent]
public class BigRobotAni : MonoBehaviour
{
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    [Header("Animator States")]
    public string idleStateName = "BigRobotIdle";
    public string attackStateName = "BigRobotAttack1";

    [Header("References")]
    public BigRobot2D bigRobot;
    public Animator animator;
    public SpriteRenderer bodySpriteRenderer;

    [Tooltip("Idle / 攻击时切换 localScale、localPosition 的 Transform（通常为 Textures）")]
    public Transform scaleTransform;

    [Header("Scale")]
    public Vector3 idleScale = new Vector3(0.75f, 0.75f, 0.75f);
    public Vector3 attackScale = Vector3.one;

    [Header("Position")]
    public Vector3 idlePosition = Vector3.zero;
    public Vector3 attackPosition = new Vector3(-1f, 0.9f, 0f);

    [Header("Shutdown")]
    [SerializeField] private Sprite shutdownSpriteAsset;
    [SerializeField] private string shutdownSpriteResourcePath = "textures/敌人资源/大机器人/关机动画/图层_57-removebg-preview";
    [SerializeField] private Vector3 shutdownScale = new Vector3(0.9f, 0.9f, 0.9f);
    [SerializeField] private Vector3 shutdownPosition = Vector3.zero;

    private int idleStateHash;
    private int attackStateHash;
    private bool lastAttackIntent;
    private int lastAttackAnimVersion = -1;
    private bool lastAttackTransform;
    private bool shutdownApplied;
    private Sprite shutdownSprite;

    private void Awake()
    {
        if (bigRobot == null)
        {
            bigRobot = GetComponent<BigRobot2D>();
        }

        if (bigRobot == null)
        {
            bigRobot = GetComponentInParent<BigRobot2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (bodySpriteRenderer == null && animator != null)
        {
            bodySpriteRenderer = animator.GetComponent<SpriteRenderer>();
        }

        if (scaleTransform == null && animator != null)
        {
            scaleTransform = animator.transform;
        }

        if (scaleTransform == null)
        {
            Transform textures = transform.Find("Textures");
            scaleTransform = textures != null ? textures : transform;
        }

        idleStateHash = Animator.StringToHash(idleStateName);
        attackStateHash = Animator.StringToHash(attackStateName);
    }

    private void OnEnable()
    {
        lastAttackIntent = false;
        lastAttackAnimVersion = -1;
        lastAttackTransform = false;
        shutdownApplied = false;

        if (bigRobot != null && bigRobot.IsShutdown)
        {
            ApplyShutdownVisual();
            return;
        }

        ApplySpriteAnimation(true);
        ApplyTransform(ShouldUseAttackTransform(), true);
    }

    private void LateUpdate()
    {
        if (bigRobot == null)
        {
            return;
        }

        if (bigRobot.IsShutdown)
        {
            ApplyShutdownVisual();
            return;
        }

        ApplySpriteAnimation(false);

        bool attackTransform = ShouldUseAttackTransform();
        ApplyTransform(attackTransform, false);
    }

    public void ApplyShutdownVisual()
    {
        if (shutdownApplied)
        {
            return;
        }

        Sprite sprite = GetShutdownSprite();

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (bodySpriteRenderer != null && sprite != null)
        {
            bodySpriteRenderer.sprite = sprite;
        }

        if (scaleTransform != null)
        {
            scaleTransform.localScale = shutdownScale;
            scaleTransform.localPosition = shutdownPosition;
        }

        shutdownApplied = true;
    }

    private Sprite GetShutdownSprite()
    {
        if (shutdownSpriteAsset != null)
        {
            return shutdownSpriteAsset;
        }

        if (shutdownSprite != null)
        {
            return shutdownSprite;
        }

        if (!string.IsNullOrWhiteSpace(shutdownSpriteResourcePath))
        {
            shutdownSprite = Resources.Load<Sprite>(shutdownSpriteResourcePath);
        }

        return shutdownSprite;
    }

    private void ApplySpriteAnimation(bool force)
    {
        if (animator == null || bigRobot == null)
        {
            return;
        }

        bool attackIntent = bigRobot.IsInAttackSequence;
        int attackVersion = bigRobot.AttackAnimVersion;

        if (!force && attackIntent == lastAttackIntent && attackVersion == lastAttackAnimVersion)
        {
            return;
        }

        bool wasAttackIntent = lastAttackIntent;
        lastAttackIntent = attackIntent;
        animator.SetBool(IsAttackingHash, attackIntent);

        if (attackIntent)
        {
            if (force || attackVersion != lastAttackAnimVersion)
            {
                animator.Play(attackStateName, 0, 0f);
                lastAttackAnimVersion = attackVersion;
            }

            return;
        }

        lastAttackAnimVersion = -1;

        if (force || wasAttackIntent)
        {
            animator.CrossFade(idleStateName, 0.08f, 0, 0f);
        }
    }

    /// <summary>
    /// 按 Animator 实际所处状态决定 transform，而不是按 IsInAttackSequence 瞬时切换。
    /// 过渡期间：Attack→Idle 保持攻击 transform 直到离开攻击状态；Idle→Attack 过半后再切攻击 transform。
    /// </summary>
    private bool ShouldUseAttackTransform()
    {
        if (animator == null)
        {
            return bigRobot != null && bigRobot.IsInAttackSequence;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            AnimatorTransitionInfo transition = animator.GetAnimatorTransitionInfo(0);

            if (IsAttackState(next))
            {
                return transition.normalizedTime >= 0.5f;
            }

            if (IsAttackState(current))
            {
                return true;
            }

            return IsAttackState(next);
        }

        return IsAttackState(current);
    }

    private bool IsAttackState(AnimatorStateInfo stateInfo)
    {
        return stateInfo.shortNameHash == attackStateHash || stateInfo.IsName(attackStateName);
    }

    private void ApplyTransform(bool attacking, bool force)
    {
        if (scaleTransform == null)
        {
            return;
        }

        if (!force && attacking == lastAttackTransform)
        {
            return;
        }

        lastAttackTransform = attacking;
        scaleTransform.localScale = attacking ? attackScale : idleScale;
        scaleTransform.localPosition = attacking ? attackPosition : idlePosition;
    }
}
