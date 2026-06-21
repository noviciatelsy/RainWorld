using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Water/Water Physics Settings", fileName = "WaterPhysicsSettings")]
public class WaterPhysicsSettings : ScriptableObject
{
    [Header("Forces")]
    public float buoyancy = 22f;
    public float linearDrag = 8f;
    public float swimForce = 16f;
    public float swimBoostForce = 20f;

    [Header("Gravity")]
    [Range(0f, 1f)]
    public float gravityInWater = 0.3f;

    [Header("Speed")]
    public float maxSwimSpeed = 5.5f;

    [Header("Submersion thresholds")]
    [Range(0f, 1f)]
    public float enterSubmersionThreshold = 0.15f;
    [Range(0f, 1f)]
    public float exitSubmersionThreshold = 0.05f;
    [Range(0f, 1f)]
    public float fullSubmersionThreshold = 0.85f;
    [Range(0f, 1f)]
    public float dropPlatformBlockSubmersion = 0.5f;

    [Header("Swim boost")]
    public float swimBoostCooldown = 0.35f;
    public Vector2 swimBoostDefaultDirection = new Vector2(1f, 0.3f);

    [Header("Backpack load")]
    [Tooltip("背包满格时浮力倍率；空包为 1。")]
    [Range(0f, 1f)]
    public float minBuoyancyScaleAtFullLoad = 0.1f;
    [Tooltip("背包满格时水中重力额外倍率；空包为 1。")]
    [Min(1f)]
    public float maxWaterGravityScaleAtFullLoad = 2f;

    public float GetBuoyancyScaleForFillRatio(float fillRatio)
    {
        return Mathf.Lerp(1f, minBuoyancyScaleAtFullLoad, Mathf.Clamp01(fillRatio));
    }

    public float GetWaterGravityLoadScaleForFillRatio(float fillRatio)
    {
        return Mathf.Lerp(1f, maxWaterGravityScaleAtFullLoad, Mathf.Clamp01(fillRatio));
    }

    private static WaterPhysicsSettings runtimeFallback;

    public static WaterPhysicsSettings RuntimeFallback
    {
        get
        {
            if (runtimeFallback == null)
            {
                runtimeFallback = CreateInstance<WaterPhysicsSettings>();
            }

            return runtimeFallback;
        }
    }
}
