using System.Collections.Generic;
using UnityEngine;

public class DemonRevealingMirror : MonoBehaviour
{
    [Header("References")]
    private Transform detectionCenter;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask mimicMonsterLayerMask;
    // 可被照妖镜检测到的拟态怪物所在 Layer

    [SerializeField] private float revealRadius = 16f;
    // 照妖镜检测半径

    [SerializeField] private bool includeInactiveParents = false;
    // 查找父物体上的接口脚本时，是否包含未激活父物体
    // 通常保持 false 即可


    [Header("Use Settings")]
    [SerializeField] private float useCooldown = 1f;
    // 使用冷却

    [SerializeField] private bool canRevealMultipleTargets = true;
    // 是否一次解除范围内所有拟态怪物
    // 如果为 false，则只解除第一个找到的目标

    [Header("VFX")]
    [SerializeField] private MirrorRevealCircleVFX revealCircleVFXPrefab;
    // 照妖镜使用时生成的扩散圆圈特效

    [SerializeField] private bool circleFollowsPlayer = true;
    // 扩散圆圈播放期间是否跟随玩家

    private float nextAllowedUseTime;

    private void Awake()
    {
        detectionCenter = transform;
    }


    /// <summary>
    /// 使用照妖镜。
    /// </summary>
    /// <returns>
    /// 是否成功使用。
    /// </returns>
    public bool UseMirror()
    {
        if (Time.time < nextAllowedUseTime)
        {
            return false;
        }

        nextAllowedUseTime = Time.time + useCooldown;

        PlayRevealCircleVFX();

        RevealMimicryTargets();

        return true;
    }


    /// <summary>
    /// 以玩家为中心检测并解除范围内怪物的拟态。
    /// </summary>
    private void RevealMimicryTargets()
    {
        Vector2 centerPosition =
            GetDetectionCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll
            (
                centerPosition,
                revealRadius,
                mimicMonsterLayerMask
            );

        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IMimicryReleasable>
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
                // 已经触发过一次的目标，不重复调用。
                continue;
            }

            IMimicryReleasable mimicryReleasable =
                interfaceBehaviour as IMimicryReleasable;

            mimicryReleasable?.ReleaseMimicry
            (
                centerPosition
            );

            if (!canRevealMultipleTargets)
            {
                return;
            }
        }
    }


    /// <summary>
    /// 从检测到的 Collider2D 所在物体及其父物体上，
    /// 寻找实现指定接口的 MonoBehaviour。
    /// 
    /// 这样可以支持：
    /// MimicMonsterRoot
    /// └── HitBox
    ///     └── Collider2D
    /// 
    /// Collider2D 挂在子物体上，
    /// 解除拟态脚本挂在根物体上。
    /// </summary>
    private MonoBehaviour FindInterfaceBehaviourInParents<T>
    (
        Collider2D myCollider
    )
        where T : class
    {
        MonoBehaviour[] parentBehaviours =
            myCollider.GetComponentsInParent<MonoBehaviour>
            (
                includeInactiveParents
            );

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

    private void PlayRevealCircleVFX()
    {
        if (revealCircleVFXPrefab == null)
        {
            return;
        }

        Vector2 centerPosition =
            GetDetectionCenterPosition();

        MirrorRevealCircleVFX newVFX =
            Instantiate
            (
                revealCircleVFXPrefab,
                centerPosition,
                Quaternion.identity
            );

        if (circleFollowsPlayer && detectionCenter != null)
        {
            newVFX.PlayAroundTarget
            (
                detectionCenter,
                revealRadius
            );
        }
        else
        {
            newVFX.PlayAtPosition
            (
                centerPosition,
                revealRadius
            );
        }
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

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere
        (
            centerPosition,
            revealRadius
        );
    }
}