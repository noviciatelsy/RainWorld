using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MosquitoCoil : MonoBehaviour
{
    [Header("References")]
    private Transform detectionCenter;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask repellableMonsterLayerMask;
    // 可被蚊香驱赶的怪物所在 Layer

    [SerializeField] private float repelRadius = 3f;
    // 蚊香检测半径

    [SerializeField] private float detectInterval =1f;
    // 每隔多久检测一次

    [SerializeField] private float activeDuration = 60f;
    // 蚊香持续时间

    [SerializeField] private bool detectImmediatelyOnUse = true;
    // 使用后是否立刻检测一次


    [Header("Reuse Settings")]
    [SerializeField] private bool restartDurationWhenUsedAgain = true;
    // 蚊香正在生效时再次使用，是否重新计时
   

    public bool IsActive
    {
        get
        {
            return activeRoutine != null;
        }
    }


    private Coroutine activeRoutine;
    // 当前生效协程

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
    /// <returns>
    /// 是否成功使用。
    /// </returns>
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
        }

        activeRoutine = StartCoroutine
        (
            MosquitoCoilRoutine()
        );

        return true;
    }


    /// <summary>
    /// 手动停止蚊香效果。
    /// 适合玩家死亡、切场景、道具被强制取消时调用。
    /// </summary>
    public void StopMosquitoCoil()
    {
        if (activeRoutine == null)
        {
            return;
        }

        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }


    private IEnumerator MosquitoCoilRoutine()
    {
        float safeDetectInterval =
            Mathf.Max(0.02f, detectInterval);

        float elapsedTime = 0f;

        if (detectImmediatelyOnUse)
        {
            DetectAndRepelMonsters();
        }

        while (elapsedTime < activeDuration)
        {
            yield return new WaitForSeconds
            (
                safeDetectInterval
            );

            elapsedTime += safeDetectInterval;

            if (elapsedTime > activeDuration)
            {
                break;
            }

            DetectAndRepelMonsters();
        }

        activeRoutine = null;
    }


    /// <summary>
    /// 检测范围内可被蚊香驱赶的怪物，并调用接口方法。
    /// </summary>
    private void DetectAndRepelMonsters()
    {
        Vector2 centerPosition =
            GetDetectionCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll
            (
                centerPosition,
                repelRadius,
                repellableMonsterLayerMask
            );

        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IMosquitoCoilRepellable>
                (
                    detectedCollider
                );

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                // 同一个怪物可能有多个 Collider2D。
                // 同一轮检测中只调用一次。
                continue;
            }

            IMosquitoCoilRepellable repellableMonster =
                interfaceBehaviour as IMosquitoCoilRepellable;

            repellableMonster?.RepelByMosquitoCoil
            (
                centerPosition
            );
        }
    }


    /// <summary>
    /// 从 Collider2D 自身及其父物体上寻找实现指定接口的脚本。
    /// 
    /// 这样可以支持：
    /// MonsterRoot
    /// └── HitBox
    ///     └── Collider2D
    ///
    /// Collider2D 挂在子物体上，
    /// 怪物控制脚本挂在根物体上。
    /// </summary>
    private MonoBehaviour FindInterfaceBehaviourInParents<T>
    (
        Collider2D myCollider
    )
        where T : class
    {
        MonoBehaviour[] parentBehaviours =
            myCollider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            MonoBehaviour currentBehaviour =
                parentBehaviours[i];

            if (currentBehaviour is T)
            {
                return currentBehaviour;
            }
        }

        return null;
    }


    private Vector2 GetDetectionCenterPosition()
    {
        if (detectionCenter != null)
        {
            return detectionCenter.position;
        }

        return transform.position;
    }


    private void OnDrawGizmosSelected()
    {
        Vector3 centerPosition;

        if (detectionCenter != null)
        {
            centerPosition = detectionCenter.position;
        }
        else
        {
            centerPosition = transform.position;
        }

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere
        (
            centerPosition,
            repelRadius
        );
    }
}
