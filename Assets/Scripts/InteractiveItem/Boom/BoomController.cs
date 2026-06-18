using System.Collections;
using UnityEngine;

/// <summary>
/// Boom 交互：玩家踩下触发平台后，贴图在指定时间内飞向目标点，并破坏可破坏墙。
/// </summary>
[DisallowMultipleComponent]
public class BoomController : MonoBehaviour
{
    [Header("Trigger Platform")]
    [SerializeField] private BoomStompPlatform stompPlatform;

    [Header("Texture Move")]
    [SerializeField] private Transform movingTexture;
    [Tooltip("留空则使用 textureTargetWorldPosition")]
    [SerializeField] private Transform textureTarget;
    [SerializeField] private Vector3 textureTargetWorldPosition;
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private bool useLocalSpace;

    [Header("Destructible Wall")]
    [SerializeField] private DestructibleWall destructibleWall;
    [SerializeField] private bool permanentWallDestroy;
    [Tooltip("true：贴图到达目标后再碎墙；false：踩下瞬间碎墙")]
    [SerializeField] private bool destroyWallOnMoveComplete;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private bool isActivated;
    private Coroutine moveRoutine;

    public bool IsActivated => isActivated;

    private void Awake()
    {
        if (stompPlatform == null)
        {
            stompPlatform = GetComponentInChildren<BoomStompPlatform>(true);
        }

        if (stompPlatform != null)
        {
            stompPlatform.Bind(this);
        }
    }

    /// <summary>
    /// 由 BoomStompPlatform 在玩家踩下时调用。
    /// </summary>
    public void ActivateFromPlatform(BoomStompPlatform platform)
    {
        if (isActivated)
        {
            return;
        }

        if (stompPlatform != null && platform != null && platform != stompPlatform)
        {
            return;
        }

        isActivated = true;

        if (!destroyWallOnMoveComplete)
        {
            TriggerDestructibleWall();
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveTextureRoutine());
    }

    private IEnumerator MoveTextureRoutine()
    {
        if (movingTexture == null)
        {
            if (destroyWallOnMoveComplete)
            {
                TriggerDestructibleWall();
            }

            yield break;
        }

        float duration = Mathf.Max(0.01f, moveDuration);
        Vector3 start = useLocalSpace ? movingTexture.localPosition : movingTexture.position;
        Vector3 end = ResolveTargetPosition();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = Vector3.Lerp(start, end, t);

            if (useLocalSpace)
            {
                movingTexture.localPosition = pos;
            }
            else
            {
                movingTexture.position = pos;
            }

            yield return null;
        }

        if (useLocalSpace)
        {
            movingTexture.localPosition = end;
        }
        else
        {
            movingTexture.position = end;
        }

        if (destroyWallOnMoveComplete)
        {
            TriggerDestructibleWall();
        }

        moveRoutine = null;
    }

    private Vector3 ResolveTargetPosition()
    {
        if (textureTarget != null)
        {
            return useLocalSpace && movingTexture != null && movingTexture.parent != null
                ? movingTexture.parent.InverseTransformPoint(textureTarget.position)
                : textureTarget.position;
        }

        return useLocalSpace && movingTexture != null && movingTexture.parent != null
            ? movingTexture.parent.InverseTransformPoint(textureTargetWorldPosition)
            : textureTargetWorldPosition;
    }

    private void TriggerDestructibleWall()
    {
        if (destructibleWall == null || destructibleWall.IsDestroyed)
        {
            return;
        }

        destructibleWall.NotifyWallDestroy(permanentWallDestroy);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Vector3 target = textureTarget != null ? textureTarget.position : textureTargetWorldPosition;

        if (movingTexture != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(movingTexture.position, target);
            Gizmos.DrawWireSphere(movingTexture.position, 0.08f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target, 0.12f);
    }
#endif
}
