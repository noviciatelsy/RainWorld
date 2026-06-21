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

    [Header("Depth buoyancy")]
    [Tooltip("玩家中心低于水面达到该深度时，浮力系数达到上限。")]
    [Min(0.05f)]
    public float maxBuoyancyDepth = 1.1f;
    [Tooltip("深度-浮力曲线指数；>1 表示越接近最大深度浮力增长越快。")]
    [Min(0.1f)]
    public float depthBuoyancyCurve = 1.35f;
    [Tooltip("下潜越深，背包负重对浮力的削弱越轻；1 表示在最大深度完全抵消负重惩罚。")]
    [Range(0f, 1f)]
    public float depthLoadRelief = 1f;

    [Header("Surface float")]
    [Tooltip("静止漂浮时，玩家中心相对水面的目标高度；正值表示中心略高于水面。")]
    public float surfaceEquilibriumCenterOffset = 0.12f;
    [Tooltip("仅在玩家中心低于该深度时启用水面回正力。")]
    [Min(0.05f)]
    public float surfaceRestoreMaxDepth = 0.85f;
    public float surfaceRestoreStrength = 58f;
    public float surfaceRestoreDamping = 11f;
    [Range(0f, 1f)]
    public float surfaceRestoreDiveSuppression = 0.85f;
    [Tooltip("靠近水面且向上运动时，垂直水阻倍率。")]
    [Range(0f, 1f)]
    public float surfaceUpwardDragMultiplier = 0.2f;

    [Header("Surface jump")]
    [Tooltip("浸没度低于该值才可从水面跳跃出水。")]
    [Range(0f, 1f)]
    public float surfaceJumpMaxSubmersion = 0.75f;
    [Tooltip("玩家中心低于水面的最大深度，超过则无法跳跃出水。")]
    [Min(0.01f)]
    public float surfaceJumpMaxDepth = 0.38f;
    [Tooltip("水面跳跃力度相对 jumpForce 的倍率。")]
    [Min(0.1f)]
    public float surfaceJumpForceMultiplier = 1.05f;
    [Tooltip("跳跃出水后，短时间内禁止重新进入游泳状态。")]
    [Min(0.05f)]
    public float surfaceJumpGraceDuration = 0.45f;
    [Tooltip("向上速度高于该值且仍接近水面时，不重新进入游泳状态。")]
    [Min(0f)]
    public float surfaceJumpReentryVerticalSpeed = 0.35f;

    public float GetBuoyancyScaleForFillRatio(float fillRatio)
    {
        return Mathf.Lerp(1f, minBuoyancyScaleAtFullLoad, Mathf.Clamp01(fillRatio));
    }

    public float GetWaterGravityLoadScaleForFillRatio(float fillRatio)
    {
        return Mathf.Lerp(1f, maxWaterGravityScaleAtFullLoad, Mathf.Clamp01(fillRatio));
    }

    public float GetDepthBuoyancyFactor(float depthBelowSurface)
    {
        if (depthBelowSurface <= 0f)
        {
            return 0f;
        }

        float normalizedDepth = Mathf.Clamp01(depthBelowSurface / Mathf.Max(maxBuoyancyDepth, 0.01f));
        return Mathf.Pow(normalizedDepth, depthBuoyancyCurve);
    }

    public float EvaluateBuoyancyDepthFactor(float depthBelowCenter, float submergedHeight, float submersion)
    {
        float centerDepthFactor = GetDepthBuoyancyFactor(depthBelowCenter);
        float feetDepthFactor = GetDepthBuoyancyFactor(submergedHeight * 0.65f);
        float wadingFactor = submersion * 0.55f;
        return Mathf.Clamp01(Mathf.Max(centerDepthFactor, feetDepthFactor, wadingFactor));
    }

    public float GetLoadAdjustedBuoyancyScale(float fillRatio, float depthFactor)
    {
        float loadScale = GetBuoyancyScaleForFillRatio(fillRatio);
        return Mathf.Lerp(loadScale, 1f, depthFactor * depthLoadRelief);
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
