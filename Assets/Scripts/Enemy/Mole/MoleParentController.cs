using UnityEngine;

/// <summary>
/// 鼹鼠爷爷：识别区域内丢弃的宝物，吸收达到数量后永久进入开心状态。
/// </summary>
[DisallowMultipleComponent]
public class MoleParentController : MonoBehaviour
{
    [Header("Save")]
    [Tooltip("用于存档的唯一 ID，同场景多个实例需不同")]
    [SerializeField] private string moleParentSaveID = "MoleParent";

    [Header("Treasure Detection")]
    [Tooltip("宝物识别半径")]
    [SerializeField] private float treasureDetectRadius = 8f;

    [Tooltip("触发开心状态所需吸收的宝物数量")]
    [SerializeField] private int requiredTreasureCount = 1;

    [Tooltip("可识别的宝物 ItemData")]
    [SerializeField] private ItemDataSO[] targetTreasures;

    [Tooltip("是否接受所有 ItemType.Treasure")]
    [SerializeField] private bool acceptAnyTreasureType = true;

    [Header("Mole Charm Detection")]
    [Tooltip("鼹鼠护符 ItemData；留空则按名称包含「护符」识别")]
    [SerializeField] private ItemDataSO[] moleCharmItems;

    [SerializeField] private float detectInterval = 0.25f;

    [Header("Happy Landing")]
    [Tooltip("开心动画结束后根节点的世界坐标")]
    [SerializeField] private Vector2 happyLandingWorldPosition;

    [Header("Destructible Wall")]
    [Tooltip("开心下落开始时破坏的墙壁（场景内由 Prefab 放置的实例）")]
    [SerializeField] private DestructibleWall destructibleWall;

    [Tooltip("是否永久破坏（对应 DestructibleWall.isPermanentDestroy）")]
    [SerializeField] private bool permanentWallDestroy = true;

    [Header("References")]
    [SerializeField] private MoleParentAni moleParentAni;

    [Header("图鉴解锁")]
    [SerializeField] private EnemyInformationDataSO enemyInformationData;
    [SerializeField] private float enemyInformationUnlockRadius = 8f;

    private int absorbedTreasureCount;
    private float detectTimer;
    private bool kinSenseIntelUnlocked;
    private bool isSubscribedToSaveManager;
    private Vector3 initialWorldPosition;

    public bool IsHappy => moleParentAni != null && moleParentAni.IsHappy;

    private void Awake()
    {
        initialWorldPosition = transform.position;

        if (moleParentAni == null)
        {
            moleParentAni = GetComponent<MoleParentAni>();
        }
    }

    private void Start()
    {
        TrySubscribeSaveManager();
        LoadHappyStateFromSave();
        EnemyInformationUnlockRangeTrigger.Ensure(gameObject, enemyInformationData, enemyInformationUnlockRadius);
    }

    private void OnDestroy()
    {
        UnsubscribeSaveManager();
    }

    private void Update()
    {
        if (IsHappy || moleParentAni != null && moleParentAni.IsPlayingHappySequence)
        {
            return;
        }

        detectTimer -= Time.deltaTime;

        if (detectTimer > 0f)
        {
            return;
        }

        detectTimer = Mathf.Max(0.05f, detectInterval);
        ScanAndAbsorbTreasures();
        ScanForMoleCharm();
    }

    private void ScanAndAbsorbTreasures()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, treasureDetectRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            PickableObject pickable = hit.GetComponentInParent<PickableObject>();

            if (!IsValidDiscardedTreasure(pickable))
            {
                continue;
            }

            AbsorbTreasure(pickable);

