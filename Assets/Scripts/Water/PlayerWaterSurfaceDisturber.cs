using UnityEngine;

/// <summary>
/// 玩家在水面附近移动时，向对应水体的波纹模拟器注入扰动。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(120)]
[RequireComponent(typeof(PlayerWaterContact))]
[RequireComponent(typeof(PlayerControl))]
public sealed class PlayerWaterSurfaceDisturber : MonoBehaviour
{
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private float minHorizontalSpeed = 0.12f;
    [SerializeField] private float surfaceBandDepth = 0.55f;
    [SerializeField] private float splashVerticalSpeed = 0.75f;

    private PlayerWaterContact waterContact;
    private PlayerControl playerControl;
    private Rigidbody2D rb;
    private float previousVerticalVelocity;

    private void Awake()
    {
        waterContact = GetComponent<PlayerWaterContact>();
        playerControl = GetComponent<PlayerControl>();
        rb = playerControl.rb;

        if (bodyCollider == null)
        {
            bodyCollider = playerControl.playerColliderRef;
        }
    }

    private void FixedUpdate()
    {
        if (!waterContact.HasActiveVolume || waterContact.DominantVolume == null)
        {
            previousVerticalVelocity = rb != null ? rb.velocity.y : 0f;
            return;
        }

        if (!WaterVisualManager.TryGetRippleSimulator(waterContact.DominantVolume, out WaterSurfaceRippleSimulator simulator))
        {
            previousVerticalVelocity = rb.velocity.y;
            return;
        }

        Collider2D sampleCollider = bodyCollider != null ? bodyCollider : playerControl.playerColliderRef;
        if (sampleCollider == null || rb == null)
        {
            previousVerticalVelocity = rb != null ? rb.velocity.y : 0f;
            return;
        }

        Bounds bounds = sampleCollider.bounds;
        float surfaceY = waterContact.SurfaceY;
        float surfaceDepth = surfaceY - bounds.min.y;
        float surfaceInfluence = EvaluateSurfaceInfluence(surfaceDepth, waterContact.RawSubmersion);
        if (surfaceInfluence <= 0f)
        {
            previousVerticalVelocity = rb.velocity.y;
            return;
        }

        WaterVisualSettings settings = WaterVisualSettings.RuntimeFallback;
        WaterVisualController visualController = waterContact.DominantVolume.GetComponent<WaterVisualController>();
        if (visualController != null && visualController.VisualSettings != null)
        {
            settings = visualController.VisualSettings;
        }

        float horizontalVelocity = rb.velocity.x;
        if (Mathf.Abs(horizontalVelocity) >= minHorizontalSpeed)
        {
            float wakeImpulse = -horizontalVelocity * settings.playerRippleInjectStrength * surfaceInfluence * 0.028f;
            simulator.AddImpulse(bounds.center.x, wakeImpulse);
        }

        if (previousVerticalVelocity < -splashVerticalSpeed && rb.velocity.y > previousVerticalVelocity)
        {
            float splashImpulse = -previousVerticalVelocity * settings.playerRippleSplashStrength * surfaceInfluence * 0.045f;
            simulator.AddImpulse(bounds.center.x, splashImpulse);
        }

        previousVerticalVelocity = rb.velocity.y;
    }

    private float EvaluateSurfaceInfluence(float surfaceDepth, float submersion)
    {
        if (surfaceDepth < -0.12f)
        {
            return 0f;
        }

        Collider2D sampleCollider = bodyCollider != null ? bodyCollider : playerControl.playerColliderRef;
        if (sampleCollider == null)
        {
            return 0f;
        }

        float surfaceY = waterContact.SurfaceY;
        float bodyDepth = surfaceY - sampleCollider.bounds.center.y;
        float feetInfluence = 1f - Mathf.Clamp01(surfaceDepth / Mathf.Max(surfaceBandDepth, 0.01f));
        float bodyInfluence = 1f - Mathf.Clamp01(bodyDepth / Mathf.Max(surfaceBandDepth * 1.35f, 0.01f));
        float submersionInfluence = 1f - Mathf.Clamp01((submersion - 0.15f) / 0.85f);

        return Mathf.Clamp01(Mathf.Max(feetInfluence, bodyInfluence * 0.8f) * Mathf.Lerp(0.45f, 1f, submersionInfluence));
    }
}
