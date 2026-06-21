using UnityEngine;

/// <summary>
/// 在 FixedUpdate 中对玩家刚体施加水中浮力、阻力与游泳输入力。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(110)]
[RequireComponent(typeof(PlayerControl))]
[RequireComponent(typeof(PlayerWaterContact))]
public class PlayerWaterPhysics : MonoBehaviour
{
    private PlayerControl playerControl;
    private PlayerWaterContact waterContact;
    private InventoryPlayer inventoryPlayer;
    private Rigidbody2D rb;
    private float swimBoostCooldownTimer;

    private void Awake()
    {
        playerControl = GetComponent<PlayerControl>();
        waterContact = GetComponent<PlayerWaterContact>();
        inventoryPlayer = GetComponent<InventoryPlayer>();
        rb = playerControl.rb;
    }

    private void FixedUpdate()
    {
        if (swimBoostCooldownTimer > 0f)
        {
            swimBoostCooldownTimer -= Time.fixedDeltaTime;
        }

        if (!playerControl.isInWater || !playerControl.IsInSwimState())
        {
            return;
        }

        WaterPhysicsSettings settings = waterContact.ActiveSettings;
        float submersion = playerControl.waterSubmersion;
        if (submersion <= 0f)
        {
            return;
        }

        float backpackFillRatio = GetBackpackFillRatio();
        ApplyBuoyancy(settings, submersion, backpackFillRatio);
        ApplySurfaceFloat(settings, backpackFillRatio);
        ApplyDrag(settings, submersion);
        ApplySwimInput(settings, submersion);
        ClampSwimSpeed(settings);
    }

    public bool TrySwimBoost()
    {
        if (!playerControl.isInWater || swimBoostCooldownTimer > 0f)
        {
            return false;
        }

        WaterPhysicsSettings settings = waterContact.ActiveSettings;
        if (playerControl.waterSubmersion <= settings.enterSubmersionThreshold)
        {
            return false;
        }

        Vector2 direction = playerControl.moveInput;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = new Vector2(
                playerControl.facingDir * settings.swimBoostDefaultDirection.x,
                settings.swimBoostDefaultDirection.y);
        }

        direction.Normalize();
        rb.AddForce(direction * settings.swimBoostForce, ForceMode2D.Impulse);
        swimBoostCooldownTimer = settings.swimBoostCooldown;
        playerControl.Handleflip(direction.x);
        return true;
    }

    public float GetGravityMultiplier()
    {
        if (!playerControl.isInWater)
        {
            return 1f;
        }

        WaterPhysicsSettings settings = waterContact.ActiveSettings;
        float submersionGravity = Mathf.Lerp(1f, settings.gravityInWater, playerControl.waterSubmersion);
        float loadGravityScale = settings.GetWaterGravityLoadScaleForFillRatio(GetBackpackFillRatio());
        return submersionGravity * loadGravityScale;
    }

    private float GetBackpackFillRatio()
    {
        if (inventoryPlayer == null)
        {
            return 0f;
        }

        return inventoryPlayer.GetCellFillRatio();
    }

    private void ApplyBuoyancy(WaterPhysicsSettings settings, float submersion, float backpackFillRatio)
    {
        float depthFactor = settings.EvaluateBuoyancyDepthFactor(
            waterContact.DepthBelowSurface,
            waterContact.SubmergedHeight,
            submersion);

        if (depthFactor <= 0f)
        {
            return;
        }

        float loadScale = settings.GetLoadAdjustedBuoyancyScale(backpackFillRatio, depthFactor);
        float force = settings.buoyancy * depthFactor * loadScale * waterContact.ActiveBuoyancyMultiplier;
        rb.AddForce(Vector2.up * force, ForceMode2D.Force);
    }

    private void ApplySurfaceFloat(WaterPhysicsSettings settings, float backpackFillRatio)
    {
        if (waterContact.DepthBelowSurface > settings.surfaceRestoreMaxDepth)
        {
            return;
        }

        float targetCenterY = waterContact.SurfaceY + settings.surfaceEquilibriumCenterOffset;
        float centerY = waterContact.SurfaceY - waterContact.DepthBelowSurface;
        float positionError = targetCenterY - centerY;

        float diveIntent = Mathf.Clamp01(-playerControl.moveInput.y);
        float restoreWeight = 1f - diveIntent * settings.surfaceRestoreDiveSuppression;
        if (restoreWeight <= 0f)
        {
            return;
        }

        float loadScale = settings.GetLoadAdjustedBuoyancyScale(backpackFillRatio, 1f);
        float springForce = positionError * settings.surfaceRestoreStrength * restoreWeight * loadScale;
        float dampForce = -rb.velocity.y * settings.surfaceRestoreDamping * restoreWeight;
        rb.AddForce(new Vector2(0f, springForce + dampForce), ForceMode2D.Force);
    }

    private void ApplyDrag(WaterPhysicsSettings settings, float submersion)
    {
        Vector2 velocity = rb.velocity;
        Vector2 drag = -velocity * (settings.linearDrag * submersion);

        float nearSurfaceFactor = 1f - Mathf.Clamp01(
            waterContact.DepthBelowSurface / Mathf.Max(settings.surfaceRestoreMaxDepth, 0.01f));
        if (velocity.y > 0f && nearSurfaceFactor > 0f)
        {
            drag.y *= Mathf.Lerp(1f, settings.surfaceUpwardDragMultiplier, nearSurfaceFactor);
        }

        rb.AddForce(drag, ForceMode2D.Force);
    }

    private void ApplySwimInput(WaterPhysicsSettings settings, float submersion)
    {
        Vector2 input = playerControl.moveInput;
        if (input.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 swimForce = input.normalized * (settings.swimForce * submersion);
        rb.AddForce(swimForce, ForceMode2D.Force);

        if (Mathf.Abs(input.x) > playerControl.climbInputDeadZone)
        {
            playerControl.Handleflip(input.x);
        }
    }

    private void ClampSwimSpeed(WaterPhysicsSettings settings)
    {
        if (rb.velocity.magnitude <= settings.maxSwimSpeed)
        {
            return;
        }

        rb.velocity = rb.velocity.normalized * settings.maxSwimSpeed;
    }
}
