using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCameraPhotographable : MonoBehaviour, ICameraPhotographable
{
    private const string PhotoColliderChildName = "CameraPhotoTrigger";
    private const string EnemyLayerName = "Enemy";

    [SerializeField] private EnemyInformationDataSO enemyInformationData;
    [SerializeField] private bool autoCreatePhotoCollider = true;
    [SerializeField] private bool enablePhotoDebugLog = true;
    [SerializeField] private Vector2 photoColliderSize = new Vector2(1.2f, 1.2f);
    [SerializeField] private Vector2 photoColliderOffset = Vector2.zero;

    private void Awake()
    {
        if (autoCreatePhotoCollider)
        {
            EnsurePhotoDetectionCollider();
        }
    }

    public void OnPhotographed()
    {
        if (enemyInformationData == null)
        {
            if (enablePhotoDebugLog)
            {
                Debug.LogWarning($"[CameraPhoto] {name} missing EnemyInformationDataSO", this);
            }

            return;
        }

        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null)
        {
            if (enablePhotoDebugLog)
            {
                Debug.LogWarning($"[CameraPhoto] IntelligenceArchiveManager missing when photographing {enemyInformationData.enemyName}", this);
            }

            return;
        }

        bool knownBefore = archiveManager.IsEnemyUnlocked(enemyInformationData);
        bool pictureUnlocked = archiveManager.UnlockEnemyPicture(enemyInformationData);
        bool knownAfter = archiveManager.IsEnemyUnlocked(enemyInformationData);

        if (enablePhotoDebugLog)
        {
            Debug.Log(
                $"[CameraPhoto] {enemyInformationData.enemyName} | pictureUnlocked={pictureUnlocked} | knownBefore={knownBefore} | knownAfter={knownAfter}",
                this
            );
        }
    }

    private void EnsurePhotoDetectionCollider()
    {
        if (HasEnemyLayerColliderInHierarchy())
        {
            return;
        }

        if (transform.Find(PhotoColliderChildName) != null)
        {
            return;
        }

        Vector2 colliderSize = photoColliderSize;
        Vector2 colliderOffset = photoColliderOffset;

        if (TryGetVisualBounds(out Bounds visualBounds))
        {
            colliderSize = visualBounds.size;
            colliderOffset = visualBounds.center - transform.position;
        }

        colliderSize.x = Mathf.Max(0.4f, colliderSize.x);
        colliderSize.y = Mathf.Max(0.4f, colliderSize.y);

        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);

        if (enemyLayer < 0)
        {
            Debug.LogWarning($"{nameof(EnemyCameraPhotographable)} 找不到 Layer「{EnemyLayerName}」，无法创建拍照触发器：{name}");
            return;
        }

        GameObject triggerObject = new GameObject(PhotoColliderChildName);
        triggerObject.transform.SetParent(transform, false);
        triggerObject.transform.localPosition = Vector3.zero;
        triggerObject.layer = enemyLayer;

        BoxCollider2D photoCollider = triggerObject.AddComponent<BoxCollider2D>();
        photoCollider.isTrigger = true;
        photoCollider.size = colliderSize;
        photoCollider.offset = colliderOffset;
    }

    private bool HasEnemyLayerColliderInHierarchy()
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);

        if (enemyLayer < 0)
        {
            return false;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider == null)
            {
                continue;
            }

            if (collider.gameObject.name == PhotoColliderChildName)
            {
                continue;
            }

            if (collider.gameObject.layer == enemyLayer)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled || renderer.sprite == null)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;

            if (!hasBounds)
            {
                bounds = rendererBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rendererBounds);
            }
        }

        return hasBounds;
    }
}
