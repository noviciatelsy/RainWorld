using System.Collections;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
[RequireComponent(typeof(CinemachineConfiner2D))]
public class RoomCameraController : MonoBehaviour
{
    [Header("Cinemachine 组件")]
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineConfiner2D confiner2D;

    [Header("房间切换设置")]
    [SerializeField] private bool invalidateConfinerCacheWhenApplyRoom = true;

    private Coroutine cameraTransitionRoutine;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        confiner2D = GetComponent<CinemachineConfiner2D>();
    }

    public void SetFollowTarget(Transform target)
    {
        if (virtualCamera == null)
        {
            return;
        }

        virtualCamera.Follow = target;

        virtualCamera.LookAt = null;
    }

    /// <param name="cameraMoveDuration">摄像机平移到跟随目标的用时（秒）。0 表示立即跳转。</param>
    public void ApplyRoom(RoomController room, float cameraMoveDuration)
    {
        if (room == null)
        {
            return;
        }

        if (confiner2D == null)
        {
            Debug.LogWarning("RoomCameraController 缺少 CinemachineConfiner2D。");
            return;
        }

        if (room.CameraBoundsCollider == null)
        {
            Debug.LogWarning($"房间 {room.name} 没有设置 CameraBoundsCollider。");
            return;
        }

        confiner2D.m_BoundingShape2D = room.CameraBoundsCollider;

        if (invalidateConfinerCacheWhenApplyRoom)
        {
            confiner2D.InvalidateCache();
        }

        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = null;
        }

        if (cameraMoveDuration <= 0f)
        {
            ForceCameraToFollowTargetImmediately();
            return;
        }

        cameraTransitionRoutine = StartCoroutine(TransitionCameraToFollowTarget(cameraMoveDuration));
    }

    private IEnumerator TransitionCameraToFollowTarget(float duration)
    {
        if (virtualCamera == null || virtualCamera.Follow == null)
        {
            yield break;
        }

        virtualCamera.PreviousStateIsValid = false;

        Vector3 startPosition = virtualCamera.transform.position;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.0001f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));

            Vector3 followPosition = virtualCamera.Follow.position;
            Vector3 nextPosition = new Vector3(
                Mathf.Lerp(startPosition.x, followPosition.x, t),
                Mathf.Lerp(startPosition.y, followPosition.y, t),
                startPosition.z
            );

            virtualCamera.ForceCameraPosition(nextPosition, virtualCamera.transform.rotation);
            yield return null;
        }

        ForceCameraToFollowTargetImmediately();
        cameraTransitionRoutine = null;
    }

    private void ForceCameraToFollowTargetImmediately()
    {
        if (virtualCamera == null)
        {
            return;
        }

        if (virtualCamera.Follow == null)
        {
            return;
        }

        Vector3 targetPosition = virtualCamera.Follow.position;
        Vector3 cameraPosition = virtualCamera.transform.position;

        cameraPosition.x = targetPosition.x;
        cameraPosition.y = targetPosition.y;

        virtualCamera.PreviousStateIsValid = false;
        virtualCamera.ForceCameraPosition(cameraPosition, virtualCamera.transform.rotation);
    }
}
