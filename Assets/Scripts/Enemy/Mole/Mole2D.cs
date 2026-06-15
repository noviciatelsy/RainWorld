using UnityEngine;

public class Mole2D : MonsterBase, IAttractedByTreasure
{
    [Header("鼹鼠属性配置")]
    public float moveSpeed = 2.5f;
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

    [Header("当前状态数据（由 AI 与 Motor 维护）")]
    public int idleArrivalCount = 0;
    public float stealTimer = 0f;
    public MoleCave currentHomeCave;

    protected override void Init()
    {
        ai = new MoleUtilityAI(this);
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
            transform.position = currentHomeCave.Position;
            return;
        }

        if (manager == null)
        {
            Debug.LogWarning("Mole2D: 场景中未找到 MoleCaveManager。");
            return;
        }

        currentHomeCave = manager.FindClosestValidCave(Position);

        if (currentHomeCave == null)
        {
            currentHomeCave = manager.FindClosestCave(Position);
        }

        if (currentHomeCave != null)
        {
            transform.position = currentHomeCave.Position;

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
}
