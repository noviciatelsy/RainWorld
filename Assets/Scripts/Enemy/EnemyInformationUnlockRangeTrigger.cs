using System.Collections;
using UnityEngine;

/// <summary>
/// 怪物激活后，在自身中心生成圆形触发器；玩家进入时解锁对应敌人图鉴。
/// </summary>
[DisallowMultipleComponent]
public class EnemyInformationUnlockRangeTrigger : MonoBehaviour
{
    private const string TriggerChildName = "EnemyInformationUnlockTrigger";
    private const string TriggerLayerName = "CanCollideWithPlayer";

    [SerializeField] private EnemyInformationDataSO enemyInformationData;
    [SerializeField] private float triggerRadius = 4f;

    private CircleCollider2D rangeCollider;
    private Coroutine overlapCheckRoutine;

    public static EnemyInformationUnlockRangeTrigger Ensure(
        MonsterBase owner,
        EnemyInformationDataSO informationData = null,
        float radius = 4f)
    {
        if (owner == null)
        {
            return null;
        }

        return Ensure(owner.gameObject, informationData, radius);
    }

    public static EnemyInformationUnlockRangeTrigger Ensure(
        GameObject owner,
        EnemyInformationDataSO informationData = null,
        float radius = 4f)
    {
        if (owner == null)
        {
            return null;
        }

        EnemyInformationUnlockRangeTrigger trigger = owner.GetComponent<EnemyInformationUnlockRangeTrigger>();

        if (trigger == null)
        {
            trigger = owner.AddComponent<EnemyInformationUnlockRangeTrigger>();
        }

        trigger.Configure(informationData, radius);
        return trigger;
    }

    public void Configure(EnemyInformationDataSO informationData, float radius)
    {
        if (informationData != null)
        {
            enemyInformationData = informationData;
        }

        if (radius > 0f)
        {
            triggerRadius = radius;
        }

        if (isActiveAndEnabled)
        {
            RefreshTriggerState();
        }
    }

    private void OnEnable()
    {
        RefreshTriggerState();
    }

    private void OnDisable()
    {
        StopOverlapCheckRoutine();
    }

    internal void HandlePlayerEntered(Collider2D collision)
    {
        if (collision == null || collision.GetComponentInParent<Player>() == null)
        {
            return;
        }

        TryUnlock();
    }

    private void RefreshTriggerState()
    {
        StopOverlapCheckRoutine();

        if (enemyInformationData == null || IsAlreadyUnlocked())
        {
            DisableRangeCollider();
            return;
        }

        EnsureRangeCollider();
        overlapCheckRoutine = StartCoroutine(CheckPlayerOverlapAfterPhysicsSync());
    }

    private void TryUnlock()
    {
        if (enemyInformationData == null || IsAlreadyUnlocked())
        {
            return;
        }

        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null)
        {
            return;
        }

        if (archiveManager.UnlockEnemy(enemyInformationData))
        {
            DisableRangeCollider();
        }
    }

    private bool IsAlreadyUnlocked()
    {
        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null || enemyInformationData == null)
        {
            return false;
        }

        return archiveManager.IsEnemyUnlocked(enemyInformationData);
    }

    private void EnsureRangeCollider()
    {
        if (rangeCollider == null)
        {
            Transform existing = transform.Find(TriggerChildName);

            if (existing != null)
            {
                rangeCollider = existing.GetComponent<CircleCollider2D>();
            }
        }

        if (rangeCollider == null)
        {
            int triggerLayer = LayerMask.NameToLayer(TriggerLayerName);

            if (triggerLayer < 0)
            {
                Debug.LogWarning(
                    $"{nameof(EnemyInformationUnlockRangeTrigger)} 找不到 Layer「{TriggerLayerName}」，无法创建解锁触发器：{name}",
                    this
                );
                return;
            }

            GameObject triggerObject = new GameObject(TriggerChildName);
            triggerObject.transform.SetParent(transform, false);
            triggerObject.transform.localPosition = Vector3.zero;
            triggerObject.layer = triggerLayer;

            Rigidbody2D triggerBody = triggerObject.AddComponent<Rigidbody2D>();
            triggerBody.bodyType = RigidbodyType2D.Kinematic;
            triggerBody.gravityScale = 0f;
            triggerBody.simulated = true;

            rangeCollider = triggerObject.AddComponent<CircleCollider2D>();
            TriggerForwarder forwarder = triggerObject.AddComponent<TriggerForwarder>();
            forwarder.Initialize(this);
        }

        rangeCollider.isTrigger = true;
        rangeCollider.enabled = true;
        rangeCollider.radius = Mathf.Max(0.1f, triggerRadius);
    }

    private void DisableRangeCollider()
    {
        if (rangeCollider != null)
        {
            rangeCollider.enabled = false;
        }
    }

    private IEnumerator CheckPlayerOverlapAfterPhysicsSync()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        overlapCheckRoutine = null;

        if (!isActiveAndEnabled || enemyInformationData == null || IsAlreadyUnlocked())
        {
            yield break;
        }

        TryUnlockViaDistanceCheck();
    }

    private void TryUnlockViaDistanceCheck()
    {
        Player player = ResolvePlayer();

        if (player == null)
        {
            return;
        }

        Vector2 center = rangeCollider != null
            ? rangeCollider.transform.position
            : transform.position;

        if (Vector2.Distance(center, player.transform.position) > triggerRadius)
        {
            return;
        }

        TryUnlock();
    }

    private static Player ResolvePlayer()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayer != null)
        {
            return PlayerManager.Instance.CurrentPlayer;
        }

        return Object.FindObjectOfType<Player>();
    }

    private void StopOverlapCheckRoutine()
    {
        if (overlapCheckRoutine == null)
        {
            return;
        }

        StopCoroutine(overlapCheckRoutine);
        overlapCheckRoutine = null;
    }

    private sealed class TriggerForwarder : MonoBehaviour
    {
        private EnemyInformationUnlockRangeTrigger owner;

        public void Initialize(EnemyInformationUnlockRangeTrigger triggerOwner)
        {
            owner = triggerOwner;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            owner?.HandlePlayerEntered(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            owner?.HandlePlayerEntered(collision);
        }
    }
}
