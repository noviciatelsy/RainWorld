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

    private GameData gameData;

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
        InitializeFromSave();
    }


    public void InitializeFromSave()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("IntelligenceArchiveManager 初始化失败：场景中找不到 SaveManager。");
            return;
        }

        gameData = SaveManager.Instance.GetRunTimeGameData();

        if (gameData == null)
        {
            Debug.LogWarning("IntelligenceArchiveManager 初始化失败：SaveManager 中的 GameData 为空。");
            return;
        }

        EnsureGameDataLists();
        RebuildRuntimeCache();
    }

    private bool TryPrepareGameData()
    {
        if (gameData == null)
        {
            InitializeFromSave();
        }

        if (gameData == null)
        {
            Debug.LogWarning("无法操作图鉴数据：GameData 为空。");
            return false;
        }

        EnsureGameDataLists();
        return true;
    }

    private void EnsureGameDataLists()
    {
        if (gameData.unlockedIntelligences == null)
        {
            gameData.unlockedIntelligences = new List<string>();
        }

        if (gameData.unlockedEnemyIntelligences == null)
        {
            gameData.unlockedEnemyIntelligences = new List<string>();
        }

        if (gameData.unlockedEnemies == null)
        {
            gameData.unlockedEnemies = new List<string>();
        }

        if (gameData.unlockedEnemyPicture == null)
        {
            gameData.unlockedEnemyPicture = new SerializableDictionary<string, bool>();
        }
    }

    private void RebuildRuntimeCache()
    {
        unlockedIntelligenceIDSet.Clear();
        unlockedEnemyIDSet.Clear();
        unlockedEnemyIntelligenceIDSet.Clear();
        unlockedEnemyPictureIDSet.Clear();

        AddIDsToSet(gameData.unlockedIntelligences, unlockedIntelligenceIDSet);
        AddIDsToSet(gameData.unlockedEnemies, unlockedEnemyIDSet);
        AddIDsToSet(gameData.unlockedEnemyIntelligences, unlockedEnemyIntelligenceIDSet);
        AddUnlockedPictureIDsToSet(gameData.unlockedEnemyPicture, unlockedEnemyPictureIDSet);
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
            gameData.unlockedIntelligences,
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
            gameData.unlockedEnemies,
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
            gameData.unlockedEnemyPicture,
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
            gameData.unlockedEnemyIntelligences,
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

        for (int i = 0; i < gameData.unlockedIntelligences.Count; i++)
        {
            string saveID = gameData.unlockedIntelligences[i];
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

        for (int i = 0; i < gameData.unlockedEnemies.Count; i++)
        {
            string saveID = gameData.unlockedEnemies[i];
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
}