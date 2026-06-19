using System;
using System.Collections.Generic;
using UnityEngine;

public enum ArchiveUnlockType
{
    Intelligence,
    Enemy,
    EnemyIntelligence,
    EnemyPicture
}

public class ArchiveUnlockRecord
{
    public ArchiveUnlockType unlockType;
    public IntelligenceDataSO intelligenceData;
    public EnemyInformationDataSO enemyInformationData;
    public EnemyIntelligenceDataSO enemyIntelligenceData;

    public static ArchiveUnlockRecord CreateIntelligenceRecord(IntelligenceDataSO intelligenceData)
    {
        return new ArchiveUnlockRecord
        {
            unlockType = ArchiveUnlockType.Intelligence,
            intelligenceData = intelligenceData
        };
    }

    public static ArchiveUnlockRecord CreateEnemyRecord(EnemyInformationDataSO enemyInformationData)
    {
        return new ArchiveUnlockRecord
        {
            unlockType = ArchiveUnlockType.Enemy,
            enemyInformationData = enemyInformationData
        };
    }

    public static ArchiveUnlockRecord CreateEnemyIntelligenceRecord(EnemyInformationDataSO enemyInformationData, EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        return new ArchiveUnlockRecord
        {
            unlockType = ArchiveUnlockType.EnemyIntelligence,
            enemyInformationData = enemyInformationData,
            enemyIntelligenceData = enemyIntelligenceData
        };
    }

    public static ArchiveUnlockRecord CreateEnemyPictureRecord(EnemyInformationDataSO enemyInformationData)
    {
        return new ArchiveUnlockRecord
        {
            unlockType = ArchiveUnlockType.EnemyPicture,
            enemyInformationData = enemyInformationData
        };
    }
}

public class IntelligenceArchiveManager : MonoBehaviour
{
    public static IntelligenceArchiveManager Instance { get; private set; }

    [Header("DataBase References")]
    [SerializeField] private IntelligenceDataBaseSO intelligenceDataBase;
    [SerializeField] private EnemyInformationDataBaseSO enemyInformationDataBase;
    [SerializeField] private EnemyIntelligenceDataBaseSO enemyIntelligenceDataBase;

    [Header("Unlock Settings")]
    [SerializeField] private bool saveImmediatelyWhenUnlock = true;

    [Tooltip("解锁敌人专属情报时，是否顺便解锁这个敌人的图鉴页")]
    [SerializeField] private bool autoUnlockEnemyWhenUnlockEnemyIntelligence = true;

    [Tooltip("解锁敌人照片时，是否顺便解锁这个敌人的图鉴页。")]
    [SerializeField] private bool autoUnlockEnemyWhenUnlockEnemyPicture = true;

    //[Header("Test")]
    //[SerializeField] private IntelligenceDataSO test;

    private GameRunData gameRunData;

    private readonly HashSet<string> unlockedIntelligenceIDSet = new HashSet<string>();
    private readonly HashSet<string> unlockedEnemyIDSet = new HashSet<string>();
    private readonly HashSet<string> unlockedEnemyIntelligenceIDSet = new HashSet<string>();
    private readonly HashSet<string> unlockedEnemyPictureIDSet = new HashSet<string>();

    public event Action<IntelligenceDataSO> OnIntelligenceUnlocked;
    public event Action<EnemyInformationDataSO> OnEnemyUnlocked;
    public event Action<EnemyIntelligenceDataSO> OnEnemyIntelligenceUnlocked;
    public event Action<EnemyInformationDataSO> OnEnemyPictureUnlocked;

    // 统一事件：给 UI 使用，告诉图鉴“刚刚新增了哪类条目”
    public event Action<ArchiveUnlockRecord> OnArchiveEntryUnlocked;

