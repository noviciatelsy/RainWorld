using System.Collections;
using UnityEngine;

public class Ghost : MonsterBase, IMimicryReleasable, ITalismanExterminable
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    [Tooltip("绕行强度：0=直线，1≈绕玩家一圈，越大弯越紧")]
    public float spiralCurvature = 0.8f;
    [Tooltip("每隔多久重新规划一段螺旋路径")]
    public float pathPlanInterval = 2f;

    [Header("Combat")]
    public float attackRange = 0.9f;
    public int attackDamage = 30;
    [Tooltip("攻击后原地等待时间")]
    public float waitDuration = 1f;

    [Header("Mimicry")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float mimickedAlpha = 25f / 255f;
    [SerializeField] private float revealAlpha = 1f;
    [SerializeField] private float revealDuration = 0.35f;

    [Header("Talisman")]
    [SerializeField] private SpriteDissolveEffect dissolveEffect;
    [SerializeField] private float exterminateDissolveDuration = 1f;

    [Header("Debug")]
    public bool drawDebugGizmos = true;

    private bool isMimicReleased;
    private bool isExterminating;
    private Coroutine revealRoutine;

    private void Awake()
    {
        CacheReferences();
        ApplyMimickedVisualState();
    }

    protected override void Init()
    {
        ai = new ghostAI();
        motor = new ghostMotor();
    }

    public void ReleaseMimicry(Vector2 myMirrorPosition)
    {
        if (isMimicReleased || isExterminating)
        {
            return;
        }

        isMimicReleased = true;

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }

        revealRoutine = StartCoroutine(RevealMimicryRoutine());
    }

    public void ExterminateByTalisman(Vector2 myTalismanPosition)
    {
        if (isExterminating)
        {
            return;
        }

        isExterminating = true;
        SetStompPaused(true);

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        if (dissolveEffect != null)
        {
            dissolveEffect.PlayDissolveIn(
                exterminateDissolveDuration,
                () => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject, exterminateDissolveDuration);
        }
    }

    public bool IsPlayerInAttackRange(Transform playerTransform)
    {
        if (playerTransform == null || isExterminating)
        {
            return false;
        }

        return ((Vector2)playerTransform.position - Position).sqrMagnitude <= attackRange * attackRange;
    }

    public bool TryDamagePlayer(Transform playerTransform)
    {
        if (playerTransform == null || !IsPlayerInAttackRange(playerTransform))
        {
            return false;
        }

        Player player = playerTransform.GetComponentInParent<Player>();

        if (player == null)
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();

        if (vitals == null || vitals.IsDead)
        {
            return false;
        }

        if (GameStateManager.Instance != null
            && GameStateManager.Instance.currentGameState != GameState.Game)
        {
            return false;
        }

        return MonsterPlayerDamage.TryDealDamage(vitals, attackDamage);
    }

    private void CacheReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (dissolveEffect == null)
        {
            dissolveEffect = GetComponent<SpriteDissolveEffect>();
        }
    }

    private void ApplyMimickedVisualState()
    {
        if (isMimicReleased || isExterminating)
        {
            return;
        }

        if (spriteRenderer != null)
        {
            Color tint = spriteRenderer.color;
            tint.a = mimickedAlpha;
            spriteRenderer.color = tint;
        }

        if (dissolveEffect != null)
        {
            dissolveEffect.SetDissolveAmount(0f);
        }
    }

    private IEnumerator RevealMimicryRoutine()
    {
        if (spriteRenderer == null)
        {
            revealRoutine = null;
            yield break;
        }

        Color startColor = spriteRenderer.color;
        float startAlpha = startColor.a;
        float duration = Mathf.Max(0f, revealDuration);

        if (duration <= 0f)
        {
            Color revealedColor = startColor;
            revealedColor.a = revealAlpha;
            spriteRenderer.color = revealedColor;
            revealRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(startAlpha, revealAlpha, t);
            spriteRenderer.color = currentColor;
            yield return null;
        }

        Color finalColor = startColor;
        finalColor.a = revealAlpha;
        spriteRenderer.color = finalColor;
        revealRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (DebugPath != null && DebugPath.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < DebugPath.Count - 1; i++)
            {
                Gizmos.DrawLine(DebugPath[i], DebugPath[i + 1]);
            }
        }
    }
}
