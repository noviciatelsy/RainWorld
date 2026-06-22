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

    [Tooltip("?????????????l????????????????????????")]
    [SerializeField] private bool autoUnlockEnemyWhenUnlockEnemyIntelligence = true;

    [Tooltip("?????????????????????????????????????")]
    [SerializeField] private bool autoUnlockEnemyWhenUnlockEnemyPicture = true;

    [Header("Debug")]
    [SerializeField] private bool enableArchiveDebugLog = true;

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

    // ????????? UI ???????????????????????????????
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

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;
            TryAcquireGameRunData();
        }
    }

    private void OnEnable()
    {
        if (!hasInitializedFromSave)
        {
            return;
        }

        if (SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnCurrentGameRunDataChanged += HandleCurrentGameRunDataChanged;
        TryAcquireGameRunData();
    }

    private void OnDisable()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.OnCurrentGameRunDataChanged -= HandleCurrentGameRunDataChanged;
    }

    private void HandleCurrentGameRunDataChanged(int mySlotIndex, GameRunData myRunData)
    {
        BindCurrentGameRunData(myRunData);
    }

    private void BindCurrentGameRunData(GameRunData myRunData)
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
        TryAcquireGameRunData();

        if (gameRunData == null)
        {
            Debug.LogWarning("?????????? GameRunData ???");
            return false;
        }

        EnsureGameDataLists();
        return true;
    }

    private void TryAcquireGameRunData()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        GameRunData runtimeData = SaveManager.Instance.GetRunTimeGameData();

        if (runtimeData != null)
        {
            BindCurrentGameRunData(runtimeData);
            return;
        }

        int selectedSlotIndex = SaveManager.Instance.CurrentSelectedSlotIndex;

        if (selectedSlotIndex >= 0 && !SaveManager.Instance.IsGameDataSlotEmpty(selectedSlotIndex))
        {
            SaveManager.Instance.SelectGameRunDataSlot(selectedSlotIndex);
            runtimeData = SaveManager.Instance.GetRunTimeGameData();
        }

        if (runtimeData == null)
        {
            for (int slotIndex = 0; slotIndex < GameData.GameDataSlotCount; slotIndex++)
            {
                if (SaveManager.Instance.IsGameDataSlotEmpty(slotIndex))
                {
                    continue;
                }

                SaveManager.Instance.SelectGameRunDataSlot(slotIndex);
                runtimeData = SaveManager.Instance.GetRunTimeGameData();

                if (runtimeData != null)
                {
                    LogArchiveDebug($"TryAcquireGameRunData: auto selected slot {slotIndex}");
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (runtimeData == null)
        {
            for (int slotIndex = 0; slotIndex < GameData.GameDataSlotCount; slotIndex++)
            {
                if (!SaveManager.Instance.IsGameDataSlotEmpty(slotIndex))
                {
                    continue;
                }

                if (SaveManager.Instance.CreateNewGameRunDataInSlot(slotIndex))
                {
                    runtimeData = SaveManager.Instance.GetRunTimeGameData();
                    LogArchiveDebug($"TryAcquireGameRunData: created editor test run in slot {slotIndex}");
                    break;
                }
            }
        }
#endif

        BindCurrentGameRunData(runtimeData);
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

    // ?????????l
    public bool UnlockIntelligence(IntelligenceDataSO intelligenceData)
    {
        if (intelligenceData == null)
        {
            Debug.LogWarning("?????????l????????? IntelligenceDataSO ????");
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

    // ????????????
    public bool UnlockEnemy(EnemyInformationDataSO enemyInformationData)
    {

        return UnlockEnemyInternal(enemyInformationData, true);

    }

    private bool UnlockEnemyInternal(EnemyInformationDataSO enemyInformationData, bool notify)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("???????????????????? EnemyInformationDataSO ????");
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
            GlobalUI.Instance.hintMessageUI.ShowQuickMessage(enemyInformationData.enemyName + "已记入笔记");
        }

        return unlocked;
    }

    // ???????????
    public bool UnlockEnemyPicture(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("???????????????????? EnemyInformationDataSO ????");
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        bool wasEnemyKnown = IsEnemyUnlocked(enemyInformationData);
        EnsureEnemyRecognized(enemyInformationData);

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

        LogArchiveDebug(
            $"UnlockEnemyPicture: {enemyInformationData.enemyName} | " +
            $"photoNew={unlocked} | knownBefore={wasEnemyKnown} | knownNow={IsEnemyUnlocked(enemyInformationData)}"
        );

        return unlocked;
    }

    // ?????????????????l????????????????????
    public bool UnlockEnemyIntelligence(EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        EnemyInformationDataSO ownerEnemyData = FindEnemyInformationByEnemyIntelligence(enemyIntelligenceData);
        return UnlockEnemyIntelligenceInternal(ownerEnemyData, enemyIntelligenceData, true);
    }

    /// <summary>
    /// ? intelligenceName ???????????? Trim??
    /// </summary>
    public bool TryUnlockEnemyIntelligenceByName(string intelligenceName)
    {
        EnemyIntelligenceDataSO enemyIntelligenceData = FindEnemyIntelligenceByName(intelligenceName);

        if (enemyIntelligenceData == null)
        {
            LogArchiveDebug($"TryUnlockEnemyIntelligenceByName: ??????{intelligenceName}?");
            return false;
        }

        return UnlockEnemyIntelligence(enemyIntelligenceData);
    }

    // ????????????????????l
    public bool UnlockEnemyIntelligence(EnemyInformationDataSO enemyInformationData, EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("??????????l????????? EnemyInformationDataSO ????");
            return false;
        }

        if (enemyIntelligenceData == null)
        {
            Debug.LogWarning("??????????l????????? EnemyIntelligenceDataSO ????");
            return false;
        }

        if (!enemyInformationData.ContainsEnemyIntelligence(enemyIntelligenceData))
        {
            Debug.LogWarning($"??????????l????{enemyIntelligenceData.name} ????????? {enemyInformationData.name}??");
            return false;
        }

        return UnlockEnemyIntelligenceInternal(enemyInformationData, enemyIntelligenceData, true);
    }

    private bool UnlockEnemyIntelligenceInternal(EnemyInformationDataSO enemyInformationData, EnemyIntelligenceDataSO enemyIntelligenceData, bool notify)
    {
        if (enemyIntelligenceData == null)
        {
            Debug.LogWarning("??????????l????????? EnemyIntelligenceDataSO ????");
            return false;
        }

        if (!TryPrepareGameData())
        {
            return false;
        }

        if (enemyInformationData == null)
        {
            enemyInformationData = FindEnemyInformationByEnemyIntelligence(enemyIntelligenceData);
        }

        bool wasEnemyKnown = enemyInformationData != null && IsEnemyUnlocked(enemyInformationData);
        EnsureEnemyRecognized(enemyInformationData);

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

        if (unlocked && enemyInformationData != null)
        {
            LogArchiveDebug(
                $"UnlockEnemyIntelligence: {enemyInformationData.enemyName} / {enemyIntelligenceData.intelligenceName} | " +
                $"intelNew={unlocked} | knownBefore={wasEnemyKnown} | knownNow={IsEnemyUnlocked(enemyInformationData)}"
            );
        }

        return unlocked;
    }

    // ????????????????????l
    public int UnlockAllEnemyIntelligences(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            Debug.LogWarning("?????????????l????????? EnemyInformationDataSO ????");
            return 0;
        }

        if (enemyInformationData.enemyIntelligences == null)
        {
            return 0;
        }

        int unlockCount = 0;

        EnsureEnemyRecognized(enemyInformationData);

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            // ????????????????????????????????????????????
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
            Debug.LogWarning("???????????????? SaveID ????");
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
            Debug.LogWarning("???????????????? SaveID ????");
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
            Debug.LogWarning("??????????????????????????? SaveManager??");
            return;
        }

        SaveManager.Instance.SaveGame();
    }

    public void SaveArchiveData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("???????????????????? SaveManager??");
            return;
        }

        SaveManager.Instance.SaveGame();
    }

    // ??????????l????????
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

    // ?????????????????????
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

        string saveID = enemyInformationData.SaveID;

        if (unlockedEnemyIDSet.Contains(saveID))
        {
            return true;
        }

        if (unlockedEnemyPictureIDSet.Contains(saveID))
        {
            return true;
        }

        return HasAnyUnlockedEnemyIntelligence(enemyInformationData);
    }

    // ??????????????l????????
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

    // ????????????????????
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

    // ???????????????????l
    public List<IntelligenceDataSO> GetUnlockedIntelligences()
    {
        List<IntelligenceDataSO> result = new List<IntelligenceDataSO>();

        if (!TryPrepareGameData())
        {
            return result;
        }

        if (intelligenceDataBase == null)
        {
            Debug.LogWarning("????????l????IntelligenceDataBaseSO ????????");
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

    public List<EnemyInformationDataSO> GetUnlockedEnemies()
    {
        List<EnemyInformationDataSO> result = new List<EnemyInformationDataSO>();

        if (!TryPrepareGameData())
        {
            return result;
        }

        if (enemyInformationDataBase == null || enemyInformationDataBase.enemyInformationDataBase == null)
        {
            Debug.LogWarning("获取敌人图鉴失败：EnemyInformationDataBaseSO 没有赋值。");
            return result;
        }

        HashSet<string> addedSaveIDs = new HashSet<string>();

        // 第一阶段：严格按照 gameRunData.unlockedEnemies 的顺序显示。
        // 这个 List 的顺序就是玩家实际解锁敌人的顺序。
        for (int i = 0; i < gameRunData.unlockedEnemies.Count; i++)
        {
            string saveID = gameRunData.unlockedEnemies[i];

            if (string.IsNullOrEmpty(saveID))
            {
                continue;
            }

            EnemyInformationDataSO enemyData = enemyInformationDataBase.GetEnemyInformationData(saveID);

            if (enemyData == null)
            {
                continue;
            }

            if (!IsEnemyUnlocked(enemyData))
            {
                continue;
            }

            if (addedSaveIDs.Add(enemyData.SaveID))
            {
                result.Add(enemyData);
            }
        }

        // 第二阶段：兼容旧存档。
        // 如果旧存档里有照片或敌人情报，但没有写入 unlockedEnemies，
        // 这里会把它补到最后，避免旧数据直接丢失显示。
        for (int i = 0; i < enemyInformationDataBase.enemyInformationDataBase.Length; i++)
        {
            EnemyInformationDataSO enemyData = enemyInformationDataBase.enemyInformationDataBase[i];

            if (enemyData == null)
            {
                continue;
            }

            if (!IsEnemyUnlocked(enemyData))
            {
                continue;
            }

            if (addedSaveIDs.Add(enemyData.SaveID))
            {
                result.Add(enemyData);
            }
        }

        return result;
    }
    // ????????????????????????l
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

    // ???????????????????l??UI ??????????? IsEnemyIntelligenceUnlocked ???????????????????????
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

    private void EnsureEnemyRecognized(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            return;
        }

        bool newlyRecognized = UnlockEnemyInternal(enemyInformationData, false);

        if (newlyRecognized)
        {
            LogArchiveDebug($"EnsureEnemyRecognized: newly recognized {enemyInformationData.enemyName}");
        }
    }

    private bool HasAnyUnlockedEnemyIntelligence(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
        {
            return false;
        }

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            if (enemyIntelligenceData != null && unlockedEnemyIntelligenceIDSet.Contains(enemyIntelligenceData.SaveID))
            {
                return true;
            }
        }

        return false;
    }

    private void LogArchiveDebug(string message)
    {
        if (enableArchiveDebugLog)
        {
            Debug.Log($"[IntelligenceArchive] {message}", this);
        }
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

    private EnemyIntelligenceDataSO FindEnemyIntelligenceByName(string intelligenceName)
    {
        if (string.IsNullOrWhiteSpace(intelligenceName)
            || enemyIntelligenceDataBase == null
            || enemyIntelligenceDataBase.enemyIntelligenceDataBase == null)
        {
            return null;
        }

        string trimmedName = intelligenceName.Trim();
        EnemyIntelligenceDataSO[] database = enemyIntelligenceDataBase.enemyIntelligenceDataBase;

        for (int i = 0; i < database.Length; i++)
        {
            EnemyIntelligenceDataSO candidate = database[i];

            if (candidate == null || string.IsNullOrWhiteSpace(candidate.intelligenceName))
            {
                continue;
            }

            if (candidate.intelligenceName.Trim() == trimmedName)
            {
                return candidate;
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
            Debug.Log("????????????????????????l??");
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

        // ??????????????????????
        if (!IsEnemyUnlocked(exchangeData.requiredEnemyInformationData))
        {
            return false;
        }

        // ?????????????????????????????
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
            Debug.LogWarning($"??????l??????????????l {exchangeData.rewardEnemyIntelligenceData.name} ??????????????");
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

    public List<EnemyIntelligenceDataSO> GetEnemyIntelligencesForNotebookDisplay(EnemyInformationDataSO enemyInformationData)
    {
        List<EnemyIntelligenceDataSO> result = new List<EnemyIntelligenceDataSO>();

        if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
        {
            return result;
        }

        HashSet<string> addedSaveIDs = new HashSet<string>();

        if (!TryPrepareGameData())
        {
            AddEnemyIntelligencesInOriginalOrder(enemyInformationData, result, addedSaveIDs);
            return result;
        }

        // 第一阶段：已解锁的敌人情报排在上面。
        // 顺序使用 gameRunData.unlockedEnemyIntelligences 的顺序，
        // 也就是玩家实际解锁这些情报的顺序。
        for (int i = 0; i < gameRunData.unlockedEnemyIntelligences.Count; i++)
        {
            string saveID = gameRunData.unlockedEnemyIntelligences[i];

            EnemyIntelligenceDataSO enemyIntelligenceData = FindEnemyIntelligenceInEnemy(enemyInformationData, saveID);

            if (enemyIntelligenceData == null)
            {
                continue;
            }

            if (addedSaveIDs.Add(enemyIntelligenceData.SaveID))
            {
                result.Add(enemyIntelligenceData);
            }
        }

        // 第二阶段：未解锁的敌人情报排在下面，继续显示 ???。
        AddEnemyIntelligencesInOriginalOrder(enemyInformationData, result, addedSaveIDs);

        return result;
    }

    private void AddEnemyIntelligencesInOriginalOrder(
        EnemyInformationDataSO enemyInformationData,
        List<EnemyIntelligenceDataSO> result,
        HashSet<string> addedSaveIDs)
    {
        if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
        {
            return;
        }

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            if (enemyIntelligenceData == null)
            {
                continue;
            }

            if (addedSaveIDs.Add(enemyIntelligenceData.SaveID))
            {
                result.Add(enemyIntelligenceData);
            }
        }
    }

    private EnemyIntelligenceDataSO FindEnemyIntelligenceInEnemy(EnemyInformationDataSO enemyInformationData, string saveID)
    {
        if (enemyInformationData == null || enemyInformationData.enemyIntelligences == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(saveID))
        {
            return null;
        }

        for (int i = 0; i < enemyInformationData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO enemyIntelligenceData = enemyInformationData.enemyIntelligences[i];

            if (enemyIntelligenceData == null)
            {
                continue;
            }

            if (enemyIntelligenceData.SaveID == saveID)
            {
                return enemyIntelligenceData;
            }
        }

        return null;
    }
}