            if (absorbedTreasureCount >= requiredTreasureCount)
            {
                TriggerPermanentHappyState();
                return;
            }
        }
    }

    private void AbsorbTreasure(PickableObject treasure)
    {
        if (treasure == null)
        {
            return;
        }

        Destroy(treasure.gameObject);
        absorbedTreasureCount++;

        if (absorbedTreasureCount == 1)
        {
            EnemyIntelligenceUnlockUtility.TryUnlockByName(EnemyIntelligenceNames.MoleParentCollection);
        }
    }

    private void ScanForMoleCharm()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, treasureDetectRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            PickableObject pickable = hit.GetComponentInParent<PickableObject>();

            if (!IsValidDiscardedMoleCharm(pickable))
            {
                continue;
            }

            AbsorbMoleCharm(pickable);
            return;
        }
    }

    private void AbsorbMoleCharm(PickableObject charm)
    {
        if (charm == null)
        {
            return;
        }

        Destroy(charm.gameObject);

        if (!kinSenseIntelUnlocked)
        {
            kinSenseIntelUnlocked = true;
            EnemyIntelligenceUnlockUtility.TryUnlockByName(EnemyIntelligenceNames.MoleParentKinSense);
        }

        TriggerPermanentHappyState();
    }

    private void TriggerPermanentHappyState()
    {
        if (moleParentAni == null || IsHappy || moleParentAni.IsPlayingHappySequence)
        {
            return;
        }

        if (IsHappyTriggeredInSave())
        {
            moleParentAni.ApplyPermanentHappyStateImmediate(
                happyLandingWorldPosition,
                destructibleWall,
                permanentWallDestroy);
            return;
        }

        SaveHappyStateToRunData();

        EnemyMoleParentAudioEmitter audioEmitter = GetComponent<EnemyMoleParentAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.PlayWake();
        }

        moleParentAni.EnterPermanentHappyState(
            happyLandingWorldPosition,
            destructibleWall,
            permanentWallDestroy);
    }

    private void LoadHappyStateFromSave()
    {
        if (moleParentAni == null)
        {
            return;
        }

        if (IsHappyTriggeredInSave())
        {
            if (!IsHappy)
            {
                moleParentAni.ApplyPermanentHappyStateImmediate(
                    happyLandingWorldPosition,
                    destructibleWall,
                    permanentWallDestroy);
            }

            return;
        }

        if (IsHappy || moleParentAni.IsPlayingHappySequence)
        {
            moleParentAni.ResetToSleepState(initialWorldPosition);
        }
    }

    private bool IsHappyTriggeredInSave()
    {
        if (string.IsNullOrWhiteSpace(moleParentSaveID) || SaveManager.Instance == null)
        {
            return false;
        }

        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return false;
        }

        runData.EnsureDataValid();
        return runData.moleParentHappyTriggeredIds.Contains(moleParentSaveID);
    }

    private void SaveHappyStateToRunData()
    {
        if (string.IsNullOrWhiteSpace(moleParentSaveID) || SaveManager.Instance == null)
        {
            return;
        }

        GameRunData runData = SaveManager.Instance.GetRunTimeGameData();
        if (runData == null)
        {
            return;
        }

        runData.EnsureDataValid();

        if (runData.moleParentHappyTriggeredIds == null)
        {
            runData.moleParentHappyTriggeredIds = new System.Collections.Generic.List<string>();
        }

        if (runData.moleParentHappyTriggeredIds.Contains(moleParentSaveID))
        {
            return;
        }

        runData.moleParentHappyTriggeredIds.Add(moleParentSaveID);
        SaveManager.Instance.SaveGame();
    }

    private void TrySubscribeSaveManager()
    {
        if (isSubscribedToSaveManager || SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnGameRunDataOverwrite += LoadHappyStateFromSave;
        SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;
        isSubscribedToSaveManager = true;
    }

    private void UnsubscribeSaveManager()
    {
        if (!isSubscribedToSaveManager || SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnGameRunDataOverwrite -= LoadHappyStateFromSave;
        SaveManager.Instance.OnCurrentGameRunDataChanged -= HandleCurrentGameRunDataChanged;
        isSubscribedToSaveManager = false;
    }

    private void HandleCurrentGameRunDataChanged(int slotIndex, GameRunData runData)
    {
        LoadHappyStateFromSave();
    }

    private bool IsValidDiscardedTreasure(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null)
        {
            return false;
        }

        if (!pickable.IsSettledOnGround)
        {
            return false;
        }

        if (acceptAnyTreasureType && pickable.ItemData.itemType == ItemType.Treasure)
        {
            return true;
        }

        if (targetTreasures == null || targetTreasures.Length == 0)
        {
            return false;
        }

        ItemDataSO itemData = pickable.ItemData;

        for (int i = 0; i < targetTreasures.Length; i++)
        {
            if (targetTreasures[i] == itemData)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidDiscardedMoleCharm(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null)
        {
            return false;
        }

        if (!pickable.IsSettledOnGround)
        {
            return false;
        }

        ItemDataSO itemData = pickable.ItemData;

        if (moleCharmItems != null && moleCharmItems.Length > 0)
        {
            for (int i = 0; i < moleCharmItems.Length; i++)
            {
                if (moleCharmItems[i] == itemData)
                {
                    return true;
                }
            }

            return false;
        }

        string itemName = itemData.name;
        return itemName.Contains("护符") || itemName.IndexOf("Amulet", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, treasureDetectRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(happyLandingWorldPosition, 0.2f);
    }
#endif
}
