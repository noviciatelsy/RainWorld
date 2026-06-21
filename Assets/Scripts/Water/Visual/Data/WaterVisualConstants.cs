using UnityEngine;

public static class WaterVisualConstants
{
    public const string WaterSortingLayerName = "Water";

    public static readonly int VolumeBounds = Shader.PropertyToID("_VolumeBounds");
    public static readonly int SurfaceY = Shader.PropertyToID("_SurfaceY");
    public static readonly int ShallowColor = Shader.PropertyToID("_ShallowColor");
    public static readonly int DeepColor = Shader.PropertyToID("_DeepColor");
    public static readonly int SurfaceHighlight = Shader.PropertyToID("_SurfaceHighlight");
    public static readonly int SurfaceLineWidth = Shader.PropertyToID("_SurfaceLineWidth");
    public static readonly int SurfaceWaveAmplitude = Shader.PropertyToID("_SurfaceWaveAmplitude");
    public static readonly int SurfaceWaveSpeed = Shader.PropertyToID("_SurfaceWaveSpeed");
    public static readonly int CausticsStrength = Shader.PropertyToID("_CausticsStrength");
    public static readonly int CausticsScale = Shader.PropertyToID("_CausticsScale");
    public static readonly int CausticsSpeed = Shader.PropertyToID("_CausticsSpeed");
    public static readonly int FoamStrength = Shader.PropertyToID("_FoamStrength");
    public static readonly int FoamBandWidth = Shader.PropertyToID("_FoamBandWidth");
    public static readonly int RippleTexture = Shader.PropertyToID("_RippleTex");
    public static readonly int RippleBounds = Shader.PropertyToID("_RippleBounds");
    public static readonly int RippleDisplacementStrength = Shader.PropertyToID("_RippleDisplacementStrength");
    public static readonly int RippleLineStrength = Shader.PropertyToID("_RippleLineStrength");
    public static readonly int RippleShallowDepth = Shader.PropertyToID("_RippleShallowDepth");
}
