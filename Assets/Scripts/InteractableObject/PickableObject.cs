using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class PickableObject : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("��Ʒ��С��ʾ����")]
    [SerializeField] private float baseScaleMultiplier = 3;
    [SerializeField] private Vector2 originalGroundBounceColliderSize = new Vector2(0.2f, 0.2f);
    [SerializeField] private Vector2 originalPlayerPickableTriggerCollider = new Vector2(0.25f, 0.25f);

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D groundBounceCollider; // �������ײ����ײ��
    [SerializeField] private BoxCollider2D playerPickableTriggerCollider; // �����ʰȡ�Ĵ�������ײ��

    [Header("Item Drop")]
    [SerializeField] private Vector2 dropForce = new Vector2(3, 10); // ���ٶȷ�Χ��x ȡ������y ���ϣ�
    [SerializeField] private float rotationSpeed = 360f;            // ��ʼ��ת�ٶȣ���/�룩
    [SerializeField] private float rotationDamping = 3f;            // ��ת���ᣨԽ��Խ��ͣ��
    [SerializeField] private Vector2 initialRotationAngleRange = new Vector2(0f, 45f); // ��ʼ�����ת�Ƕȷ�Χ

    [Header("Settle (�����ж�)")]
    [SerializeField] private LayerMask groundMask;   // Ground ��
    [SerializeField] private float minSettleSpeed = 0.1f;    // �ٶȵ��ڴ���ֵ������
    [SerializeField] private float freezeDelay = 0.1f; // ��Ʒ��������ٶȺ��ö�����Ʒ
    [SerializeField] private float settleExtraDelay = 0.05f; // ���Ⱥ����ӳ�һ������д���
    [SerializeField] private float settleCheckCooldown = 0.1f;

    // �ڲ�״̬
    private float currentRotationSpeed;
    private bool canRotate = true;
    private float settleCheckTimer;

    [Header("Item Details")]
    [SerializeField] private ItemDataSO itemData;

    public ItemDataSO ItemData => itemData;

    public bool IsSettledOnGround =>
        rb != null && rb.constraints == RigidbodyConstraints2D.FreezeAll;

    public event Action onItemStop;
    private bool onItemStopHasTriggered;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        sr.enabled = false;
        settleCheckTimer = settleCheckCooldown;
    }

    private void FixedUpdate()
    {
        // ��������ת����٣�����ȫ���������ʣ�
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
        // ��ת�����ˮƽ����ͬ��
        currentRotationSpeed = rotationSpeed * Mathf.Sign(Mathf.Approximately(dropForceX, 0f) ? 1f : dropForceX);
        canRotate = true;

        playerPickableTriggerCollider.enabled = false; // �������ʰȡ��ײ������

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

        Debug.LogWarning($"{name}: {targetCollider.name} ���� BoxCollider2D �� CapsuleCollider2D���޷�ͨ�� size ���� x/y��", this);
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
        if(canRotate==false)
        {
            return;
        }
        settleCheckTimer -= Time.fixedDeltaTime;
        if (settleCheckTimer <= 0f)
        {
            settleCheckTimer = settleCheckCooldown;
            float speed = rb.velocity.magnitude; // ÿ���������ײʱ�������ʱ���ٶ�
            if (speed < minSettleSpeed) // ���ﵽ����ٶ���ֵʱ
            {
                StartCoroutine(FreezeCo()); // һ��ʱ��󶳽���Ʒ����һ��ʱ�����ʰȡ����
            }
        }
    }

    private IEnumerator SettleCo()
    {
        // �ȴ�һС��ʱ��
        yield return new WaitForSeconds(settleExtraDelay);

        playerPickableTriggerCollider.enabled = true; // �������ʰȡ
        if(!onItemStopHasTriggered)
        {
            onItemStop?.Invoke();
            onItemStopHasTriggered = true;
        }
    }

    private IEnumerator FreezeCo()
    {
        yield return new WaitForSeconds(freezeDelay); // �ȴ�һС��ʱ��

        // ������Ʒ
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        canRotate = false;
        StartCoroutine(SettleCo()); // һС��ʱ��������ʰȡ
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        InventoryPlayer  playerInventory = other.GetComponent<InventoryPlayer>();
        if (playerInventory == null)
        {
            return;
        }
    
        if(playerInventory.AddItem(itemData)) // ����ɹ�ʰȡ
        {
            string pickupMessage = "��ʰȡ:" + itemData.itemDisplayName;
            GlobalUI.Instance.hintMessageUI.ShowQuickMessage(pickupMessage);
            Destroy(gameObject);
        }
    }
}
