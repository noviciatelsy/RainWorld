using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鼹鼠洞：鼹鼠 AI 的活动/连通图节点，持有鼹鼠护符时玩家可按 E 传送到随机相邻洞窟。
/// </summary>
public class MoleCave : PlayerSensorTarget
{
    private const string DefaultPromptText = "按E使用鼹鼠洞传送";
    private const int PlayerSensorTargetLayerName = 14;

    [Header("活动区域（鼹鼠）")]
    [Tooltip("该洞窟所辖区域内 Idle 与巡逻等行为范围")]
    public Bounds activityBounds;

    [Header("图结构（互通的洞窟）")]
    [Tooltip("与当前洞窟连通的相邻洞窟列表")]
    public List<MoleCave> connectedCaves = new List<MoleCave>();

    [Header("玩家传送")]
    [SerializeField] private Vector2 teleportOffset = new Vector2(0f, 0.35f);

    private MainInput mainInput;
    private int playerSensorOverlapCount;
    private int lastTeleportFrame = -1;

    public Vector2 Position => (Vector2)transform.position + teleportOffset;

    /// <summary>鼹鼠 AI 用的洞口脚底世界坐标（与 feetYOffset 贴地逻辑一致，不含玩家传送偏移）。</summary>
    public Vector2 GetMoleFeetPosition(float feetYOffset = RobotGroundPath.DefaultFeetYOffset)
    {
        return RobotGroundPath.SnapToFlatGround((Vector2)transform.position, feetYOffset);
    }

    public bool IsMoleAtEntrance(Vector2 molePosition, float feetYOffset, float maxDistance = 0.35f)
    {
        return Vector2.Distance(molePosition, GetMoleFeetPosition(feetYOffset)) <= maxDistance;
    }

    protected override void Awake()
    {
        EnsureInteractionSetup();

        base.Awake();

        mainInput = InputManager.Instance != null
            ? InputManager.Instance.mainInput
            : null;

        if (displayText != null && string.IsNullOrWhiteSpace(displayText.text))
        {
            displayText.text = DefaultPromptText;
        }
    }

    private void OnEnable()
    {
        MoleCaveManager.Instance?.RegisterCave(this);
    }

    private void Start()
    {
        MoleCaveManager.Instance?.RegisterCave(this);
    }

    private void OnDisable()
    {
        MoleCaveManager.Instance?.UnregisterCave(this);
    }

    private void Update()
    {
        if (playerSensorOverlapCount <= 0 || mainInput == null)
        {
            return;
        }

        if (ElevatorInputGate.IsBlocking)
        {
            return;
        }

        if (!mainInput.Player.Interact.WasPerformedThisFrame())
        {
            return;
        }

        TryUseCave();
    }

    public override void Interact()
    {
        TryUseCave();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsPlayerSensorCollider(collision))
        {
            return;
        }

        playerSensorOverlapCount++;
        RefreshPromptVisibility();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsPlayerSensorCollider(collision))
        {
            return;
        }

        RefreshPromptVisibility();
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsPlayerSensorCollider(collision))
        {
            return;
        }

        playerSensorOverlapCount = Mathf.Max(0, playerSensorOverlapCount - 1);
        RefreshPromptVisibility();
    }

    /// <summary>
    /// 在 Inspector 中手动建立双向连接的辅助方法。
    /// </summary>
    public void AddConnection(MoleCave other)
    {
        if (other == null || other == this)
        {
            return;
        }

        if (!connectedCaves.Contains(other))
        {
            connectedCaves.Add(other);
        }

        if (!other.connectedCaves.Contains(this))
        {
            other.connectedCaves.Add(this);
        }
    }

    private void EnsureInteractionSetup()
    {
        int targetLayer = LayerMask.NameToLayer("PlayerSensorTarget");
        if (targetLayer >= 0)
        {
            gameObject.layer = targetLayer;
        }
        else if (gameObject.layer != PlayerSensorTargetLayerName)
        {
            gameObject.layer = PlayerSensorTargetLayerName;
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.offset = new Vector2(0f, 0.25f);
            box.size = new Vector2(1f, 0.5f);
            return;
        }

        collider.isTrigger = true;
    }

    private void RefreshPromptVisibility()
    {
        if (displayText == null)
        {
            return;
        }

        bool shouldShow = playerSensorOverlapCount > 0 && CanPlayerUseCave(GetCurrentPlayer());
        displayText.gameObject.SetActive(shouldShow);
    }

    private void TryUseCave()
    {
        if (Time.frameCount == lastTeleportFrame)
        {
            return;
        }

        Player player = GetCurrentPlayer();
        if (!CanPlayerUseCave(player))
        {
            return;
        }

        MoleCave destination = MoleCaveManager.Instance != null
            ? MoleCaveManager.Instance.GetRandomAdjacentCave(this)
            : null;

        if (destination == null)
        {
            Debug.LogWarning($"{name} 传送失败：没有可用的相邻鼹鼠洞，请在 connectedCaves 中配置连通关系。");
            return;
        }

        TeleportPlayer(player, destination);
        lastTeleportFrame = Time.frameCount;
        RefreshPromptVisibility();
    }

    private static bool IsPlayerSensorCollider(Collider2D collision)
    {
        return collision != null && collision.GetComponent<PlayerSensor>() != null;
    }

    private static Player GetCurrentPlayer()
    {
        return PlayerManager.Instance != null
            ? PlayerManager.Instance.TryGetCurrentPlayer()
            : null;
    }

    private static bool CanPlayerUseCave(Player player)
    {
        if (player == null)
        {
            return false;
        }

        MoleAmuletPassiveEffect effect = player.GetComponentInChildren<MoleAmuletPassiveEffect>(true);
        if (effect != null && effect.canUseMoleCave)
        {
            return true;
        }

        InventoryPlayer inventory = player.GetComponent<InventoryPlayer>();
        if (inventory == null || inventory.inventoryItems == null)
        {
            return false;
        }

        for (int i = 0; i < inventory.inventoryItems.Count; i++)
        {
            InventoryItem item = inventory.inventoryItems[i];
            if (item == null || item.ItemData == null || item.itemEffect == null)
            {
                continue;
            }

            if (item.itemEffect is ItemEffectDataSO_MoleAmulet)
            {
                if (effect != null)
                {
                    effect.EnableEffect();
                }

                return true;
            }
        }

        return false;
    }

    private static void TeleportPlayer(Player player, MoleCave destination)
    {
        if (player == null || destination == null)
        {
            return;
        }

        player.transform.position = destination.Position;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(activityBounds.center, activityBounds.size);

        Gizmos.color = new Color(0.6f, 0.2f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.3f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Position, 0.15f);

        if (connectedCaves == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        foreach (MoleCave neighbor in connectedCaves)
        {
            if (neighbor != null && GetInstanceID() < neighbor.GetInstanceID())
            {
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}
