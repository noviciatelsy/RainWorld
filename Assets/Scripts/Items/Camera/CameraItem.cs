using System.Collections.Generic;
using UnityEngine;

public class CameraItem : MonoBehaviour
{
    [Header("References")]
    private PhotographyOverlayUI photographyOverlayUI;
    // 摄影模式遮罩 UI

    private Camera worldCamera;
    // 游戏主相机

    private PlayerControl playerControl;
    // 玩家控制脚本
    // 如果希望摄影模式中禁止玩家移动，可以使用它

    [Header("Detection Settings")]
    [SerializeField] private LayerMask photographableEnemyLayerMask;
    // 可被相机拍照的敌人所在 Layer

    [SerializeField] private float worldPlaneZ = 0f;
    // 2D 游戏物体所在的世界 Z 平面
    // 一般 2D 项目里物体都在 Z = 0


    [Header("Camera Mode Settings")]
    [SerializeField] private bool disablePlayerControlInPhotographyMode = false;
    // 开启摄影模式时是否禁用玩家操作

    public bool IsPhotographyModeOpen
    {
        get
        {
            return isPhotographyModeOpen;
        }
    }


    private bool isPhotographyModeOpen;
    // 是否处于摄影模式

    private Vector2 lastWorldAreaMin;
    // 最近一次拍照检测区域左下角

    private Vector2 lastWorldAreaMax;
    // 最近一次拍照检测区域右上角


    private void Awake()
    {
        worldCamera = Camera.main;
        playerControl = GetComponentInParent<PlayerControl>();
    }

    private void Start()
    {
        photographyOverlayUI = InGameUI.Instance.photographyOverlayUI;
    }


    public void OpenPhotographyMode()
    {
        if (isPhotographyModeOpen)
        {
            return;
        }

        isPhotographyModeOpen = true;

        if (photographyOverlayUI != null)
        {
            photographyOverlayUI.Open();
        }

        if (disablePlayerControlInPhotographyMode
            && playerControl != null)
        {
            playerControl.DisablePlayerControl();
        }
    }


    public void ClosePhotographyMode()
    {
        if (!isPhotographyModeOpen)
        {
            return;
        }

        isPhotographyModeOpen = false;

        if (photographyOverlayUI != null)
        {
            photographyOverlayUI.Close();
        }

        if (disablePlayerControlInPhotographyMode
            && playerControl != null)
        {
            playerControl.EnablePlayerControl();
        }
    }


    public void TogglePhotographyMode()
    {
        if (isPhotographyModeOpen)
        {
            ClosePhotographyMode();
        }
        else
        {
            OpenPhotographyMode();
        }
    }


    /// <summary>
    /// 使用相机拍照。
    /// 只有摄影模式开启时才会生效。
    /// </summary>
    public bool UseCamera()
    {
        if (!isPhotographyModeOpen)
        {
            return false;
        }



        if (photographyOverlayUI == null
            || worldCamera == null)
        {
            return false;
        }


        PhotographVisibleTargets();

        return true;
    }


    private void PhotographVisibleTargets()
    {
        Rect screenRect =
            photographyOverlayUI.CurrentVisibleScreenRect;

        GetWorldAreaFromScreenRect
        (
            screenRect,
            out Vector2 worldAreaMin,
            out Vector2 worldAreaMax,
            out Vector2 worldAreaCenter
        );

        lastWorldAreaMin =
            worldAreaMin;

        lastWorldAreaMax =
            worldAreaMax;

        Collider2D[] detectedColliders =
            Physics2D.OverlapAreaAll
            (
                worldAreaMin,
                worldAreaMax,
                photographableEnemyLayerMask
            );

        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<ICameraPhotographable>
                (
                    detectedCollider
                );

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            ICameraPhotographable photographableEnemy =
                interfaceBehaviour as ICameraPhotographable;

            photographableEnemy?.OnPhotographed
            (
            );
        }
    }


    private void GetWorldAreaFromScreenRect
    (
        Rect myScreenRect,
        out Vector2 myWorldAreaMin,
        out Vector2 myWorldAreaMax,
        out Vector2 myWorldAreaCenter
    )
    {
        float distanceFromCameraToWorldPlane =
            worldPlaneZ - worldCamera.transform.position.z;

        Vector3 screenMin =
            new Vector3
            (
                myScreenRect.xMin,
                myScreenRect.yMin,
                distanceFromCameraToWorldPlane
            );

        Vector3 screenMax =
            new Vector3
            (
                myScreenRect.xMax,
                myScreenRect.yMax,
                distanceFromCameraToWorldPlane
            );

        Vector3 screenCenter =
            new Vector3
            (
                myScreenRect.center.x,
                myScreenRect.center.y,
                distanceFromCameraToWorldPlane
            );

        Vector3 worldMin =
            worldCamera.ScreenToWorldPoint(screenMin);

        Vector3 worldMax =
            worldCamera.ScreenToWorldPoint(screenMax);

        Vector3 worldCenter =
            worldCamera.ScreenToWorldPoint(screenCenter);

        myWorldAreaMin =
            new Vector2
            (
                Mathf.Min(worldMin.x, worldMax.x),
                Mathf.Min(worldMin.y, worldMax.y)
            );

        myWorldAreaMax =
            new Vector2
            (
                Mathf.Max(worldMin.x, worldMax.x),
                Mathf.Max(worldMin.y, worldMax.y)
            );

        myWorldAreaCenter =
            worldCenter;
    }


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


    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Vector3 bottomLeft =
            new Vector3
            (
                lastWorldAreaMin.x,
                lastWorldAreaMin.y,
                0f
            );

        Vector3 topLeft =
            new Vector3
            (
                lastWorldAreaMin.x,
                lastWorldAreaMax.y,
                0f
            );

        Vector3 topRight =
            new Vector3
            (
                lastWorldAreaMax.x,
                lastWorldAreaMax.y,
                0f
            );

        Vector3 bottomRight =
            new Vector3
            (
                lastWorldAreaMax.x,
                lastWorldAreaMin.y,
                0f
            );

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }
}