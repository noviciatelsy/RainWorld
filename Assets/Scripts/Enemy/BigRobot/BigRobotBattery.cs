using UnityEngine;

/// <summary>
/// 大机器人电池：玩家从上方踩下后压扁、切换短路贴图，并通知机器人停机。
/// </summary>
[DisallowMultipleComponent]
public class BigRobotBattery : MonoBehaviour
{
    [SerializeField] private BigRobot2D bigRobot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D stompCollider;

    [Header("Sprites")]
    [SerializeField] private Sprite brokenSpriteAsset;
    [SerializeField] private string normalSpriteResourcePath = "textures/敌人资源/大机器人/电池";
    [SerializeField] private string brokenSpriteResourcePath = "textures/敌人资源/大机器人/短路电池";

    [Header("Stomp")]
    [SerializeField] private float stompSquashScaleY = 0.4f;
    [SerializeField] private float minStompDownSpeed = 0.5f;
    [SerializeField] private float topContactTolerance = 0.12f;
    [SerializeField] private float stompBounceImpulse = 0f;

    private Sprite normalSprite;
    private Sprite brokenSprite;
    private Vector3 baseScale;
    private bool isBroken;

    public bool IsBroken => isBroken;

    private void Awake()
    {
        if (bigRobot == null)
        {
            bigRobot = GetComponentInParent<BigRobot2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (stompCollider == null)
        {
            stompCollider = GetComponent<Collider2D>();
        }

        ResolveSprites();

        if (spriteRenderer != null)
        {
            baseScale = spriteRenderer.transform.localScale;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStomp(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryStomp(collision);
    }

    private void TryStomp(Collision2D collision)
    {
        if (isBroken || collision == null || stompCollider == null)
        {
            return;
        }

        Collider2D other = collision.collider;

        if (other == null || other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        if (collision.relativeVelocity.y > -minStompDownSpeed)
        {
            return;
        }

        if (other.bounds.min.y < stompCollider.bounds.max.y - topContactTolerance)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        BreakFromStomp(player);
    }

    public void BreakFromStomp(Player player)
    {
        if (isBroken)
        {
            return;
        }

        isBroken = true;

        if (spriteRenderer != null)
        {
            Vector3 squashed = baseScale;
            squashed.y = baseScale.y * stompSquashScaleY;
            spriteRenderer.transform.localScale = squashed;

            Sprite broken = GetBrokenSprite();

            if (broken != null)
            {
                spriteRenderer.sprite = broken;
            }
        }

        if (stompCollider != null)
        {
            stompCollider.enabled = false;
        }

        PlatformEffector2D effector = GetComponent<PlatformEffector2D>();

        if (effector != null)
        {
            effector.enabled = false;
        }

        if (player != null)
        {
            PlayerControl playerControl = player.GetComponent<PlayerControl>();

            if (playerControl != null)
            {
                playerControl.ApplyStompBounce(stompBounceImpulse);
            }
        }

        if (bigRobot != null)
        {
            bigRobot.NotifyBatteryBroken();
        }
    }

    private Sprite GetBrokenSprite()
    {
        if (brokenSpriteAsset != null)
        {
            return brokenSpriteAsset;
        }

        if (brokenSprite != null)
        {
            return brokenSprite;
        }

        if (!string.IsNullOrWhiteSpace(brokenSpriteResourcePath))
        {
            brokenSprite = Resources.Load<Sprite>(brokenSpriteResourcePath);
        }

        return brokenSprite;
    }

    private void ResolveSprites()
    {
        if (normalSprite == null && !string.IsNullOrWhiteSpace(normalSpriteResourcePath))
        {
            normalSprite = Resources.Load<Sprite>(normalSpriteResourcePath);
        }

        if (brokenSprite == null && brokenSpriteAsset == null && !string.IsNullOrWhiteSpace(brokenSpriteResourcePath))
        {
            brokenSprite = Resources.Load<Sprite>(brokenSpriteResourcePath);
        }

        if (spriteRenderer != null && normalSprite != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }
}
