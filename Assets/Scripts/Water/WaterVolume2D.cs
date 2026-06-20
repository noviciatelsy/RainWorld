using UnityEngine;

/// <summary>
/// 标记一段 Trigger 水域，并提供该段的水面世界 Y。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class WaterVolume2D : MonoBehaviour
{
    [SerializeField] private WaterPhysicsSettings settings;
    [SerializeField] private bool autoSurfaceFromCollider = true;
    [SerializeField] private float surfaceY;
    [SerializeField] private float localBuoyancyMultiplier = 1f;

    private Collider2D volumeCollider;

    public WaterPhysicsSettings Settings =>
        settings != null ? settings : WaterPhysicsSettings.RuntimeFallback;
    public float LocalBuoyancyMultiplier => localBuoyancyMultiplier;

    public float GetSurfaceY()
    {
        if (autoSurfaceFromCollider)
        {
            EnsureCollider();
            if (volumeCollider != null)
            {
                return volumeCollider.bounds.max.y;
            }
        }

        return surfaceY;
    }

    private void Awake()
    {
        EnsureCollider();
        SyncSurfaceFromCollider();
    }

    private void OnValidate()
    {
        EnsureCollider();

        if (volumeCollider != null && !volumeCollider.isTrigger)
        {
            volumeCollider.isTrigger = true;
        }

        SyncSurfaceFromCollider();
    }

    private void EnsureCollider()
    {
        if (volumeCollider == null)
        {
            volumeCollider = GetComponent<Collider2D>();
        }
    }

    private void SyncSurfaceFromCollider()
    {
        if (!autoSurfaceFromCollider || volumeCollider == null)
        {
            return;
        }

        surfaceY = volumeCollider.bounds.max.y;
    }

    private void OnDrawGizmos()
    {
        EnsureCollider();
        if (volumeCollider == null)
        {
            return;
        }

        Bounds bounds = volumeCollider.bounds;
        float drawSurfaceY = GetSurfaceY();

        Gizmos.color = new Color(0f, 0.75f, 1f, 0.25f);
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = Color.cyan;
        Vector3 left = new Vector3(bounds.min.x, drawSurfaceY, 0f);
        Vector3 right = new Vector3(bounds.max.x, drawSurfaceY, 0f);
        Gizmos.DrawLine(left, right);
    }
}
