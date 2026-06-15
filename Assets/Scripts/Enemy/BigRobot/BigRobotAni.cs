using UnityEngine;

/// <summary>
/// 大机器人动画：Idle / 攻击时切换 BigRobotIdle、BigRobotAttack1，以及 scaleTransform 的 scale 与 localPosition。
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

    [Tooltip("Idle / 攻击时切换 localScale、localPosition 的 Transform（通常为 Textures）")]
    public Transform scaleTransform;

    [Header("Scale")]
    public Vector3 idleScale = new Vector3(0.75f, 0.75f, 0.75f);
    public Vector3 attackScale = Vector3.one;

    [Header("Position")]
    public Vector3 idlePosition = Vector3.zero;
    public Vector3 attackPosition = new Vector3(-1f, 0.9f, 0f);

    private bool lastAttackAnim;
    private bool lastIdleAnim;

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

        if (scaleTransform == null && animator != null)
        {
            scaleTransform = animator.transform;
        }

        if (scaleTransform == null)
        {
            Transform textures = transform.Find("Textures");
            scaleTransform = textures != null ? textures : transform;
        }
    }

    private void OnEnable()
    {
        lastAttackAnim = false;
        lastIdleAnim = false;
        ApplySpriteAnimation(true);
        ApplyTransform(bigRobot != null && bigRobot.IsInAttackSequence);
    }

    private void LateUpdate()
    {
        if (bigRobot == null)
        {
            return;
        }

        bool attacking = bigRobot.IsInAttackSequence;
        ApplySpriteAnimation(false);
        ApplyTransform(attacking);
    }

    private void ApplySpriteAnimation(bool force)
    {
        if (animator == null || bigRobot == null)
        {
            return;
        }

        bool attackAnim = bigRobot.IsInAttackSequence;
        bool idleAnim = !attackAnim;

        if (!force && attackAnim == lastAttackAnim && idleAnim == lastIdleAnim)
        {
            return;
        }

        lastAttackAnim = attackAnim;
        lastIdleAnim = idleAnim;

        animator.SetBool(IsAttackingHash, attackAnim);

        if (attackAnim)
        {
            animator.CrossFade(attackStateName, 0.05f, 0, 0f);
            return;
        }

        if (idleAnim)
        {
            animator.CrossFade(idleStateName, 0.05f, 0, 0f);
        }
    }

    private void ApplyTransform(bool attacking)
    {
        if (scaleTransform == null)
        {
            return;
        }

        scaleTransform.localScale = attacking ? attackScale : idleScale;
        scaleTransform.localPosition = attacking ? attackPosition : idlePosition;
    }
}
