using System.Collections.Generic;
using UnityEngine;

public class CameraItem : MonoBehaviour
{
    [Header("References")]
    private PhotographyOverlayUI photographyOverlayUI;
    // ????????? UI

    private Camera worldCamera;
    // ????????

    private PlayerControl playerControl;
    // ????????
    // ?????????????????????????????????

    [Header("Detection Settings")]
    [SerializeField] private LayerMask photographableEnemyLayerMask;
    // ?????????????????? Layer

    [SerializeField] private float worldPlaneZ = 0f;
    // 2D ???????????????? Z ???
    // ??? 2D ?????????Z?? Z = 0


    [Header("Camera Mode Settings")]
    [SerializeField] private bool disablePlayerControlInPhotographyMode = false;
    [SerializeField] private bool enablePhotoDebugLog = true;
    // ??????????????????????
    [SerializeField] private float photoCooldown = 1f;
    // ???????
    // ????????????
    public bool IsPhotographyModeOpen
    {
        get
        {
            return isPhotographyModeOpen;
        }
    }


    private bool isPhotographyModeOpen;
    // ??????????

    private Vector2 lastWorldAreaMin;
    // ?????????????????????

    private Vector2 lastWorldAreaMax;
    // ?????????????????????

    private float nextAllowedPhotoTime;
    // ?????????????????
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
    /// ???????????
    /// ??????????????????????
    /// </summary>
    public bool UseCamera()
    {
        if (!isPhotographyModeOpen)
        {
            return false;
        }

        if (Time.time < nextAllowedPhotoTime)
        {
            return false;
        }

        if (photographyOverlayUI == null
            || worldCamera == null)
        {
            return false;
        }

        nextAllowedPhotoTime =
            Time.time + photoCooldown;

        // ???????????
        // ????????????????????????????????????
        // ????????????????
        PhotographVisibleTargets();
        AudioManager.Instance.PlaySFX("UseItemCameraSFX");
        // ?????????????
        photographyOverlayUI.PlayShutterPulse();

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

        if (enablePhotoDebugLog)
        {
            Debug.Log(
                $"[CameraPhoto] overlap area={worldAreaMin}~{worldAreaMax}, layerMask={photographableEnemyLayerMask.value}, hits={detectedColliders.Length}",
                this
            );
        }

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
                if (enablePhotoDebugLog)
                {
                    Debug.Log($"[CameraPhoto] collider {detectedCollider.name} has no ICameraPhotographable parent", this);
                }

                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            ICameraPhotographable photographableEnemy =
                interfaceBehaviour as ICameraPhotographable;

            if (enablePhotoDebugLog)
            {
                Debug.Log($"[CameraPhoto] photographing {interfaceBehaviour.name}", interfaceBehaviour);
            }

            photographableEnemy?.OnPhotographed
            (
            );
        }

        if (enablePhotoDebugLog && triggeredTargets.Count == 0)
        {
            Debug.Log("[CameraPhoto] no photographable enemy in frame", this);
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


    private MonoBehaviour FindInterfaceBehaviourInParents<T>(Collider2D myCollider) where T : class
    {
        EnemyCameraPhotographable photographable =
            myCollider.GetComponentInParent<EnemyCameraPhotographable>(true);

        if (photographable != null)
        {
            return photographable;
        }

        MonoBehaviour[] parentBehaviours = myCollider.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            MonoBehaviour currentBehaviour = parentBehaviours[i];

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