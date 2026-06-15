using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 电梯平台：FixedUpdate 内 MovePosition 移动，暴露 FrameDelta / Velocity；不直接改玩家速度。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-100)]
public class ElevatorPlatform : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;

    [Header("References")]
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private Transform uiAnchor;
    [SerializeField] private PhysicsMaterial2D platformPhysicsMaterial;

    public Transform UiAnchor => uiAnchor != null ? uiAnchor : transform;

    public event Action<Vector2> OnTravelFinished;

    private Rigidbody2D rb;
    private Vector2 targetWorldPosition;
    private Vector2 platformVelocity;
    private Vector2 frameDelta;
    private bool isMoving;

    private readonly HashSet<PlayerControl> riders = new HashSet<PlayerControl>();

    public Vector2 Velocity => isMoving ? platformVelocity : Vector2.zero;
    public Vector2 FrameDelta => frameDelta;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // 由玩家侧 MovePosition 同步；关闭全接触避免横向移动时与 Dynamic 玩家摩擦“卡脚”
        rb.useFullKinematicContacts = false;

        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        EnsureZeroFrictionMaterial();
    }

    private void EnsureZeroFrictionMaterial()
    {
        if (platformPhysicsMaterial == null)
        {
            platformPhysicsMaterial = new PhysicsMaterial2D("ElevatorZeroFriction")
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

    public void MoveToPosition(Vector3 worldPosition)
    {
        targetWorldPosition = worldPosition;
        isMoving = true;
        platformVelocity = Vector2.zero;
        frameDelta = Vector2.zero;
    }

    public void SnapToPosition(Vector3 worldPosition)
    {
        Vector2 delta = (Vector2)worldPosition - rb.position;
        isMoving = false;
        platformVelocity = Vector2.zero;
        frameDelta = delta;
        targetWorldPosition = worldPosition;
        rb.position = worldPosition;
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

    private void FixedUpdate()
    {
        frameDelta = Vector2.zero;

        if (!isMoving)
        {
            platformVelocity = Vector2.zero;
            return;
        }

        Vector2 currentPos = rb.position;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetWorldPosition, moveSpeed * Time.fixedDeltaTime);
        Vector2 delta = newPos - currentPos;

        if (delta.sqrMagnitude > 0f)
        {
            platformVelocity = delta / Time.fixedDeltaTime;
            frameDelta = delta;
            rb.MovePosition(newPos);
        }
        else
        {
            platformVelocity = Vector2.zero;
        }

        if (Vector2.Distance(newPos, targetWorldPosition) <= 0.001f)
        {
            rb.MovePosition(targetWorldPosition);
            isMoving = false;
            platformVelocity = Vector2.zero;
            frameDelta = Vector2.zero;
            OnTravelFinished?.Invoke(targetWorldPosition);
        }
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
        playerControl.NotifyLeftElevatorPlatform(this);
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
        playerControl.NotifyStandingOnElevator(this);
    }

    private bool IsPlayerStandingOnTop(Collision2D collision)
    {
        PlayerControl playerControl = collision.collider.GetComponentInParent<PlayerControl>();
        if (playerControl == null)
        {
            return false;
        }

        if (playerControl.transform.position.y >= transform.position.y - 0.05f)
        {
            return true;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (Mathf.Abs(contact.normal.y) >= 0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
