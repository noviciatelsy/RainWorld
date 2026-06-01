using System;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public event Action<int, GameRunData> OnCurrentGameRunDataChanged;

    private FileDataHandler dataHandler;      // 专门负责“文件读写”的工具类

    [SerializeField] private GameData gameData;               // 整个游戏的总存档数据

    [SerializeField] private string fileName = "RainWorldYC.json";   // 存档文件名（会和 persistentDataPath 组合成完整路径）
    [SerializeField] private bool encryptData = true; // 是否需要加密

    [Header("当前选中的游戏内存档")]
    [SerializeField] private int currentSelectedSlotIndex = -1;
    [SerializeField] private GameRunData currentGameRunData;
    private GameRunData clonedCurrentGameRunData;

    public int CurrentSelectedSlotIndex
    {
        get
        {
            return currentSelectedSlotIndex;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 用 路径 + 文件名 生成一个 FileDataHandler
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);

        LoadGameDataFromDisk();
    }

    private void LoadGameDataFromDisk()
    {
        // 从文件里读出 GameData（可能是 null，也可能是一份完整存档）
        gameData = dataHandler.loadData();

        // 如果没有读到存档（比如第一次进入游戏）
        if (gameData == null)
        {
            // 创建一份新的默认 GameData（此时里面的字段都是默认值）
            gameData = new GameData();
        }

        EnsureGameDataValid();

        currentSelectedSlotIndex = -1;
        currentGameRunData = null;
        clonedCurrentGameRunData = null;
    }

    private void EnsureGameDataValid()
    {
        if (gameData == null)
        {
            gameData = new GameData();
        }

        gameData.EnsureDataValid();
    }

    public void SaveGame()
    {
        EnsureGameDataValid();

        if (currentGameRunData != null && IsSlotIndexValid(currentSelectedSlotIndex))
        {
            GameDataSlot currentSlot = gameData.GetGameDataSlot(currentSelectedSlotIndex);

            if (currentSlot != null && currentSlot.IsEmpty() == false)
            {
                currentGameRunData = currentSlot.runData;

                // 每次保存当前局内存档时，记录现实时间
                currentGameRunData.lastSaveTimeIso = DateTime.Now.ToString("o");
            }
        }

        // 把最终收集好的 GameData 交给 FileDataHandler，写到磁盘文件中
        dataHandler.SaveData(gameData);
    }

    public void SaveGlobalGameData()
    {
        EnsureGameDataValid();

        // 保存全局数据时，不修改当前局内存档的最后运行时间
        dataHandler.SaveData(gameData);
    }

    public GameData GetGameData()
    {
        EnsureGameDataValid();
        return gameData;
    }

    public GlobalGameData GetGlobalGameData()
    {
        EnsureGameDataValid();
        return gameData.globalGameData;
    }

    public GameRunData GetRunTimeGameData()
    {
        return currentGameRunData;
    }

    public GameDataSlot GetGameDataSlot(int mySlotIndex)
    {
        EnsureGameDataValid();

        if (IsSlotIndexValid(mySlotIndex) == false)
        {
            return null;
        }

        return gameData.GetGameDataSlot(mySlotIndex);
    }

    public bool IsGameDataSlotEmpty(int mySlotIndex)
    {
        GameDataSlot slot = GetGameDataSlot(mySlotIndex);

        if (slot == null)
        {
            return true;
        }

        return slot.IsEmpty();
    }

    public bool SelectGameRunDataSlot(int mySlotIndex)
    {
        GameDataSlot slot = GetGameDataSlot(mySlotIndex);

        if (slot == null || slot.IsEmpty())
        {
            Debug.LogWarning("试图加载一个空存档槽：" + mySlotIndex);
            return false;
        }

        SetCurrentGameRunData(mySlotIndex, slot.runData);
        return true;
    }

    public bool CreateNewGameRunDataInSlot(int mySlotIndex)
    {
        GameDataSlot slot = GetGameDataSlot(mySlotIndex);

        if (slot == null)
        {
            Debug.LogWarning("试图在非法存档槽中新建游戏：" + mySlotIndex);
            return false;
        }

        if (slot.IsEmpty() == false)
        {
            Debug.LogWarning("试图在非空存档槽中新建游戏：" + mySlotIndex);
            return false;
        }

        slot.CreateNewRunData();

        SetCurrentGameRunData(mySlotIndex, slot.runData);

        // 新游戏创建后，立刻保存一次，让这个槽位变成非空
        SaveGame();

        return true;
    }

    public bool DeleteGameRunDataSlot(int mySlotIndex)
    {
        GameDataSlot slot = GetGameDataSlot(mySlotIndex);

        if (slot == null)
        {
            Debug.LogWarning("试图删除非法存档槽：" + mySlotIndex);
            return false;
        }

        bool isDeletingCurrentSlot = currentSelectedSlotIndex == mySlotIndex;

        slot.Clear();

        if (isDeletingCurrentSlot)
        {
            SetCurrentGameRunData(-1, null);
        }

        // 删除槽位时，只保存总数据，不刷新其他当前存档的最后运行时间
        SaveGlobalGameData();

        return true;
    }

    private void SetCurrentGameRunData(int mySlotIndex, GameRunData myRunData)
    {
        currentSelectedSlotIndex = mySlotIndex;
        currentGameRunData = myRunData;

        ClearClonedCurrentGameRunData();
        OnCurrentGameRunDataChanged?.Invoke(currentSelectedSlotIndex, currentGameRunData);
    }
    private void ClearClonedCurrentGameRunData()
    {
        clonedCurrentGameRunData = null;
    }
    private bool IsSlotIndexValid(int mySlotIndex)
    {
        return mySlotIndex >= 0 && mySlotIndex < GameData.GameDataSlotCount;
    }

    public void CloneCurrentGameRunData()
    {
        if (currentGameRunData == null)
        {
            Debug.LogWarning("当前没有选中的局内存档，无法克隆。");
            return;
        }

        clonedCurrentGameRunData=CloneGameRunData(currentGameRunData);
    }

    private GameRunData CloneGameRunData(GameRunData mySourceData)
    {
        if (mySourceData == null)
        {
            return null;
        }

        // 通过 JsonUtility 做深拷贝
        // 这样 List、SerializableDictionary 里的数据都会复制一份新的，而不是继续共用引用
        string json = JsonUtility.ToJson(mySourceData);
        GameRunData clonedData = JsonUtility.FromJson<GameRunData>(json);

        return clonedData;
    }

    public bool OverwriteCurrentGameRunData(bool mySaveImmediately = true)
    {
        if (clonedCurrentGameRunData == null)
        {
            Debug.LogWarning("克隆存档为空，无法覆盖当前局内存档。");
            return false;
        }

        if (IsSlotIndexValid(currentSelectedSlotIndex) == false)
        {
            Debug.LogWarning("当前没有有效的存档槽位，无法覆盖当前局内存档。");
            return false;
        }

        EnsureGameDataValid();

        GameDataSlot currentSlot = gameData.GetGameDataSlot(currentSelectedSlotIndex);

        if (currentSlot == null)
        {
            Debug.LogWarning("当前存档槽为空，无法覆盖当前局内存档。");
            return false;
        }

        // 注意：这里不要直接 currentSlot.runData = myClonedRunData;
        // 而是再克隆一次，避免外部继续持有这个对象引用并修改它
        GameRunData newRunData = CloneGameRunData(clonedCurrentGameRunData);

        currentSlot.hasRunData = true;
        currentSlot.runData = newRunData;

        SetCurrentGameRunData(currentSelectedSlotIndex, currentSlot.runData);

        if (mySaveImmediately)
        {
            SaveGame();
        }

        return true;
    }

    [ContextMenu("Delete Saved Data")]
    public void DeleteSavedData() // 编辑器内使用
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);

        // 调用 FileDataHandler 的删除方法，删掉对应路径的存档文件
        dataHandler.Delete();

        gameData = new GameData();
        gameData.EnsureDataValid();

        SetCurrentGameRunData(-1, null);
    }
}