using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可站立移动地面（电梯、蜗牛壳等）：Kinematic RB + 碰撞骑手注册，暴露 Velocity / FrameDelta。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-100)]
public abstract class MovingGroundPlatform : MonoBehaviour
{
    [SerializeField] protected Collider2D platformCollider;
    [SerializeField] protected PhysicsMaterial2D platformPhysicsMaterial;

    protected Rigidbody2D rb;
    protected Vector2 platformVelocity;
    protected Vector2 frameDelta;
    protected bool isMoving;

    private readonly HashSet<PlayerControl> riders = new HashSet<PlayerControl>();

    public Vector2 Velocity => isMoving ? platformVelocity : Vector2.zero;
    public Vector2 FrameDelta => frameDelta;
    public bool IsMoving => isMoving;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.useFullKinematicContacts = false;

        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        EnsureZeroFrictionMaterial();
    }

    protected void EnsureZeroFrictionMaterial()
    {
        if (platformPhysicsMaterial == null)
        {
            platformPhysicsMaterial = new PhysicsMaterial2D("MovingGroundZeroFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        if (platformCollider != null)
        {
            platformCollider.sharedMaterial = platformPhysicsMaterial;
        }
    }

    protected void SetFrameMotion(Vector2 delta)
    {
        frameDelta = delta;
        if (delta.sqrMagnitude > 0.000001f)
        {
            platformVelocity = delta / Time.fixedDeltaTime;
            isMoving = true;
        }
        else
        {
            platformVelocity = Vector2.zero;
            isMoving = false;
        }
    }

    protected void ClearFrameMotion()
    {
        frameDelta = Vector2.zero;
        platformVelocity = Vector2.zero;
        isMoving = false;
    }

    /// <summary>
    /// 本帧无位移，但仍视为移动平台（例如下压遇玩家暂停时）。
    /// </summary>
    protected void SetHeldMovingState()
    {
        frameDelta = Vector2.zero;
        platformVelocity = Vector2.zero;
        isMoving = true;
    }

    public bool HasRider(PlayerControl playerControl)
    {
        return playerControl != null && riders.Contains(playerControl);
    }

    public void RegisterRider(PlayerControl playerControl)
    {
        if (playerControl == null)
        {
            return;
        }

        riders.Add(playerControl);
    }

    public void UnregisterRider(PlayerControl playerControl)
    {
        if (playerControl == null)
        {
            return;
        }

        riders.Remove(playerControl);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryRegisterRider(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryRegisterRider(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerControl playerControl = collision.collider.GetComponentInParent<PlayerControl>();
        if (playerControl == null)
        {
            return;
        }

        UnregisterRider(playerControl);
        NotifyPlayerLeft(playerControl);
    }

    private void TryRegisterRider(Collision2D collision)
    {
        if (!IsPlayerStandingOnTop(collision))
        {
            return;
        }

        PlayerControl playerControl = collision.collider.GetComponentInParent<PlayerControl>();
        if (playerControl == null)
        {
            return;
        }

        RegisterRider(playerControl);
        NotifyPlayerStanding(playerControl);
    }

    private void NotifyPlayerStanding(PlayerControl playerControl)
    {
        if (this is ElevatorPlatform elevator)
        {
            playerControl.NotifyStandingOnElevator(elevator);
        }
    }

    private void NotifyPlayerLeft(PlayerControl playerControl)
    {
        if (this is ElevatorPlatform elevator)
        {
            playerControl.NotifyLeftElevatorPlatform(elevator);
        }
    }

    private bool IsPlayerStandingOnTop(Collision2D collision)
    {
        PlayerControl playerControl = collision.collider.GetComponentInParent<PlayerControl>();
        if (playerControl == null)
        {
            return false;
        }

        if (playerControl.transform.position.y >= transform.position.y - 0.15f)
        {
            return true;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (Mathf.Abs(contact.normal.y) >= 0.35f)
            {
                return true;
            }
        }

        return false;
    }
}
