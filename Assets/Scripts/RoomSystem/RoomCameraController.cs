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

    public void ApplyRoom(RoomController room, bool forceCameraImmediately)
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

        // 换房间边界后，强制让 Confiner2D 重新计算缓存。
        if (invalidateConfinerCacheWhenApplyRoom)
        {
            confiner2D.InvalidateCache();
        }

        if (forceCameraImmediately)
        {
            ForceCameraToFollowTargetImmediately();
        }
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

        // 告诉 Cinemachine 不要沿用上一帧的平滑状态。
        // 否则它可能还会带着旧房间的惯性继续插值。
        virtualCamera.PreviousStateIsValid = false;

        // 黑屏期间直接把虚拟相机贴到玩家附近。
        // 之后 Confiner2D 会根据新房间边界把最终画面限制在房间内。
        virtualCamera.ForceCameraPosition(cameraPosition, virtualCamera.transform.rotation);
    }
}