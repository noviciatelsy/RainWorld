using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Water/Water Visual Settings", fileName = "WaterVisualSettings")]
public class WaterVisualSettings : ScriptableObject
{
    [Header("Depth Gradient")]
    public Color shallowColor = new Color(0.35f, 0.78f, 0.92f, 0.28f);
    public Color deepColor = new Color(0.05f, 0.18f, 0.32f, 0.62f);

    [Header("Surface Line")]
    public Color surfaceHighlight = new Color(0.75f, 0.95f, 1f, 0.45f);
    [Range(0.005f, 0.25f)] public float surfaceLineWidth = 0.045f;
    [Range(0f, 0.15f)] public float surfaceWaveAmplitude = 0.035f;
    [Range(0f, 4f)] public float surfaceWaveSpeed = 1.1f;

    [Header("Caustics (procedural, no screen grab)")]
    [Range(0f, 1f)] public float causticsStrength = 0.28f;
    [Range(0.1f, 4f)] public float causticsScale = 0.55f;
    [Range(0f, 2f)] public float causticsSpeed = 0.35f;

    [Header("Surface Foam")]
    [Range(0f, 1f)] public float foamStrength = 0.22f;
    [Range(0.01f, 0.35f)] public float foamBandWidth = 0.09f;

    [Header("Player Ripples")]
    [Range(0f, 8f)] public float playerRippleInjectStrength = 2.4f;
    [Range(0f, 8f)] public float playerRippleSplashStrength = 1.6f;
    [Range(0f, 0.25f)] public float rippleDisplacementStrength = 0.07f;
    [Range(0f, 1f)] public float rippleLineStrength = 0.42f;
    [Range(0.05f, 1.5f)] public float rippleShallowDepth = 0.35f;

    [Header("Sorting")]
    public string sortingLayerName = WaterVisualConstants.WaterSortingLayerName;
    public int sortingOrder = 0;

    private static WaterVisualSettings runtimeFallback;

    public static WaterVisualSettings RuntimeFallback
    {
        get
        {
            if (runtimeFallback == null)
            {
                runtimeFallback = CreateInstance<WaterVisualSettings>();
            }

            return runtimeFallback;
        }
    }
}
