using UnityEngine;
using UnityEngine.InputSystem;

public class Minecart : MonoBehaviour
{
    private const float RailEndEpsilon = 0.001f;

    private static Minecart playerBoardZoneCart;

    [Header("Rail")]
    [SerializeField] private RailPath railPath;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [Tooltip("true：按一次跳跃上车后自动驶向路径终点；false：上车后需方向键手动移动")]
    [SerializeField] private bool autoDriveToEndOnBoard = true;

    private Transform passenger;
    private Rigidbody2D passengerRb;
    private Rigidbody2D cartRb;
    private RigidbodyType2D passengerOriginalBodyType;
    private bool hasPassenger;
    private bool isAutoDriving;
    private float autoDriveDirection = 1f;

    private int segmentIndex;
    private float t;

    private void Awake()
    {
        cartRb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        SnapToRail();
    }

    private void Update()
    {
        if (!hasPassenger || isAutoDriving)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExitCart();
        }
    }

    private void FixedUpdate()
    {
        if (!hasPassenger)
        {
            return;
        }

        if (isAutoDriving)
        {
            UpdateAutoDrive();
        }
        else
        {
            HandleManualDrive();
        }

        SyncPassenger();
    }

    private void UpdateAutoDrive()
    {
        if (HasReachedRailEnd(autoDriveDirection))
        {
            SnapToRailEnd(autoDriveDirection);
            FinishRide();
            return;
        }

        MoveOnRail(autoDriveDirection);
    }

    private void HandleManualDrive()
    {
        float input = ReadHorizontalInput();
        if (Mathf.Abs(input) > 0.01f)
        {
            MoveOnRail(input);
        }
    }

    public static bool TryBoardNearbyCart()
    {
        if (playerBoardZoneCart == null || playerBoardZoneCart.hasPassenger)
        {
            return false;
        }

        playerBoardZoneCart.EnterCart();
        return playerBoardZoneCart.hasPassenger;
    }

    private void MoveOnRail(float dir)
    {
        if (railPath == null || !railPath.HasValidPath)
        {
            return;
        }

        Transform a = railPath.points[segmentIndex];
        Transform b = railPath.points[segmentIndex + 1];

        float length = Vector2.Distance(a.position, b.position);
        if (length < 0.001f)
        {
            return;
        }

        t += dir * speed * Time.fixedDeltaTime / length;

        while (t > 1f && segmentIndex < railPath.points.Length - 2)
        {
            t -= 1f;
            segmentIndex++;
        }

        while (t < 0f && segmentIndex > 0)
        {
            t += 1f;
            segmentIndex--;
        }

        if (dir > 0f)
        {
            segmentIndex = Mathf.Clamp(segmentIndex, 0, railPath.points.Length - 2);
            t = Mathf.Clamp(t, 0f, 1f);
        }
        else
        {
            segmentIndex = Mathf.Clamp(segmentIndex, 0, railPath.points.Length - 2);
            t = Mathf.Clamp(t, 0f, 1f);
        }

        UpdateCartTransform();
    }

    private void UpdateCartTransform()
    {
        if (railPath == null || !railPath.HasValidPath)
        {
            return;
        }

        Transform a = railPath.points[segmentIndex];
        Transform b = railPath.points[segmentIndex + 1];

        Vector2 worldPosition = Vector2.Lerp(a.position, b.position, t);

        if (cartRb != null)
        {
            cartRb.MovePosition(worldPosition);
        }
        else
        {
            transform.position = worldPosition;
        }

        Vector2 dir = b.position - a.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void SnapToRail()
    {
        if (railPath == null || !railPath.HasValidPath)
        {
            return;
        }

        Vector2 cartPosition = transform.position;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < railPath.points.Length - 1; i++)
        {
            Transform a = railPath.points[i];
            Transform b = railPath.points[i + 1];
            Vector2 closest = ClosestPointOnSegment(cartPosition, a.position, b.position, out float segmentT);
            float distanceSqr = (closest - cartPosition).sqrMagnitude;

            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            segmentIndex = i;
            t = segmentT;
        }

        UpdateCartTransform();
    }

    private void SnapToRailEnd(float direction)
    {
        if (railPath == null || !railPath.HasValidPath)
        {
            return;
        }

        if (direction >= 0f)
        {
            segmentIndex = railPath.points.Length - 2;
            t = 1f;
        }
        else
        {
            segmentIndex = 0;
            t = 0f;
        }

        UpdateCartTransform();
    }

    private bool HasReachedRailEnd(float direction)
    {
        if (railPath == null || !railPath.HasValidPath)
        {
            return true;
        }

        if (direction >= 0f)
        {
            return segmentIndex >= railPath.points.Length - 2 && t >= 1f - RailEndEpsilon;
        }

        return segmentIndex <= 0 && t <= RailEndEpsilon;
    }

    private float ResolveAutoDriveDirection()
    {
        return 1f;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b, out float segmentT)
    {
        Vector2 ab = b - a;
        float lengthSqr = ab.sqrMagnitude;

        if (lengthSqr < 0.0001f)
        {
            segmentT = 0f;
            return a;
        }

        segmentT = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSqr);
        return a + ab * segmentT;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterPassenger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (hasPassenger || passenger != null)
        {
            return;
        }

        RegisterPassenger(other);
    }

    private void RegisterPassenger(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        passenger = player.transform;
        passengerRb = player.GetComponent<Rigidbody2D>();
        playerBoardZoneCart = this;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        if (hasPassenger && isAutoDriving)
        {
            return;
        }

        if (hasPassenger)
        {
            ExitCart();
        }

        if (playerBoardZoneCart == this)
        {
            playerBoardZoneCart = null;
        }

        passenger = null;
        passengerRb = null;
    }

    private void OnDisable()
    {
        if (playerBoardZoneCart == this)
        {
            playerBoardZoneCart = null;
        }
    }

    public void EnterCart()
    {
        if (passenger == null || hasPassenger)
        {
            return;
        }

        hasPassenger = true;
        SnapToRail();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.mainInput.Player.Disable();
            InputManager.Instance.mainInput.UI.Disable();
        }

        if (passengerRb != null)
        {
            passengerOriginalBodyType = passengerRb.bodyType;
            passengerRb.bodyType = RigidbodyType2D.Kinematic;
            passengerRb.velocity = Vector2.zero;
            passengerRb.angularVelocity = 0f;
            passengerRb.simulated = true;
        }

        SyncPassenger();

        if (autoDriveToEndOnBoard)
        {
            autoDriveDirection = ResolveAutoDriveDirection();
            isAutoDriving = true;

            if (HasReachedRailEnd(autoDriveDirection))
            {
                SnapToRailEnd(autoDriveDirection);
                FinishRide();
            }

            return;
        }

        float boardDirection = ReadHorizontalInput();
        if (Mathf.Abs(boardDirection) > 0.01f)
        {
            MoveOnRail(boardDirection);
        }
    }

    private void FinishRide()
    {
        isAutoDriving = false;
        ExitCart();
    }

    public void ExitCart()
    {
        if (!hasPassenger)
        {
            return;
        }

        hasPassenger = false;
        isAutoDriving = false;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.mainInput.Player.Enable();
            InputManager.Instance.mainInput.UI.Enable();
        }

        if (passengerRb != null)
        {
            passengerRb.bodyType = passengerOriginalBodyType;
            passengerRb.velocity = Vector2.zero;
            passengerRb.simulated = true;
        }

        passenger = null;
        passengerRb = null;
    }

    private void SyncPassenger()
    {
        if (!hasPassenger || passengerRb == null)
        {
            return;
        }

        Vector2 cartPosition = cartRb != null ? cartRb.position : (Vector2)transform.position;
        passengerRb.MovePosition(cartPosition);
    }

    private static float ReadHorizontalInput()
    {
        if (Keyboard.current == null)
        {
            return 0f;
        }

        float input = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input += 1f;
        }

        return input;
    }
}
