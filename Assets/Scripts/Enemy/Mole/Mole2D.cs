using UnityEngine;

public class Mole2D : MonsterBase, IAttractedByTreasure, IToyCarAttractable, ITorchRepellable
{
    [Header("鼹鼠属性配置")]
    public float moveSpeed = 2.5f;
    [Tooltip("脚底相对格子中心的 Y 偏移")]
    public float feetYOffset = RobotGroundPath.DefaultFeetYOffset;
    public float playerCheckRadius = 5f;
    public LayerMask playerLayer;

    [Header("动画")]
    public MoleAni moleAni;

    [Header("偷取")]
    [SerializeField] private MoleStealController stealController;

    public IMoleStealHandler StealHandler => stealController;

    [Header("宝物")]
    [SerializeField] private MoleTreasureCollector treasureCollector;

    public MoleTreasureCollector TreasureCollector => treasureCollector;

    [Header("Attraction")]
    public float detectRadius = 8f;

    [Header("当前状态数据（由 AI 与 Motor 维护）")]
    public int idleArrivalCount = 0;
    public float stealTimer = 0f;
    public MoleCave currentHomeCave;

    private MoleUtilityAI moleAI;
    private bool stompPausedLastFrame;

    protected override void Init()
    {
        moleAI = new MoleUtilityAI(this);
        ai = moleAI;
        motor = new MoleMotor(this);

        if (moleAni == null)
        {
            moleAni = GetComponent<MoleAni>();
        }

        if (moleAni == null)
        {
            moleAni = GetComponentInChildren<MoleAni>(true);
        }

        if (stealController == null)
        {
            stealController = GetComponent<MoleStealController>();
        }

        if (treasureCollector == null)
        {
            treasureCollector = GetComponent<MoleTreasureCollector>();
        }

        ResolveHomeCave();

        idleArrivalCount = 0;
        stealTimer = 0f;

        Transform headAnchor = transform.Find("Texture");
        EnemyStompReceiver.Ensure(
            this,
            headAnchor != null ? headAnchor : transform,
            new Vector2(0.55f, 0.12f));
    }

    private void Update()
    {
        if (IsStompPaused && !stompPausedLastFrame)
        {
            moleAI?.NotifyStomped();
        }

        stompPausedLastFrame = IsStompPaused;
    }

    private void ResolveHomeCave()
    {
        MoleCaveManager manager = MoleCaveManager.Instance;

        if (manager == null)
        {
            manager = Object.FindObjectOfType<MoleCaveManager>();
        }

        if (manager != null)
        {
            manager.RefreshAllCaves();
        }

        if (currentHomeCave != null)
        {
            PlaceAtCave(currentHomeCave);
            return;
        }

        if (manager == null)
        {
            Debug.LogWarning("Mole2D: 场景中未找到 MoleCaveManager。");
            return;
        }

        currentHomeCave = manager.FindClosestValidCave(Position, feetYOffset);

        if (currentHomeCave == null)
        {
            currentHomeCave = manager.FindClosestCave(Position, feetYOffset);
        }

        if (currentHomeCave != null)
        {
            PlaceAtCave(currentHomeCave);

            if (!MoleCaveManager.CaveHasConnections(currentHomeCave))
            {
                Debug.LogWarning(
                    $"Mole2D: 已绑定洞穴「{currentHomeCave.name}」，但其 connectedCaves 为空。"
                    + "请在 Inspector 中为该洞穴指定至少一个连通洞穴。",
                    currentHomeCave
                );
            }

            return;
        }

        Debug.LogWarning("场景中未找到任何 MoleCave！请放置带 MoleCave 组件的洞穴。");
    }

    public void PlaceAtCave(MoleCave cave)
    {
        if (cave == null)
        {
            return;
        }

        SnapFeetToGround(cave.GetMoleFeetPosition(feetYOffset));
    }

    public void SnapFeetToGround(Vector2 worldPos)
    {
        Vector2 feet = RobotGroundPath.SnapToFlatGround(worldPos, feetYOffset);
        transform.position = new Vector3(feet.x, feet.y, transform.position.z);
    }

    public void SnapFeetToGround()
    {
        SnapFeetToGround(Position);
    }

    public void CompleteSteal(Player player)
    {
        if (StealHandler == null || player == null)
        {
            return;
        }

        InventoryPlayer inventoryPlayer = player.GetComponent<InventoryPlayer>();
        if (inventoryPlayer == null)
        {
            return;
        }

        StealHandler.OnStealFinished(this, inventoryPlayer);
    }

    public void AttractedByTreasure(Vector2 treasurePosition, PickableObject pickableObject)
    {
        if (treasureCollector == null || pickableObject == null)
        {
            return;
        }

        treasureCollector.RegisterTarget(pickableObject);
    }

    public void AttractToToyCar(Vector2 myToyCarPosition)
    {
        moleAI?.ForceAttractionRefresh();
    }

    public void FleeFromTorch(Vector2 torchPosition)
    {
        moleAI?.NotifyRepelledByTorch(torchPosition);
    }
}
