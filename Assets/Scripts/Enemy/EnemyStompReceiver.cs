using System.Collections;
using UnityEngine;

/// <summary>
/// 怪物踩头：整体 prefab 压扁、暂停 AI/Motor、玩家弹跳，一段时间后恢复。
/// </summary>
[DisallowMultipleComponent]
public class EnemyStompReceiver : MonoBehaviour
{
    private const string StompPlatformName = "StompPlatform";

    [SerializeField] private MonsterBase monster;
    [SerializeField] private Transform headAnchor;
    [SerializeField] private float squashScaleY = 0.3f;
    [SerializeField] private float recoverDuration = 2f;
    [SerializeField] private float stompBounceImpulse = 0f;
    [SerializeField] private Vector2 stompPlatformSize = new Vector2(0.8f, 0.12f);

    public bool IsStomped { get; private set; }

    private Vector3 baseScale;
    private Coroutine recoverRoutine;

    private void Awake()
    {
        if (monster == null)
        {
            monster = GetComponent<MonsterBase>();
        }
    }

    public static EnemyStompReceiver Ensure(
        MonsterBase owner,
        Transform headAnchorTransform,
        Vector2 platformSize)
    {
        if (owner == null)
        {
            return null;
        }

        EnemyStompReceiver receiver = owner.GetComponent<EnemyStompReceiver>();

        if (receiver == null)
        {
            receiver = owner.gameObject.AddComponent<EnemyStompReceiver>();
        }

        receiver.monster = owner;
        receiver.headAnchor = headAnchorTransform != null ? headAnchorTransform : owner.transform;
        receiver.stompPlatformSize = platformSize;
        receiver.baseScale = owner.transform.localScale;
        receiver.CleanupLegacyPlatforms();
        receiver.SetupStompPlatform();
        receiver.RegisterBodyColliders();

        return receiver;
    }

    private void CleanupLegacyPlatforms()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null || child == transform)
            {
                continue;
            }

            if (child.name != "EnemyStompPlatform")
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    public bool TryApplyStomp(Player player, Collider2D stompCollider, Vector2 relativeVelocity)
    {
        if (IsStomped || player == null)
        {
            return false;
        }

        ApplyStomp(player);
        return true;
    }

    private void ApplyStomp(Player player)
    {
        IsStomped = true;

        if (monster != null)
        {
            monster.SetStompPaused(true);
            EnemyAudioEmitter audioEmitter = monster.GetComponent<EnemyAudioEmitter>();
            if (audioEmitter != null)
            {
                audioEmitter.NotifyStomped();
            }
        }

        Vector3 squashed = baseScale;
        squashed.y = baseScale.y * squashScaleY;
        transform.localScale = squashed;

        PlayerControl playerControl = player.GetComponent<PlayerControl>();

        if (playerControl != null)
        {
            playerControl.ApplyStompBounce(stompBounceImpulse);
        }

        if (recoverRoutine != null)
        {
            StopCoroutine(recoverRoutine);
        }

        recoverRoutine = StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        yield return new WaitForSeconds(recoverDuration);

        Vector3 restored = baseScale;
        restored.x = transform.localScale.x;
        restored.z = transform.localScale.z;
        transform.localScale = restored;

        if (monster != null)
        {
            monster.SetStompPaused(false);
        }

        IsStomped = false;
        recoverRoutine = null;
    }

    private void SetupStompPlatform()
    {
        if (headAnchor == null)
        {
            headAnchor = transform;
        }

        Transform platformTransform = headAnchor.Find(StompPlatformName);
        GameObject platformObject;

        if (platformTransform != null)
        {
            platformObject = platformTransform.gameObject;
        }
        else
        {
            platformObject = new GameObject(StompPlatformName);
            platformObject.transform.SetParent(headAnchor, false);
        }

        float topY = EnemyStompUtility.GetLocalSpriteTopY(headAnchor);
        float platformHalfHeight = stompPlatformSize.y * 0.5f;
        platformObject.transform.localPosition = new Vector3(0f, topY - platformHalfHeight, 0f);
        platformObject.transform.localRotation = Quaternion.identity;
        platformObject.transform.localScale = Vector3.one;

        BoxCollider2D box = platformObject.GetComponent<BoxCollider2D>();

        if (box == null)
        {
            box = platformObject.AddComponent<BoxCollider2D>();
        }

        box.size = stompPlatformSize;
        box.usedByEffector = true;

        if (platformObject.GetComponent<EnemyStompPlatform>() == null)
        {
            platformObject.AddComponent<EnemyStompPlatform>();
        }
    }

    private void RegisterBodyColliders()
    {
        if (monster == null)
        {
            return;
        }

        Collider2D[] colliders = monster.GetComponents<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            EnemyStompBodyForwarder forwarder = collider.GetComponent<EnemyStompBodyForwarder>();

            if (forwarder == null)
            {
                forwarder = collider.gameObject.AddComponent<EnemyStompBodyForwarder>();
            }

            forwarder.Initialize(this, collider);
        }
    }
}
