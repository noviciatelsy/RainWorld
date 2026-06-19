using UnityEngine;

/// <summary>
/// 狼蛛精灵动画：起跳时 WolfspiderJump，咬击瞬间 WolfspiderAttack，其余保持待机贴图。
/// </summary>
[DisallowMultipleComponent]
public class WolfSpiderAni : MonoBehaviour
{
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    [Header("Animator States")]
    public string idleStateName = "WolfspiderIdle";
    public string jumpStateName = "WolfspiderJump";
    public string attackStateName = "WolfspiderAttack";

    [Header("References")]
    public WolfSpider2D spider;
    public Animator animator;

    [Header("Playback")]
    public float crossFadeDuration = 0.05f;

    private Sprite idleSprite;
    private bool lastAttackAnim;
    private bool lastJumpAnim;

    private void Awake()
    {
        if (spider == null)
        {
            spider = GetComponent<WolfSpider2D>();
        }

        if (spider == null)
        {
            spider = GetComponentInParent<WolfSpider2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        SpriteRenderer spriteRenderer = animator != null
            ? animator.GetComponent<SpriteRenderer>()
            : null;

        if (spriteRenderer != null)
        {
            idleSprite = spriteRenderer.sprite;
        }
    }

    private void OnEnable()
    {
        lastAttackAnim = false;
        lastJumpAnim = false;
        ApplySpriteAnimation(force: true);
    }

    private void LateUpdate()
    {
        if (spider == null || spider.IsStompPaused)
        {
            return;
        }

        ApplySpriteAnimation(force: false);
    }

    public void NotifyJumpStarted()
    {
        if (animator == null)
        {
            return;
        }

        lastJumpAnim = false;
        lastAttackAnim = false;
        animator.SetBool(IsAttackingHash, false);
        animator.SetBool(IsJumpingHash, true);
        animator.CrossFade(jumpStateName, 0f, 0, 0f);
        lastJumpAnim = true;
    }

    public void NotifyJumpEnded()
    {
        if (animator == null || spider != null && spider.IsJumping)
        {
            return;
        }

        lastJumpAnim = false;
        animator.SetBool(IsJumpingHash, false);
        animator.CrossFade(idleStateName, crossFadeDuration, 0, 0f);
        RestoreIdleSprite();
    }

    public void NotifyAttackStarted()
    {
        if (animator == null)
        {
            return;
        }

        lastAttackAnim = false;
        lastJumpAnim = false;
        animator.SetBool(IsJumpingHash, false);
        animator.SetBool(IsAttackingHash, true);
        animator.CrossFade(attackStateName, 0f, 0, 0f);
        lastAttackAnim = true;
    }

    public void NotifyAttackAnimEnded()
    {
        if (animator == null)
        {
            return;
        }

        lastAttackAnim = false;
        animator.SetBool(IsAttackingHash, false);
        animator.CrossFade(idleStateName, crossFadeDuration, 0, 0f);
        RestoreIdleSprite();
    }

    private void ApplySpriteAnimation(bool force)
    {
        if (animator == null || spider == null)
        {
            return;
        }

        bool attackAnim = spider.IsPerformingAttackAnim;
        bool jumpAnim = !attackAnim && spider.IsJumping;

        if (!force && attackAnim == lastAttackAnim && jumpAnim == lastJumpAnim)
        {
            return;
        }

        bool wasAction = lastAttackAnim || lastJumpAnim;
        lastAttackAnim = attackAnim;
        lastJumpAnim = jumpAnim;

        animator.SetBool(IsAttackingHash, attackAnim);
        animator.SetBool(IsJumpingHash, jumpAnim);

        if (attackAnim)
        {
            animator.CrossFade(attackStateName, crossFadeDuration, 0, 0f);
            return;
        }

        if (jumpAnim)
        {
            animator.CrossFade(jumpStateName, crossFadeDuration, 0, 0f);
            return;
        }

        animator.CrossFade(idleStateName, crossFadeDuration, 0, 0f);

        if (wasAction)
        {
            RestoreIdleSprite();
        }
    }

    private void RestoreIdleSprite()
    {
        if (idleSprite == null || animator == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = animator.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }
}