    private bool hasInitializedFromSave = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        hasInitializedFromSave = true;
        SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;
    }

    private void OnEnable()
    {
        if (!hasInitializedFromSave)
        {
            return;
        }
        SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;
    }

    private void OnDisable()
    {
        SaveManager.Instance.OnCurrentGameRunDataChanged -= HandleCurrentGameRunDataChanged;
    }

    private void HandleCurrentGameRunDataChanged(int mySlotIndex, GameRunData myRunData)
    {
        gameRunData = myRunData;
        if (gameRunData != null)
        {
            EnsureGameDataLists();
            RebuildRuntimeCache();
        }
    }

    private bool TryPrepareGameData()
    {
        if (gameRunData == null)
        {
            Debug.LogWarning("无法操作图鉴数据：GameData 为空。");
            return false;
        }

        EnsureGameDataLists();
        return true;
    }

    private void EnsureGameDataLists()
    {
        if (gameRunData.unlockedIntelligences == null)
        {
            gameRunData.unlockedIntelligences = new List<string>();
        }

        if (gameRunData.unlockedEnemyIntelligences == null)
        {
            gameRunData.unlockedEnemyIntelligences = new List<string>();
        }

        if (gameRunData.unlockedEnemies == null)
        {
            gameRunData.unlockedEnemies = new List<string>();
        }

        if (gameRunData.unlockedEnemyPicture == null)
        {
            gameRunData.unlockedEnemyPicture = new SerializableDictionary<string, bool>();
        }
    }

    private void RebuildRuntimeCache()
    {
        unlockedIntelligenceIDSet.Clear();
        unlockedEnemyIDSet.Clear();
        unlockedEnemyIntelligenceIDSet.Clear();
        unlockedEnemyPictureIDSet.Clear();

        AddIDsToSet(gameRunData.unlockedIntelligences, unlockedIntelligenceIDSet);
        AddIDsToSet(gameRunData.unlockedEnemies, unlockedEnemyIDSet);
        AddIDsToSet(gameRunData.unlockedEnemyIntelligences, unlockedEnemyIntelligenceIDSet);
        AddUnlockedPictureIDsToSet(gameRunData.unlockedEnemyPicture, unlockedEnemyPictureIDSet);
    }

    private void AddIDsToSet(List<string> sourceList, HashSet<string> targetSet)
    {
        if (sourceList == null)
        {
            return;
        }

        for (int i = 0; i < sourceList.Count; i++)
        {
            string id = sourceList[i];

            if (!string.IsNullOrEmpty(id))
            {
                targetSet.Add(id);
            }
        }
    }

    private void AddUnlockedPictureIDsToSet(SerializableDictionary<string, bool> sourceDictionary, HashSet<string> targetSet)
    {
        if (sourceDictionary == null)
        {
            return;
        }

        foreach (KeyValuePair<string, bool> pair in sourceDictionary)
        {
            if (!string.IsNullOrEmpty(pair.Key) && pair.Value)
            {
                targetSet.Add(pair.Key);
            }
        }
    }

    // 解锁普通情报
    public bool UnlockIntelligence(IntelligenceDataSO intelligenceData)
    {
        if (intelligenceData == null)
        {
            Debug.LogWarning("解锁普通情报失败：传入的 IntelligenceDataSO 为空。");
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        bool unlocked = AddUnlockID(
            intelligenceData.SaveID,
            gameRunData.unlockedIntelligences,
            unlockedIntelligenceIDSet
        );

        if (unlocked)
        {
            OnIntelligenceUnlocked?.Invoke(intelligenceData);
            OnArchiveEntryUnlocked?.Invoke(ArchiveUnlockRecord.CreateIntelligenceRecord(intelligenceData));
        }

        return unlocked;
    }

    // 解锁敌人图鉴页
    public bool UnlockEnemy(EnemyInformationDataSO enemyInformationData)
    {
        return UnlockEnemyInternal(enemyInformationData, true);
    }

    private bool UnlockEnemyInternal(EnemyInformationDataSO enemyInformationData, bool notify)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("解锁敌人图鉴失败：传入的 EnemyInformationDataSO 为空。");
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        bool unlocked = AddUnlockID(
            enemyInformationData.SaveID,
            gameRunData.unlockedEnemies,
            unlockedEnemyIDSet
        );

        if (unlocked && notify)
        {
            OnEnemyUnlocked?.Invoke(enemyInformationData);
            OnArchiveEntryUnlocked?.Invoke(ArchiveUnlockRecord.CreateEnemyRecord(enemyInformationData));
        }

        return unlocked;
    }

    // 解锁敌人照片
    public bool UnlockEnemyPicture(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("解锁敌人照片失败：传入的 EnemyInformationDataSO 为空。");
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        if (autoUnlockEnemyWhenUnlockEnemyPicture)
        {
            // 静默解锁敌人页，不额外弹一次“敌人解锁”的图鉴
            UnlockEnemyInternal(enemyInformationData, false);
        }

        bool unlocked = AddUnlockFlag(
            enemyInformationData.SaveID,
            gameRunData.unlockedEnemyPicture,
            unlockedEnemyPictureIDSet
        );

        if (unlocked)
        {
            OnEnemyPictureUnlocked?.Invoke(enemyInformationData);
            OnArchiveEntryUnlocked?.Invoke(ArchiveUnlockRecord.CreateEnemyPictureRecord(enemyInformationData));
        }

        return unlocked;
    }

    // 只解锁某条敌人专属情报，不指定它属于哪个敌人
    public bool UnlockEnemyIntelligence(EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        EnemyInformationDataSO ownerEnemyData = FindEnemyInformationByEnemyIntelligence(enemyIntelligenceData);
        return UnlockEnemyIntelligenceInternal(ownerEnemyData, enemyIntelligenceData, true);
    }

    // 解锁某个敌人的某条专属情报
    public bool UnlockEnemyIntelligence(EnemyInformationDataSO enemyInformationData, EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("解锁敌人情报失败：传入的 EnemyInformationDataSO 为空。");
            return false;
        }

        if (enemyIntelligenceData == null)
        {
            Debug.LogWarning("解锁敌人情报失败：传入的 EnemyIntelligenceDataSO 为空。");
            return false;
        }

        if (!enemyInformationData.ContainsEnemyIntelligence(enemyIntelligenceData))
        {
            Debug.LogWarning($"解锁敌人情报失败：{enemyIntelligenceData.name} 不属于敌人 {enemyInformationData.name}。");
            return false;
        }

        return UnlockEnemyIntelligenceInternal(enemyInformationData, enemyIntelligenceData, true);
    }

    private bool UnlockEnemyIntelligenceInternal(EnemyInformationDataSO enemyInformationData, EnemyIntelligenceDataSO enemyIntelligenceData, bool notify)
    {
        if (enemyIntelligenceData == null)
        {
            Debug.LogWarning("解锁敌人情报失败：传入的 EnemyIntelligenceDataSO 为空。");
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        if (enemyInformationData != null && autoUnlockEnemyWhenUnlockEnemyIntelligence)
        {
            // 静默解锁敌人页，不额外弹一次“敌人解锁”的图鉴
            UnlockEnemyInternal(enemyInformationData, false);
        }

        bool unlocked = AddUnlockID(
            enemyIntelligenceData.SaveID,
            gameRunData.unlockedEnemyIntelligences,
            unlockedEnemyIntelligenceIDSet
        );

        if (unlocked && notify)
        {
            OnEnemyIntelligenceUnlocked?.Invoke(enemyIntelligenceData);
            OnArchiveEntryUnlocked?.Invoke(ArchiveUnlockRecord.CreateEnemyIntelligenceRecord(enemyInformationData, enemyIntelligenceData));
        }

        return unlocked;
    }

    // 解锁某个敌人的全部专属情报
    public int UnlockAllEnemyIntelligences(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("解锁全部敌人情报失败：传入的 EnemyInformationDataSO 为空。");
            return 0;
        }

        if (enemyInformationData.enemyIntelligences == null)
        {
            return 0;
        }

        int unlockCount = 0;

        if (autoUnlockEnemyWhenUnlockEnemyIntelligence)
        {
            UnlockEnemyInternal(enemyInformationData, false);
        }

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            // 批量解锁时不逐条弹图鉴，不然可能连开好多次，很烦人
            if (UnlockEnemyIntelligenceInternal(enemyInformationData, enemyIntelligenceData, false))
            {
                unlockCount++;
            }
        }

        if (unlockCount > 0)
        {
            OnArchiveEntryUnlocked?.Invoke(ArchiveUnlockRecord.CreateEnemyRecord(enemyInformationData));
        }

        return unlockCount;
    }

    private bool AddUnlockID(string saveID, List<string> targetList, HashSet<string> targetSet)
    {
        if (string.IsNullOrEmpty(saveID))
        {
            Debug.LogWarning("解锁失败：目标数据的 SaveID 为空。");
            return false;
        }

        if (targetSet.Contains(saveID))
        {
            return false;
        }

        targetSet.Add(saveID);
        targetList.Add(saveID);

        SaveIfNeeded();

        return true;
    }

    private bool AddUnlockFlag(string saveID, SerializableDictionary<string, bool> targetDictionary, HashSet<string> targetSet)
    {
        if (string.IsNullOrEmpty(saveID))
        {
            Debug.LogWarning("解锁失败：目标数据的 SaveID 为空。");
            return false;
        }

        if (targetSet.Contains(saveID))
        {
            return false;
        }

        targetSet.Add(saveID);
        targetDictionary[saveID] = true;

        SaveIfNeeded();

        return true;
    }

    private void SaveIfNeeded()
    {
        if (!saveImmediatelyWhenUnlock)
        {
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("图鉴解锁成功，但保存失败：找不到 SaveManager。");
            return;
        }

        SaveManager.Instance.SaveGame();
    }

    public void SaveArchiveData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("保存图鉴数据失败：找不到 SaveManager。");
            return;
        }

        SaveManager.Instance.SaveGame();
    }

    // 查询：普通情报是否已解锁
    public bool IsIntelligenceUnlocked(IntelligenceDataSO intelligenceData)
    {
        if (intelligenceData == null)
        {
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        return unlockedIntelligenceIDSet.Contains(intelligenceData.SaveID);
    }

    // 查询：敌人图鉴页是否已解锁
    public bool IsEnemyUnlocked(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        return unlockedEnemyIDSet.Contains(enemyInformationData.SaveID);
    }

    // 查询：敌人专属情报是否已解锁
    public bool IsEnemyIntelligenceUnlocked(EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        if (enemyIntelligenceData == null)
        {
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        return unlockedEnemyIntelligenceIDSet.Contains(enemyIntelligenceData.SaveID);
    }

    // 查询：敌人照片是否已解锁
    public bool IsEnemyPictureUnlocked(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        return unlockedEnemyPictureIDSet.Contains(enemyInformationData.SaveID);
    }

    // 获取所有已解锁的普通情报
    public List<IntelligenceDataSO> GetUnlockedIntelligences()
    {
        List<IntelligenceDataSO> result = new List<IntelligenceDataSO>();

        if (!TryPrepareGameData())
        {
            return result;
        }

        if (intelligenceDataBase == null)
        {
            Debug.LogWarning("获取普通情报失败：IntelligenceDataBaseSO 没有赋值。");
            return result;
        }

        for (int i = 0; i < gameRunData.unlockedIntelligences.Count; i++)
        {
            string saveID = gameRunData.unlockedIntelligences[i];
            IntelligenceDataSO data = intelligenceDataBase.GetIntelligenceData(saveID);

            if (data != null)
            {
                result.Add(data);
            }
        }

        return result;
    }

    // 获取所有已解锁的敌人图鉴页
    public List<EnemyInformationDataSO> GetUnlockedEnemies()
    {
        List<EnemyInformationDataSO> result = new List<EnemyInformationDataSO>();

        if (!TryPrepareGameData())
        {
            return result;
        }

        if (enemyInformationDataBase == null)
        {
            Debug.LogWarning("获取敌人图鉴失败：EnemyInformationDataBaseSO 没有赋值。");
            return result;
        }

        for (int i = 0; i < gameRunData.unlockedEnemies.Count; i++)
        {
            string saveID = gameRunData.unlockedEnemies[i];
            EnemyInformationDataSO data = enemyInformationDataBase.GetEnemyInformationData(saveID);

            if (data != null)
            {
                result.Add(data);
            }
        }

        return result;
    }

    // 获取某个敌人当前已解锁的专属情报
    public List<EnemyIntelligenceDataSO> GetUnlockedEnemyIntelligences(EnemyInformationDataSO enemyInformationData)
    {
        List<EnemyIntelligenceDataSO> result = new List<EnemyIntelligenceDataSO>();

        if (enemyInformationData == null)
        {
            return result;
        }

        if (enemyInformationData.enemyIntelligences == null)
        {
            return result;
        }

        if (!TryPrepareGameData())
        {
            return result;
        }

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            if (enemyIntelligenceData != null && IsEnemyIntelligenceUnlocked(enemyIntelligenceData))
            {
                result.Add(enemyIntelligenceData);
            }
        }

        return result;
    }

    // 获取某个敌人的全部专属情报，UI 可以自己根据 IsEnemyIntelligenceUnlocked 判断显示正文还是“？？？”
    public List<EnemyIntelligenceDataSO> GetAllEnemyIntelligences(EnemyInformationDataSO enemyInformationData)
    {
        List<EnemyIntelligenceDataSO> result = new List<EnemyIntelligenceDataSO>();

        if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
        {
            return result;
        }

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            if (enemyIntelligenceData != null)
            {
                result.Add(enemyIntelligenceData);
            }
        }

        return result;
    }

    public EnemyInformationDataSO FindEnemyInformationByEnemyIntelligence(EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        if (enemyIntelligenceData == null)
        {
            return null;
        }

        if (enemyInformationDataBase == null || enemyInformationDataBase.enemyInformationDataBase == null)
        {
            return null;
        }

        for (int i = 0; i < enemyInformationDataBase.enemyInformationDataBase.Length; i++)
        {
            EnemyInformationDataSO enemyInformationData = enemyInformationDataBase.enemyInformationDataBase[i];

            if (enemyInformationData != null && enemyInformationData.ContainsEnemyIntelligence(enemyIntelligenceData))
            {
                return enemyInformationData;
            }
        }

        return null;
    }

    private class RandomNoteUnlockCandidate
    {
        public IntelligenceDataSO intelligenceData;
        public EnemyInformationDataSO enemyInformationData;
        public EnemyIntelligenceDataSO enemyIntelligenceData;

        public bool IsEnemyIntelligence
        {
            get
            {
                return enemyIntelligenceData != null;
            }
        }
    }

    public ArchiveUnlockRecord UnlockRandomNonImportantIntelligenceByNote()
    {
        if (!TryPrepareGameData())
        {
            return null;
        }

        List<RandomNoteUnlockCandidate> candidates = BuildRandomNoteUnlockCandidates();

        if (candidates.Count <= 0)
        {
            Debug.Log("没有可以通过纸条随机解锁的情报。");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
        RandomNoteUnlockCandidate candidate = candidates[randomIndex];

        if (candidate.IsEnemyIntelligence)
        {
            bool unlocked = UnlockEnemyIntelligence(candidate.enemyInformationData, candidate.enemyIntelligenceData);

            if (unlocked)
            {
                return ArchiveUnlockRecord.CreateEnemyIntelligenceRecord(candidate.enemyInformationData, candidate.enemyIntelligenceData);
            }

            return null;
        }
        else
        {
            bool unlocked = UnlockIntelligence(candidate.intelligenceData);

            if (unlocked)
            {
                return ArchiveUnlockRecord.CreateIntelligenceRecord(candidate.intelligenceData);
            }

            return null;
        }
    }

    private List<RandomNoteUnlockCandidate> BuildRandomNoteUnlockCandidates()
    {
        List<RandomNoteUnlockCandidate> candidates = new List<RandomNoteUnlockCandidate>();

        AddNormalIntelligenceNoteCandidates(candidates);
        AddKnownEnemyIntelligenceNoteCandidates(candidates);

        return candidates;
    }

    private void AddNormalIntelligenceNoteCandidates(List<RandomNoteUnlockCandidate> candidates)
    {
        if (intelligenceDataBase == null || intelligenceDataBase.intelligenceDataBase == null)
        {
            return;
        }

        for (int i = 0; i < intelligenceDataBase.intelligenceDataBase.Length; i++)
        {
            IntelligenceDataSO intelligenceData = intelligenceDataBase.intelligenceDataBase[i];

            if (intelligenceData == null)
            {
                continue;
            }

            if (intelligenceData.isImportant)
            {
                continue;
            }

            if (!intelligenceData.canBeLockedByNote)
            {
                continue;
            }

            if (IsIntelligenceUnlocked(intelligenceData))
            {
                continue;
            }

            candidates.Add(new RandomNoteUnlockCandidate
            {
                intelligenceData = intelligenceData
            });
        }
    }

    private void AddKnownEnemyIntelligenceNoteCandidates(List<RandomNoteUnlockCandidate> candidates)
    {
        List<EnemyInformationDataSO> unlockedEnemies = GetUnlockedEnemies();

        for (int i = 0; i < unlockedEnemies.Count; i++)
        {
            EnemyInformationDataSO enemyInformationData = unlockedEnemies[i];

            if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
            {
                continue;
            }

            for (int j = 0; j < enemyInformationData.enemyIntelligences.Length; j++)
            {
                EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[j];

                if (enemyIntelligenceData == null)
                {
                    continue;
                }

                if (enemyIntelligenceData.isImportant)
                {
                    continue;
                }

                if (!enemyIntelligenceData.canBeLockedByNote)
                {
                    continue;
                }

                if (IsEnemyIntelligenceUnlocked(enemyIntelligenceData))
                {
                    continue;
                }

                candidates.Add(new RandomNoteUnlockCandidate
                {
                    enemyInformationData = enemyInformationData,
                    enemyIntelligenceData = enemyIntelligenceData
                });
            }
        }
    }

    public List<ArchivePurchaseOffer> GetPurchasableIntelligenceOffers()
    {
        List<ArchivePurchaseOffer> offers = new List<ArchivePurchaseOffer>();

        if (!TryPrepareGameData())
        {
            return offers;
        }

        AddPurchasableNormalIntelligenceOffers(offers);
        AddPurchasableKnownEnemyIntelligenceOffers(offers);

        offers.Sort((a, b) =>
        {
            int priceCompare = a.Price.CompareTo(b.Price);

            if (priceCompare != 0)
            {
                return priceCompare;
            }

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        });

        return offers;
    }

    private void AddPurchasableNormalIntelligenceOffers(List<ArchivePurchaseOffer> offers)
    {
        if (intelligenceDataBase == null || intelligenceDataBase.intelligenceDataBase == null)
        {
            return;
        }

        for (int i = 0; i < intelligenceDataBase.intelligenceDataBase.Length; i++)
        {
            IntelligenceDataSO intelligenceData = intelligenceDataBase.intelligenceDataBase[i];

            if (intelligenceData == null)
            {
                continue;
            }

            if (intelligenceData.isImportant)
            {
                continue;
            }

            if (!intelligenceData.canBePurchased)
            {
                continue;
            }

            if (IsIntelligenceUnlocked(intelligenceData))
            {
                continue;
            }

            offers.Add(ArchivePurchaseOffer.CreateNormalIntelligenceOffer(intelligenceData));
        }
    }

    private void AddPurchasableKnownEnemyIntelligenceOffers(List<ArchivePurchaseOffer> offers)
    {
        List<EnemyInformationDataSO> unlockedEnemies = GetUnlockedEnemies();

        for (int i = 0; i < unlockedEnemies.Count; i++)
        {
            EnemyInformationDataSO enemyInformationData = unlockedEnemies[i];

            if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
            {
                continue;
            }

            for (int j = 0; j < enemyInformationData.enemyIntelligences.Length; j++)
            {
                EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[j];

                if (enemyIntelligenceData == null)
                {
                    continue;
                }

                if (enemyIntelligenceData.isImportant)
                {
                    continue;
                }

                if (!enemyIntelligenceData.canBePurchased)
                {
                    continue;
                }

                if (IsEnemyIntelligenceUnlocked(enemyIntelligenceData))
                {
                    continue;
                }

                offers.Add(ArchivePurchaseOffer.CreateEnemyIntelligenceOffer(enemyInformationData, enemyIntelligenceData));
            }
        }
    }

    public bool IsPurchaseOfferStillAvailable(ArchivePurchaseOffer offer)
    {
        if (offer == null)
        {
            return false;
        }

        if (offer.offerType == ArchivePurchaseOfferType.NormalIntelligence)
        {
            IntelligenceDataSO intelligenceData = offer.intelligenceData;

            if (intelligenceData == null)
            {
                return false;
            }

            if (intelligenceData.isImportant || !intelligenceData.canBePurchased)
            {
                return false;
            }

            return !IsIntelligenceUnlocked(intelligenceData);
        }

        EnemyInformationDataSO enemyInformationData = offer.enemyInformationData;
        EnemyIntelligenceDataSO enemyIntelligenceData = offer.enemyIntelligenceData;

        if (enemyInformationData == null || enemyIntelligenceData == null)
        {
            return false;
        }

        if (!IsEnemyUnlocked(enemyInformationData))
        {
            return false;
        }

        if (enemyIntelligenceData.isImportant || !enemyIntelligenceData.canBePurchased)
        {
            return false;
        }

        return !IsEnemyIntelligenceUnlocked(enemyIntelligenceData);
    }

    public bool ShouldShowExchangeData(IntelligenceExchangeDataSO exchangeData)
    {
        if (exchangeData == null)
        {
            return false;
        }

        if (exchangeData.requiredEnemyInformationData == null)
        {
            return false;
        }

        if (!exchangeData.HasValidReward())
        {
            return false;
        }

        // 只排列已经认识的敌人的交换项
        if (!IsEnemyUnlocked(exchangeData.requiredEnemyInformationData))
        {
            return false;
        }

        // 奖励已经解锁后，就不再显示这个交换项
        if (IsExchangeRewardUnlocked(exchangeData))
        {
            return false;
        }

        return true;
    }

    public bool CanExchangeImportantIntelligence(IntelligenceExchangeDataSO exchangeData)
    {
        if (!ShouldShowExchangeData(exchangeData))
        {
            return false;
        }

        int currentCount = GetEnemyExchangeCollectedCount(exchangeData.requiredEnemyInformationData);
        int requiredCount = Mathf.Max(1, exchangeData.requiredNonImportantEnemyIntelligenceCount);

        return currentCount >= requiredCount;
    }

    public bool TryExchangeImportantIntelligence(IntelligenceExchangeDataSO exchangeData)
    {
        if (!CanExchangeImportantIntelligence(exchangeData))
        {
            return false;
        }

        if (exchangeData.rewardType == IntelligenceExchangeRewardType.NormalIntelligence)
        {
            return UnlockIntelligence(exchangeData.rewardIntelligenceData);
        }

        EnemyInformationDataSO ownerEnemyData = exchangeData.rewardEnemyInformationData;

        if (ownerEnemyData == null)
        {
            ownerEnemyData = FindEnemyInformationByEnemyIntelligence(exchangeData.rewardEnemyIntelligenceData);
        }

        if (ownerEnemyData == null)
        {
            Debug.LogWarning($"交换情报失败：奖励敌人情报 {exchangeData.rewardEnemyIntelligenceData.name} 找不到所属敌人。");
            return false;
        }

        return UnlockEnemyIntelligence(ownerEnemyData, exchangeData.rewardEnemyIntelligenceData);
    }

    public bool IsExchangeRewardUnlocked(IntelligenceExchangeDataSO exchangeData)
    {
        if (exchangeData == null)
        {
            return false;
        }

        if (exchangeData.rewardType == IntelligenceExchangeRewardType.NormalIntelligence)
        {
            return IsIntelligenceUnlocked(exchangeData.rewardIntelligenceData);
        }

        return IsEnemyIntelligenceUnlocked(exchangeData.rewardEnemyIntelligenceData);
    }

    public int GetUnlockedNonImportantEnemyIntelligenceCount(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            if (enemyIntelligenceData == null)
            {
                continue;
            }

            if (enemyIntelligenceData.isImportant)
            {
                continue;
            }

            if (IsEnemyIntelligenceUnlocked(enemyIntelligenceData))
            {
                count++;
            }
        }

        return count;
    }

    public int GetEnemyExchangeCollectedCount(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            return 0;
        }

        int count = GetUnlockedNonImportantEnemyIntelligenceCount(enemyInformationData);

        if (IsEnemyPictureUnlocked(enemyInformationData))
        {
            count++;
        }

        return count;
    }
}