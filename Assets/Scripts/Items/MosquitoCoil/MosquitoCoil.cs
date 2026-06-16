using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MosquitoCoil : MonoBehaviour
{
    [Header("References")]
    private Transform detectionCenter;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask repellableMonsterLayerMask;

    [SerializeField] private float repelRadius = 3f;
    [SerializeField] private float detectInterval = 1f;
    [SerializeField] private float activeDuration = 60f;
    [SerializeField] private bool detectImmediatelyOnUse = true;

    [Header("Reuse Settings")]
    [SerializeField] private bool restartDurationWhenUsedAgain = true;

    [Header("Visual")]
    [SerializeField] private WaveLightEffect waveLightEffectPrefab;
    [SerializeField] private int effectCenterAlpha = 40;
    [SerializeField] private int effectWaveStartAlpha = 40;
    [SerializeField] private Color effectColor = Color.white;
    [SerializeField] private float effectWavePeriod = 1f;
    [SerializeField] private float effectWaveExpandDuration = 1.5f;

    public bool IsActive => activeRoutine != null;

    public Vector2 CenterPosition => GetDetectionCenterPosition();

    public float Radius => repelRadius;

    private Coroutine activeRoutine;
    private WaveLightEffect activeWaveLightEffect;

    private void Awake()
    {
        detectionCenter = transform;
    }

    private void OnDisable()
    {
        StopMosquitoCoil();
    }

    /// <summary>
    /// 使用蚊香。
    /// </summary>
    public bool UseMosquitoCoil()
    {
        if (activeRoutine != null)
        {
            if (!restartDurationWhenUsedAgain)
            {
                return false;
            }

            StopCoroutine(activeRoutine);
            activeRoutine = null;
            MosquitoCoilRegistry.Unregister(this);
            ClearWaveLightEffect();
        }

        activeRoutine = StartCoroutine(MosquitoCoilRoutine());
        return true;
    }

    /// <summary>
    /// 停止蚊香效果并清理视觉。
    /// </summary>
    public void StopMosquitoCoil()
    {
        if (activeRoutine == null)
        {
            return;
        }

        StopCoroutine(activeRoutine);
        activeRoutine = null;
        MosquitoCoilRegistry.Unregister(this);
        ClearWaveLightEffect();
    }

    private void SpawnWaveLightEffect()
    {
        if (waveLightEffectPrefab == null)
        {
            Debug.LogWarning($"{nameof(MosquitoCoil)}: waveLightEffectPrefab 未配置。", this);
            return;
        }

        Transform playerTransform = ResolvePlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogWarning($"{nameof(MosquitoCoil)}: 未找到 Tag 为 Player 的对象。", this);
            return;
        }

        ClearWaveLightEffect();

        activeWaveLightEffect = Instantiate(waveLightEffectPrefab, playerTransform);
        activeWaveLightEffect.transform.localPosition = Vector3.zero;
        activeWaveLightEffect.transform.localRotation = Quaternion.identity;
        activeWaveLightEffect.PlayAttached(
            repelRadius,
            activeDuration,
            effectCenterAlpha,
            effectWaveStartAlpha,
            effectColor,
            effectWavePeriod,
            effectWaveExpandDuration);
    }

    private Transform ResolvePlayerTransform()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                return current;
            }

            current = current.parent;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }

    private void ClearWaveLightEffect()
    {
        if (activeWaveLightEffect == null)
        {
            return;
        }

        activeWaveLightEffect.StopEffect();
        Destroy(activeWaveLightEffect.gameObject);
        activeWaveLightEffect = null;
    }

    public bool IsPointInsideRadius(Vector2 worldPoint)
    {
        float radius = Mathf.Max(0f, repelRadius);
        return (worldPoint - CenterPosition).sqrMagnitude < radius * radius;
    }

    private IEnumerator MosquitoCoilRoutine()
    {
        MosquitoCoilRegistry.Register(this);
        SpawnWaveLightEffect();

        float safeDetectInterval = Mathf.Max(0.02f, detectInterval);
        float elapsedTime = 0f;

        if (detectImmediatelyOnUse)
        {
            DetectAndRepelMonsters();
        }

        while (elapsedTime < activeDuration)
        {
            yield return new WaitForSeconds(safeDetectInterval);

            elapsedTime += safeDetectInterval;

            if (elapsedTime > activeDuration)
            {
                break;
            }

            DetectAndRepelMonsters();
        }

        MosquitoCoilRegistry.Unregister(this);
        ClearWaveLightEffect();
        activeRoutine = null;
    }

    private void DetectAndRepelMonsters()
    {
        Vector2 centerPosition = GetDetectionCenterPosition();

        Collider2D[] detectedColliders = Physics2D.OverlapCircleAll(
            centerPosition,
            repelRadius,
            repellableMonsterLayerMask);

        HashSet<MonoBehaviour> triggeredTargets = new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IMosquitoCoilRepellable>(detectedColliders[i]);

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            if (interfaceBehaviour is IMosquitoCoilRepellable repellableMonster)
            {
                repellableMonster.RepelByMosquitoCoil(centerPosition);
            }
        }
    }

    private MonoBehaviour FindInterfaceBehaviourInParents<T>(Collider2D myCollider) where T : class
    {
        MonoBehaviour[] parentBehaviours = myCollider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is T)
            {
                return parentBehaviours[i];
            }
        }

        return null;
    }

    private Vector2 GetDetectionCenterPosition()
    {
        return detectionCenter != null ? detectionCenter.position : transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centerPosition = detectionCenter != null ? detectionCenter.position : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centerPosition, repelRadius);
    }
}
