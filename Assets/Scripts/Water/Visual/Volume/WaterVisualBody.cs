using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class WaterVisualBody : MonoBehaviour
{
    [SerializeField] private WaterVisualSettings settings;
    [SerializeField] private Material waterMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Mesh runtimeMesh;
    private Vector3 lastSize = Vector3.one * -1f;
    private Vector3 lastCenter = Vector3.positiveInfinity;
    private bool registered;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        if (settings == null)
        {
            settings = WaterVisualSettings.RuntimeFallback;
        }

        if (waterMaterial == null)
        {
            Shader shader = Shader.Find("RainWorld/Water/Body");
            if (shader != null)
            {
                waterMaterial = new Material(shader);
            }
        }

        if (waterMaterial != null)
        {
            meshRenderer.sharedMaterial = waterMaterial;
        }

        ApplySorting();
    }

    private void OnEnable()
    {
        WaterVisualManager.Register(this);
        registered = true;
    }

    private void OnDisable()
    {
        if (registered)
        {
            WaterVisualManager.Unregister(this);
            registered = false;
        }
    }

    private void OnDestroy()
    {
        if (runtimeMesh != null)
        {
            Destroy(runtimeMesh);
        }
    }

    public void Configure(WaterVisualSettings visualSettings, Material materialOverride)
    {
        if (visualSettings != null)
        {
            settings = visualSettings;
        }

        if (materialOverride != null)
        {
            waterMaterial = materialOverride;
            meshRenderer.sharedMaterial = waterMaterial;
        }

        ApplySorting();
    }

    public void SyncFromBounds(Bounds worldBounds, float surfaceY, Texture rippleTexture, Vector4 rippleBounds)
    {
        if (settings == null)
        {
            settings = WaterVisualSettings.RuntimeFallback;
        }

        Vector3 size = worldBounds.size;
        size.z = 1f;
        Vector3 center = worldBounds.center;
        center.z = transform.parent != null ? transform.parent.position.z : 0f;

        if (size != lastSize || center != lastCenter)
        {
            EnsureMesh(size);
            transform.position = center;
            lastSize = size;
            lastCenter = center;
        }

        propertyBlock.SetVector(
            WaterVisualConstants.VolumeBounds,
            new Vector4(worldBounds.min.x, worldBounds.min.y, worldBounds.max.x, worldBounds.max.y));
        propertyBlock.SetFloat(WaterVisualConstants.SurfaceY, surfaceY);
        propertyBlock.SetColor(WaterVisualConstants.ShallowColor, settings.shallowColor);
        propertyBlock.SetColor(WaterVisualConstants.DeepColor, settings.deepColor);
        propertyBlock.SetColor(WaterVisualConstants.SurfaceHighlight, settings.surfaceHighlight);
        propertyBlock.SetFloat(WaterVisualConstants.SurfaceLineWidth, settings.surfaceLineWidth);
        propertyBlock.SetFloat(WaterVisualConstants.SurfaceWaveAmplitude, settings.surfaceWaveAmplitude);
        propertyBlock.SetFloat(WaterVisualConstants.SurfaceWaveSpeed, settings.surfaceWaveSpeed);
        propertyBlock.SetFloat(WaterVisualConstants.CausticsStrength, settings.causticsStrength);
        propertyBlock.SetFloat(WaterVisualConstants.CausticsScale, settings.causticsScale);
        propertyBlock.SetFloat(WaterVisualConstants.CausticsSpeed, settings.causticsSpeed);
        propertyBlock.SetFloat(WaterVisualConstants.FoamStrength, settings.foamStrength);
        propertyBlock.SetFloat(WaterVisualConstants.FoamBandWidth, settings.foamBandWidth);
        propertyBlock.SetFloat(WaterVisualConstants.RippleDisplacementStrength, settings.rippleDisplacementStrength);
        propertyBlock.SetFloat(WaterVisualConstants.RippleLineStrength, settings.rippleLineStrength);
        propertyBlock.SetFloat(WaterVisualConstants.RippleShallowDepth, settings.rippleShallowDepth);
        propertyBlock.SetVector(WaterVisualConstants.RippleBounds, rippleBounds);

        if (rippleTexture != null)
        {
            propertyBlock.SetTexture(WaterVisualConstants.RippleTexture, rippleTexture);
        }

        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplySorting()
    {
        if (meshRenderer == null || settings == null)
        {
            return;
        }

        meshRenderer.sortingLayerName = settings.sortingLayerName;
        meshRenderer.sortingOrder = settings.sortingOrder;
    }

    private void EnsureMesh(Vector3 size)
    {
        if (runtimeMesh == null)
        {
            runtimeMesh = new Mesh { name = "WaterBodyMesh" };
        }

        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;

        runtimeMesh.vertices = new[]
        {
            new Vector3(-halfX, -halfY, 0f),
            new Vector3(halfX, -halfY, 0f),
            new Vector3(-halfX, halfY, 0f),
            new Vector3(halfX, halfY, 0f)
        };

        runtimeMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        runtimeMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        runtimeMesh.RecalculateBounds();
        runtimeMesh.RecalculateNormals();

        meshFilter.sharedMesh = runtimeMesh;
    }
}
