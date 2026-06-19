using UnityEngine;

public class StalactiteStone2 : MonoBehaviour
{
    [Header("Fall")]
    [Tooltip("World units per second.")]
    public float fallSpeed = 6f;

    [Header("Damage")]
    public float damageAmount = 10f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float minUpwardGroundNormal = 0.5f;

    private Rigidbody2D rb;
    private Collider2D stoneCollider;
    private readonly Collider2D[] overlapBuffer = new Collider2D[8];
    private bool isFalling;
    private bool hasLandedOrHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stoneCollider = GetComponent<Collider2D>();
        ResolvePlayerLayerMask();
        ResolveGroundLayerMask();
    }

    private void OnValidate()
    {
        ResolvePlayerLayerMask();
        ResolveGroundLayerMask();
    }

    public void BeginFall(float speed)
    {
        if (speed > 0f)
        {
            fallSpeed = speed;
        }

        isFalling = true;
        hasLandedOrHit = false;

        if (rb == null)
        {
            return;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = Vector2.down * fallSpeed;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        TryDamageOverlappingPlayers();
        TryLandOnGroundBelow();
    }

    private void FixedUpdate()
    {
        if (!isFalling || hasLandedOrHit || rb == null)
        {
            return;
        }

        rb.velocity = Vector2.down * fallSpeed;
        TryLandOnGroundBelow();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleTrigger(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLandedOrHit || collision.collider == null)
        {
            return;
        }

        if (!IsLayerInMask(collision.collider.gameObject.layer, groundLayerMask))
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.normal.y >= minUpwardGroundNormal)
            {
                LandOnGround();
                return;
            }
        }
    }

    private void HandleTrigger(Collider2D other)
    {
        if (hasLandedOrHit || other == null)
        {
            return;
        }

        if (IsGroundCollider(other))
        {
            LandOnGround();
            return;
        }

        if (IsPlayerCollider(other))
        {
            TryDealDamageToPlayer(other);
        }
    }

    private void TryLandOnGroundBelow()
    {
        if (stoneCollider == null)
        {
            return;
        }

        Bounds bounds = stoneCollider.bounds;
        Vector2 rayOrigin = new Vector2(bounds.center.x, bounds.min.y);
        float checkDistance = Mathf.Max(
            groundCheckDistance,
            fallSpeed * Time.fixedDeltaTime + 0.02f);

        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            checkDistance,
            groundLayerMask);

        if (hit.collider == null)
        {
            return;
        }

        if (hit.normal.y < minUpwardGroundNormal)
        {
            return;
        }

        LandOnGround();
    }

    private void TryDamageOverlappingPlayers()
    {
        if (stoneCollider == null)
        {
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = true;
        filter.SetLayerMask(playerLayer);

        int count = Physics2D.OverlapCollider(stoneCollider, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D overlap = overlapBuffer[i];
            if (overlap != null)
            {
                TryDealDamageToPlayer(overlap);
            }
        }

        TryDamageCurrentPlayerByOverlapPoint();
    }

    private void TryDamageCurrentPlayerByOverlapPoint()
    {
        if (PlayerManager.Instance == null || stoneCollider == null)
        {
            return;
        }

        Player player = PlayerManager.Instance.TryGetCurrentPlayer();
        if (player == null)
        {
            return;
        }

        if (!stoneCollider.OverlapPoint(player.transform.position))
        {
            return;
        }

        TryDealDamageToPlayer(player);
    }

    private bool TryDealDamageToPlayer(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return false;
        }

        Player player = other.GetComponentInParent<Player>();
        if (IsPlayerProtectedFromTrap(player))
        {
            return false;
        }

        PlayerVitals vitals = ResolvePlayerVitals(other);
        if (vitals == null || vitals.IsDead || damageAmount <= 0f)
        {
            return false;
        }

        vitals.TakeDamage(damageAmount);
        return true;
    }

    private bool TryDealDamageToPlayer(Player player)
    {
        if (player == null || IsPlayerProtectedFromTrap(player))
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();
        if (vitals == null)
        {
            vitals = player.GetComponentInChildren<PlayerVitals>();
        }

        if (vitals == null || vitals.IsDead || damageAmount <= 0f)
        {
            return false;
        }

        vitals.TakeDamage(damageAmount);
        return true;
    }

    private bool IsPlayerProtectedFromTrap(Player player)
    {
        if (player == null)
        {
            return false;
        }

        MinerHelmetPassiveEffect minerHelmet =
            player.GetComponentInChildren<MinerHelmetPassiveEffect>(true);
        if (minerHelmet != null && minerHelmet.hasHeadProtection)
        {
            return true;
        }

        BreastplatePassiveEffect breastplate =
            player.GetComponentInChildren<BreastplatePassiveEffect>(true);
        if (breastplate != null && breastplate.trapProof)
        {
            return true;
        }

        return false;
    }

    private PlayerVitals ResolvePlayerVitals(Collider2D other)
    {
        PlayerVitals vitals = other.GetComponentInParent<PlayerVitals>();
        if (vitals != null)
        {
            return vitals;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            return null;
        }

        vitals = player.GetComponent<PlayerVitals>();
        if (vitals != null)
        {
            return vitals;
        }

        return player.GetComponentInChildren<PlayerVitals>();
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null)
        {
            return true;
        }

        if (playerLayer.value != 0)
        {
            return IsLayerInMask(other.gameObject.layer, playerLayer);
        }

        return other.GetComponentInParent<PlayerVitals>() != null;
    }

    private bool IsGroundCollider(Collider2D other)
    {
        return IsLayerInMask(other.gameObject.layer, groundLayerMask);
    }

    private void LandOnGround()
    {
        StopMovement();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private void StopMovement()
    {
        isFalling = false;
        hasLandedOrHit = true;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void ResolvePlayerLayerMask()
    {
        if (playerLayer.value != 0)
        {
            return;
        }

        int playerLayerIndex = LayerMask.NameToLayer("Player");
        if (playerLayerIndex >= 0)
        {
            playerLayer = 1 << playerLayerIndex;
        }
    }

    private void ResolveGroundLayerMask()
    {
        if (groundLayerMask.value != 0)
        {
            return;
        }

        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex >= 0)
        {
            groundLayerMask = 1 << groundLayerIndex;
        }
    }

    private static bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (stoneCollider == null)
        {
            stoneCollider = GetComponent<Collider2D>();
        }

        if (stoneCollider == null)
        {
            return;
        }

        Bounds bounds = stoneCollider.bounds;
        Vector2 rayOrigin = new Vector2(bounds.center.x, bounds.min.y);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            rayOrigin,
            rayOrigin + Vector2.down * groundCheckDistance);
    }
}
