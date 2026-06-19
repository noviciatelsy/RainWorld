using UnityEngine;

public class StalactiteController : MonoBehaviour
{
    [Header("����������")]
    [SerializeField] private GameObject stone0;
    [SerializeField] private GameObject stone1;
    [SerializeField] private GameObject stone2;

    [Header("����")]
    [Tooltip("����ʯ׶�����ٶȣ����絥λ/�룩")]
    public float fallSpeed = 6f;

    [Header("��Ҽ������")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 detectionSize = new Vector2(2f, 10f);
    [SerializeField] private Vector2 detectionOffset = new Vector2(0f, -5f);

    private bool isTriggered;
    private StalactiteStone2 fallingStone;

    private void Awake()
    {
        if (stone0 != null)
        {
            stone0.SetActive(true);
        }

        if (stone1 != null)
        {
            stone1.SetActive(false);
        }

        if (stone2 != null)
        {
            stone2.SetActive(false);
            fallingStone = stone2.GetComponent<StalactiteStone2>();
        }

        ResolvePlayerLayerMask();
    }

    private void OnValidate()
    {
        ResolvePlayerLayerMask();
    }

    private void FixedUpdate()
    {
        if (isTriggered)
        {
            return;
        }

        Vector2 detectCenter = (Vector2)transform.position + detectionOffset;
        Collider2D playerCollider = Physics2D.OverlapBox(detectCenter, detectionSize, 0f, playerLayer);

        if (playerCollider != null)
        {
            TriggerFall();
        }
    }

    private void TriggerFall()
    {
        isTriggered = true;

        if (stone0 != null)
        {
            stone0.SetActive(false);
        }

        if (stone1 != null)
        {
            stone1.SetActive(true);
        }

        if (stone2 != null)
        {
            stone2.SetActive(true);
        }

        if (fallingStone != null)
        {
            fallingStone.BeginFall(fallSpeed);
        }
    }

    private void ResolvePlayerLayerMask()
    {
        if (playerLayer.value != 0)
        {
            return;
        }

        int playerLayerIndex = LayerMask.NameToLayer("Player");
        if (playerLayerIndex >= 0)
        {
            playerLayer = 1 << playerLayerIndex;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 detectCenter = transform.position + (Vector3)detectionOffset;
        Gizmos.DrawWireCube(detectCenter, new Vector3(detectionSize.x, detectionSize.y, 1f));
    }
}
