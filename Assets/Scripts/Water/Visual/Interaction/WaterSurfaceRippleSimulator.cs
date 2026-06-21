using UnityEngine;

/// <summary>
/// 沿水体 X 轴的一维波方程模拟，结果写入 1D 纹理供水面 Shader 采样。
/// </summary>
[DisallowMultipleComponent]
public sealed class WaterSurfaceRippleSimulator : MonoBehaviour
{
    [SerializeField] private WaterVolume2D volume;
    [SerializeField] private int resolution = 160;
    [SerializeField] [Range(0.9f, 0.999f)] private float damping = 0.988f;
    [SerializeField] [Range(0.8f, 1f)] private float tension = 0.985f;

    private float[] currentHeights;
    private float[] previousHeights;
    private float[] scratchHeights;
    private Color[] pixelBuffer;
    private Texture2D rippleTexture;
    private float minWorldX;
    private float maxWorldX;
    private bool boundsReady;
    private bool registered;

    public Texture RippleTexture => rippleTexture;
    public Vector4 RippleBounds => new Vector4(minWorldX, 0f, Mathf.Max(maxWorldX - minWorldX, 0.001f), 1f);

    private void Awake()
    {
        if (volume == null)
        {
            volume = GetComponent<WaterVolume2D>();
        }

        EnsureBuffers();
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public void BindVolume(WaterVolume2D targetVolume)
    {
        volume = targetVolume;
        Register();
    }

    public void SyncBounds(Bounds worldBounds)
    {
        minWorldX = worldBounds.min.x;
        maxWorldX = worldBounds.max.x;
        boundsReady = maxWorldX > minWorldX;
    }

    public void AddImpulse(float worldX, float impulse)
    {
        if (!boundsReady || impulse == 0f || currentHeights == null)
        {
            return;
        }

        float normalized = Mathf.InverseLerp(minWorldX, maxWorldX, worldX);
        int center = Mathf.Clamp(Mathf.RoundToInt(normalized * (resolution - 1)), 0, resolution - 1);
        const int radius = 2;

        for (int offset = -radius; offset <= radius; offset++)
        {
            int index = center + offset;
            if (index < 0 || index >= resolution)
            {
                continue;
            }

            float falloff = 1f - Mathf.Abs(offset) / (radius + 1f);
            currentHeights[index] += impulse * falloff;
        }
    }

    public void StepSimulation()
    {
        if (!boundsReady || currentHeights == null)
        {
            return;
        }

        for (int i = 1; i < resolution - 1; i++)
        {
            float neighborAverage = (currentHeights[i - 1] + currentHeights[i + 1]) * tension;
            scratchHeights[i] = (neighborAverage - previousHeights[i]) * damping;
        }

        scratchHeights[0] = (currentHeights[1] - previousHeights[0]) * damping * 0.65f;
        scratchHeights[resolution - 1] = (currentHeights[resolution - 2] - previousHeights[resolution - 1]) * damping * 0.65f;

        float[] swapPrevious = previousHeights;
        previousHeights = currentHeights;
        currentHeights = scratchHeights;
        scratchHeights = swapPrevious;

        UploadTexture();
    }

    private void EnsureBuffers()
    {
        resolution = Mathf.Clamp(resolution, 32, 512);

        if (currentHeights != null && currentHeights.Length == resolution)
        {
            return;
        }

        currentHeights = new float[resolution];
        previousHeights = new float[resolution];
        scratchHeights = new float[resolution];
        pixelBuffer = new Color[resolution];

        if (rippleTexture != null)
        {
            Destroy(rippleTexture);
        }

        rippleTexture = new Texture2D(resolution, 1, TextureFormat.RGBA32, false, true)
        {
            name = "WaterSurfaceRipple",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int i = 0; i < resolution; i++)
        {
            pixelBuffer[i] = new Color(0.5f, 0f, 0f, 1f);
        }

        rippleTexture.SetPixels(pixelBuffer);
        rippleTexture.Apply(false, false);
    }

    private void UploadTexture()
    {
        for (int i = 0; i < resolution; i++)
        {
            pixelBuffer[i].r = currentHeights[i] + 0.5f;
        }

        rippleTexture.SetPixels(pixelBuffer);
        rippleTexture.Apply(false, false);
    }

    private void Register()
    {
        if (registered || volume == null)
        {
            return;
        }

        WaterVisualManager.RegisterRippleSimulator(volume, this);
        registered = true;
    }

    private void Unregister()
    {
        if (!registered || volume == null)
        {
            return;
        }

        WaterVisualManager.UnregisterRippleSimulator(volume, this);
        registered = false;
    }
}
