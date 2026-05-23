using System.Collections;
using UnityEngine;

public class PickableObject : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("物品大小显示倍率")]
    [SerializeField] private float baseScaleMultiplier = 3;
    [SerializeField] private Vector2 originalGroundBounceColliderSize = new Vector2(0.2f, 0.2f);
    [SerializeField] private Vector2 originalPlayerPickableTriggerCollider = new Vector2(0.25f, 0.25f);

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D groundBounceCollider; // 与地形碰撞的碰撞体
    [SerializeField] private BoxCollider2D playerPickableTriggerCollider; // 被玩家拾取的触发器碰撞体

    [Header("Item Drop")]
    [SerializeField] private Vector2 dropForce = new Vector2(3, 10); // 初速度范围（x 取正负，y 向上）
    [SerializeField] private float rotationSpeed = 360f;            // 初始旋转速度（度/秒）
    [SerializeField] private float rotationDamping = 3f;            // 旋转阻尼（越大越快停）
    [SerializeField] private Vector2 initialRotationAngleRange = new Vector2(0f, 45f); // 初始随机旋转角度范围

    [Header("Settle (落稳判定)")]
    [SerializeField] private LayerMask groundMask;   // Ground 层
    [SerializeField] private float minSettleSpeed = 0.1f;    // 速度低于此阈值才算慢
    [SerializeField] private float freezeDelay = 0.1f; // 物品低于最低速度后多久冻结物品
    [SerializeField] private float settleExtraDelay = 0.05f; // 判稳后再延迟一点点再切触发
    [SerializeField] private float settleCheckCooldown = 0.1f;

    // 内部状态
    private float currentRotationSpeed;
    private bool canRotate = true;
    private float settleCheckTimer;

    [Header("Item Details")]
    [SerializeField] private ItemDataSO itemData;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        sr.enabled = false;
        settleCheckTimer = settleCheckCooldown;
    }

    private void FixedUpdate()
    {
        // 仅负责旋转与减速（弹跳全靠物理材质）
        if (canRotate)
        {
            transform.Rotate(Vector3.forward, currentRotationSpeed * Time.fixedDeltaTime);
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0f, rotationDamping * Time.fixedDeltaTime);
        }
        TryFreezeItem();
    }

    public void SetupObject(ItemDataSO data, bool facingRight)
    {
        itemData = data;

        Vector2 imageSize = data.backpackItemData.imageSize;

        sr.enabled = true;
        sr.sprite = data.itemIcon;

        float bonusScaleMultiplier = Mathf.Sqrt(imageSize.x * imageSize.y);
        sr.transform.localScale = new Vector3(1 / (baseScaleMultiplier * bonusScaleMultiplier), 1 / (baseScaleMultiplier * bonusScaleMultiplier), 1);

        SetColliderSizeByImageSize(groundBounceCollider, originalGroundBounceColliderSize, imageSize);
        SetColliderSizeByImageSize(playerPickableTriggerCollider, originalPlayerPickableTriggerCollider, imageSize);

        float dropForceX = facingRight ? dropForce.x : -dropForce.x;
        rb.velocity = new Vector2(dropForceX, dropForce.y);

        ApplyRandomInitialRotation();
        // 旋转方向跟水平初速同号
        currentRotationSpeed = rotationSpeed * Mathf.Sign(Mathf.Approximately(dropForceX, 0f) ? 1f : dropForceX);
        canRotate = true;

        playerPickableTriggerCollider.enabled = false; // 禁用玩家拾取碰撞触发器

    }

    private void ApplyRandomInitialRotation()
    {
        float minAngle = Mathf.Min(initialRotationAngleRange.x, initialRotationAngleRange.y);
        float maxAngle = Mathf.Max(initialRotationAngleRange.x, initialRotationAngleRange.y);

        float randomAngleSize = Random.Range(minAngle, maxAngle);
        float randomDirection = Random.value < 0.5f ? -1f : 1f;

        rb.rotation = randomAngleSize * randomDirection;
    }

    private void SetColliderSizeByImageSize(Collider2D targetCollider, Vector2 originalSize, Vector2 imageSize)
    {
        if (targetCollider == null)
        {
            return;
        }

        Vector2 newSize = CalculateSameAreaSize(originalSize, imageSize);

        if (targetCollider is BoxCollider2D boxCollider)
        {
            boxCollider.size = newSize;
            return;
        }

        if (targetCollider is CapsuleCollider2D capsuleCollider)
        {
            capsuleCollider.size = newSize;
            return;
        }

        Debug.LogWarning($"{name}: {targetCollider.name} 不是 BoxCollider2D 或 CapsuleCollider2D，无法通过 size 设置 x/y。", this);
    }

    private Vector2 CalculateSameAreaSize(Vector2 originalSize, Vector2 imageSize)
    {
        if (originalSize.x <= 0f || originalSize.y <= 0f)
        {
            return originalSize;
        }

        if (imageSize.x <= 0f || imageSize.y <= 0f)
        {
            return originalSize;
        }

        float originalArea = originalSize.x * originalSize.y;
        float imageArea = imageSize.x * imageSize.y;

        float sizeMultiplier = Mathf.Sqrt(originalArea / imageArea);

        return imageSize * sizeMultiplier;
    }



    private void TryFreezeItem()
    {
        settleCheckTimer -= Time.fixedDeltaTime;
        if (settleCheckTimer <= 0f)
        {
            settleCheckTimer = settleCheckCooldown;
            float speed = rb.velocity.magnitude; // 每次与地形碰撞时，计算此时的速度
            if (speed < minSettleSpeed) // 当达到最低速度阈值时
            {
                StartCoroutine(FreezeCo()); // 一段时间后冻结物品，再一段时间后开启拾取功能
            }
        }
    }

    private IEnumerator SettleCo()
    {
        // 等待一小段时间
        yield return new WaitForSeconds(settleExtraDelay);

        playerPickableTriggerCollider.enabled = true; // 开启玩家拾取
    }

    private IEnumerator FreezeCo()
    {
        yield return new WaitForSeconds(freezeDelay); // 等待一小段时间

        // 冻结物品
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        canRotate = false;

        StartCoroutine(SettleCo()); // 一小段时间后开启玩家拾取
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        InventoryPlayer  playerInventory = other.GetComponent<InventoryPlayer>();
        if (playerInventory == null)
        {
            return;
        }
    
        if(playerInventory.AddItem(itemData)) // 如果成功拾取
        {
            string pickupMessage = "已拾取:" + itemData.itemDisplayName;
            GlobalUI.Instance.hintMessageUI.ShowQuickMessage(pickupMessage);
            Destroy(gameObject);
        }
    }
}
