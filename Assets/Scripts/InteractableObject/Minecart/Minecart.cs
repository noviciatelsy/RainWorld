using UnityEngine;

public class Minecart : MonoBehaviour
{
    [Header("Rail")]
    [SerializeField] private RailPath railPath;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Passenger")]
    private Transform passenger;
    private Rigidbody2D passengerRb;
    private bool hasPassenger;

    private int segmentIndex = 0;
    private float t = 0f;

    void Update()
    {
        HandleInput();
    }

    void LateUpdate()
    {
        SyncPassenger();
    }

    // =========================
    // 输入处理
    // =========================
    void HandleInput()
    {
        if (passenger == null)
            return;

        // 上车
        if (!hasPassenger && Input.GetKeyDown(KeyCode.Space))
        {
            EnterCart();
        }

        // 控制移动
        if (hasPassenger)
        {
            float input = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(input) > 0.01f)
            {
                MoveOnRail(input);
            }

            // 下车
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExitCart();
            }
        }
    }

    // =========================
    // 轨道移动
    // =========================
    void MoveOnRail(float dir)
    {
        if (railPath == null || railPath.points.Length < 2)
            return;

        Transform a = railPath.points[segmentIndex];
        Transform b = railPath.points[segmentIndex + 1];

        float length = Vector2.Distance(a.position, b.position);
        if (length < 0.001f) return;

        t += dir * speed * Time.deltaTime / length;

        if (t > 1f)
        {
            t = 0f;
            segmentIndex = Mathf.Min(segmentIndex + 1, railPath.points.Length - 2);
        }
        else if (t < 0f)
        {
            segmentIndex = Mathf.Max(segmentIndex - 1, 0);
            t = 1f;
        }

        UpdateCartTransform();
    }

    // =========================
    // 更新矿车位置和旋转
    // =========================
    void UpdateCartTransform()
    {
        Transform a = railPath.points[segmentIndex];
        Transform b = railPath.points[segmentIndex + 1];

        transform.position = Vector2.Lerp(a.position, b.position, t);

        Vector2 dir = b.position - a.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // =========================
    // 玩家进入检测（Trigger）
    // =========================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        passenger = other.transform;
        passengerRb = other.GetComponent<Rigidbody2D>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (hasPassenger)
            ExitCart();

        passenger = null;
        passengerRb = null;
    }

    // =========================
    // 上车
    // =========================
    public void EnterCart()
    {
        if (passenger == null)
            return;

        hasPassenger = true;

        // 禁用输入系统（你要求的方式）
        InputManager.Instance.mainInput.Player.Disable();
        InputManager.Instance.mainInput.UI.Disable();

        // 停止玩家物理
        if (passengerRb != null)
        {
            passengerRb.velocity = Vector2.zero;
            passengerRb.angularVelocity = 0f;
            passengerRb.simulated = false;
        }
    }

    // =========================
    // 下车
    // =========================
    public void ExitCart()
    {
        if (!hasPassenger)
            return;

        hasPassenger = false;

        InputManager.Instance.mainInput.Player.Enable();
        InputManager.Instance.mainInput.UI.Enable();

        if (passenger != null)
        {
            passengerRb.simulated = true;
            passengerRb.velocity = Vector2.up * 6f;
        }

        passenger = null;
        passengerRb = null;
    }

    // =========================
    // 玩家同步位置（关键）
    // =========================
    void SyncPassenger()
    {
        if (!hasPassenger || passenger == null)
            return;

        passenger.position = transform.position;
    }
}