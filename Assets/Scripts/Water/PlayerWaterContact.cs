using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 汇总玩家当前接触的 Trigger 水体，并计算水面与浸没度。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerControl))]
public class PlayerWaterContact : MonoBehaviour
{
    [SerializeField] private WaterPhysicsSettings defaultSettings;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private bool drawSampleGizmos = true;

    private readonly List<WaterVolume2D> activeVolumes = new List<WaterVolume2D>();
    private PlayerControl playerControl;

    public float SurfaceY { get; private set; }
    public float RawSubmersion { get; private set; }
    public bool HasActiveVolume => activeVolumes.Count > 0;
    public WaterVolume2D DominantVolume { get; private set; }

    public WaterPhysicsSettings ActiveSettings
    {
        get
        {
            if (DominantVolume != null && DominantVolume.Settings != null)
            {
                return DominantVolume.Settings;
            }

            if (defaultSettings != null)
            {
                return defaultSettings;
            }

            return WaterPhysicsSettings.RuntimeFallback;
        }
    }

    public float ActiveBuoyancyMultiplier =>
        DominantVolume != null ? DominantVolume.LocalBuoyancyMultiplier : 1f;

    private void Awake()
    {
        playerControl = GetComponent<PlayerControl>();

        if (bodyCollider == null)
        {
            bodyCollider = playerControl.playerColliderRef;
        }
    }

    private void OnDisable()
    {
        activeVolumes.Clear();
        DominantVolume = null;
        SurfaceY = float.NegativeInfinity;
        RawSubmersion = 0f;
        playerControl?.NotifyWaterContactChanged(false, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        WaterVolume2D volume = other.GetComponent<WaterVolume2D>();
        if (volume == null || activeVolumes.Contains(volume))
        {
            return;
        }

        activeVolumes.Add(volume);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        WaterVolume2D volume = other.GetComponent<WaterVolume2D>();
        if (volume == null)
        {
            return;
        }

        activeVolumes.Remove(volume);
    }

    private void FixedUpdate()
    {
        UpdateDominantVolume();
        UpdateSubmersion();
        playerControl.NotifyWaterContactChanged(HasActiveVolume, RawSubmersion);
    }

    private void UpdateDominantVolume()
    {
        DominantVolume = null;
        SurfaceY = float.NegativeInfinity;

        for (int i = activeVolumes.Count - 1; i >= 0; i--)
        {
            WaterVolume2D volume = activeVolumes[i];
            if (volume == null)
            {
                activeVolumes.RemoveAt(i);
                continue;
            }

            float volumeSurfaceY = volume.GetSurfaceY();
            if (volumeSurfaceY > SurfaceY)
            {
                SurfaceY = volumeSurfaceY;
                DominantVolume = volume;
            }
        }

        if (!HasActiveVolume)
        {
            SurfaceY = float.NegativeInfinity;
        }
    }

    private void UpdateSubmersion()
    {
        if (!HasActiveVolume)
        {
            RawSubmersion = 0f;
            return;
        }

        Collider2D sampleCollider = bodyCollider != null ? bodyCollider : playerControl.playerColliderRef;
        if (sampleCollider == null)
        {
            RawSubmersion = 0f;
            return;
        }

        Bounds bounds = sampleCollider.bounds;
        Vector2 feet = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 body = bounds.center;
        Vector2 head = new Vector2(bounds.center.x, bounds.max.y);

        int submergedPoints = 0;
        if (feet.y < SurfaceY)
        {
            submergedPoints++;
        }

        if (body.y < SurfaceY)
        {
            submergedPoints++;
        }

        if (head.y < SurfaceY)
        {
            submergedPoints++;
        }

        RawSubmersion = submergedPoints / 3f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawSampleGizmos || !Application.isPlaying || !HasActiveVolume)
        {
            return;
        }

        Collider2D sampleCollider = bodyCollider;
        if (sampleCollider == null && playerControl != null)
        {
            sampleCollider = playerControl.playerColliderRef;
        }

        if (sampleCollider == null)
        {
            return;
        }

        Bounds bounds = sampleCollider.bounds;
        DrawSamplePoint(new Vector2(bounds.center.x, bounds.min.y), "feet");
        DrawSamplePoint(bounds.center, "body");
        DrawSamplePoint(new Vector2(bounds.center.x, bounds.max.y), "head");

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(bounds.min.x - 0.2f, SurfaceY, 0f),
            new Vector3(bounds.max.x + 0.2f, SurfaceY, 0f));
    }

    private static void DrawSamplePoint(Vector2 point, string label)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(point, 0.05f);
    }
}
