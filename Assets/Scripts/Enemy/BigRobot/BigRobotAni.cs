using System.Collections;
using UnityEngine;

/// <summary>
/// 大机器人动画：Idle / 攻击切换 sprite；scale/position 随状态瞬间切换（攻击用 Play 避免 crossfade 导致的大小乱跳）。
/// 关机时瞬间切到 shutdownScale，播放 BigRobotOff 并停在最后一帧。
/// </summary>
[DisallowMultipleComponent]
public class BigRobotAni : MonoBehaviour
{
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    [Header("Animator States")]
    public string idleStateName = "BigRobotIdle";
    public string attackStateName = "BigRobotAttack1";
    public string shutdownStateName = "BigRobotOff";

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
    public Vector3 shutdownScale = new Vector3(0.65f, 0.65f, 0.65f);
    public Vector3 shutdownPosition = Vector3.zero;

    [Header("Shutdown Fallback")]
    [SerializeField] private Sprite shutdownSpriteAsset;
    [SerializeField] private string shutdownSpriteResourcePath = "textures/敌人资源/大机器人/关机动画/图层_57-removebg-preview";

    private int idleStateHash;
    private int attackStateHash;
    private int shutdownStateHash;
    private bool lastAttackIntent;
    private int lastAttackAnimVersion = -1;
    private bool lastAttackTransform;
    private bool shutdownApplied;
    private Sprite shutdownSprite;
    private Coroutine shutdownCoroutine;

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
        shutdownStateHash = Animator.StringToHash(shutdownStateName);
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
        ApplyTransformFromAnimator(true);
    }

    private void LateUpdate()
    {
        if (bigRobot == null)
        {
            return;
        }

        if (bigRobot.IsShutdown)
        {
            if (!shutdownApplied)
            {
                ApplyShutdownVisual();
            }

            return;
        }

        ApplySpriteAnimation(false);
        ApplyTransformFromAnimator(false);
    }

    public void ApplyShutdownVisual()
    {
        if (shutdownApplied)
        {
            return;
        }

        shutdownApplied = true;

        if (shutdownCoroutine != null)
        {
            StopCoroutine(shutdownCoroutine);
            shutdownCoroutine = null;
        }

        ApplyShutdownTransform();

        if (animator != null && HasShutdownState())
        {
            animator.enabled = true;
            animator.SetBool(IsAttackingHash, false);
            animator.Play(shutdownStateName, 0, 0f);
            shutdownCoroutine = StartCoroutine(HoldShutdownAnimationEnd());
            return;
        }

        ApplyShutdownSpriteFallback();
    }

    private bool HasShutdownState()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        return animator.HasState(0, shutdownStateHash);
    }

    private void ApplyShutdownTransform()
    {
        if (scaleTransform == null)
        {
            return;
        }

        scaleTransform.localScale = shutdownScale;
        scaleTransform.localPosition = shutdownPosition;
    }

    private IEnumerator HoldShutdownAnimationEnd()
    {
        yield return null;

        if (animator == null)
        {
            yield break;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float length = state.length > 0.01f ? state.length : 4.75f;
        yield return new WaitForSeconds(length);

        if (animator != null)
        {
            animator.enabled = false;
        }

        shutdownCoroutine = null;
    }

    private void ApplyShutdownSpriteFallback()
    {
        Sprite sprite = GetShutdownSprite();

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (bodySpriteRenderer != null && sprite != null)
        {
            bodySpriteRenderer.sprite = sprite;
        }

        ApplyShutdownTransform();
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

    private void ApplyTransformFromAnimator(bool force)
    {
        if (scaleTransform == null || animator == null)
        {
            return;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            float t = Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime);

            if (IsAttackState(next) && !IsAttackState(current))
            {
                scaleTransform.localScale = Vector3.Lerp(idleScale, attackScale, t);
                scaleTransform.localPosition = Vector3.Lerp(idlePosition, attackPosition, t);
            }
            else if (IsAttackState(current) && !IsAttackState(next))
            {
                scaleTransform.localScale = Vector3.Lerp(attackScale, idleScale, t);
                scaleTransform.localPosition = Vector3.Lerp(attackPosition, idlePosition, t);
            }
            else
            {
                bool attacking = IsAttackState(current);
                scaleTransform.localScale = attacking ? attackScale : idleScale;
                scaleTransform.localPosition = attacking ? attackPosition : idlePosition;
            }

            lastAttackTransform = IsAttackState(next);
            return;
        }

        ApplyTransform(IsAttackState(animator.GetCurrentAnimatorStateInfo(0)), force);
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
