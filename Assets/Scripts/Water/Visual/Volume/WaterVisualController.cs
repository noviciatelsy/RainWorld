using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(WaterVolume2D))]
[DefaultExecutionOrder(150)]
public sealed class WaterVisualController : MonoBehaviour
{
    [SerializeField] private WaterVolume2D volume;
    [SerializeField] private WaterVisualSettings visualSettings;
    [SerializeField] private Material waterMaterial;
    [SerializeField] private WaterVisualBody visualBody;
    [SerializeField] private WaterSurfaceRippleSimulator rippleSimulator;

    public WaterVisualSettings VisualSettings => visualSettings;

    private void Awake()
    {
        if (volume == null)
        {
            volume = GetComponent<WaterVolume2D>();
        }

        EnsureVisualBody();
        EnsureRippleSimulator();
    }

    private void OnValidate()
    {
        if (volume == null)
        {
            volume = GetComponent<WaterVolume2D>();
        }
    }

    private void FixedUpdate()
    {
        if (volume == null || rippleSimulator == null)
        {
            return;
        }

        rippleSimulator.SyncBounds(volume.WorldBounds);
        rippleSimulator.StepSimulation();
    }

    private void LateUpdate()
    {
        if (volume == null || visualBody == null)
        {
            return;
        }

        visualBody.Configure(visualSettings, waterMaterial);
        visualBody.SyncFromBounds(
            volume.WorldBounds,
            volume.GetSurfaceY(),
            rippleSimulator != null ? rippleSimulator.RippleTexture : null,
            rippleSimulator != null ? rippleSimulator.RippleBounds : Vector4.zero);
    }

    private void EnsureVisualBody()
    {
        if (visualBody != null)
        {
            return;
        }

        Transform existing = transform.Find("WaterBody");
        if (existing != null)
        {
            visualBody = existing.GetComponent<WaterVisualBody>();
            if (visualBody != null)
            {
                return;
            }
        }

        GameObject bodyObject = new GameObject("WaterBody");
        bodyObject.transform.SetParent(transform, false);
        bodyObject.layer = gameObject.layer;

        visualBody = bodyObject.AddComponent<WaterVisualBody>();
        visualBody.Configure(visualSettings, waterMaterial);
    }

    private void EnsureRippleSimulator()
    {
        if (rippleSimulator == null)
        {
            rippleSimulator = GetComponent<WaterSurfaceRippleSimulator>();
        }

        if (rippleSimulator == null)
        {
            rippleSimulator = gameObject.AddComponent<WaterSurfaceRippleSimulator>();
        }

        rippleSimulator.BindVolume(volume);
    }
}
