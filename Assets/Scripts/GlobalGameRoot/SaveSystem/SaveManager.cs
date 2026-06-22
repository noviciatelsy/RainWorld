using System;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public event Action<int, GameRunData> OnCurrentGameRunDataChanged;

    private FileDataHandler dataHandler;      // ר�Ÿ����ļ���д���Ĺ�����

    [SerializeField] private GameData gameData;               // ������Ϸ���ܴ浵����

    [SerializeField] private string fileName = "RainWorldYC.json";   // �浵�ļ�������� persistentDataPath ��ϳ�����·����
    [SerializeField] private bool encryptData = true; // �Ƿ���Ҫ����

    [Header("��ǰѡ�е���Ϸ�ڴ浵")]
    [SerializeField] private int currentSelectedSlotIndex = -1;
    [SerializeField] private GameRunData currentGameRunData;
    private GameRunData clonedCurrentGameRunData;

    public event Action OnGameRunDataOverwrite;

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

        // �� ·�� + �ļ��� ����һ�� FileDataHandler
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);

        LoadGameDataFromDisk();
    }

    private void LoadGameDataFromDisk()
    {
        // ���ļ������ GameData�������� null��Ҳ������һ�������浵��
        gameData = dataHandler.loadData();

        // ���û�ж����浵�������һ�ν�����Ϸ��
        if (gameData == null)
        {
            // ����һ���µ�Ĭ�� GameData����ʱ������ֶζ���Ĭ��ֵ��
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

                // ÿ�α��浱ǰ���ڴ浵ʱ����¼��ʵʱ��
                currentGameRunData.lastSaveTimeIso = DateTime.Now.ToString("o");
            }
        }

        // �������ռ��õ� GameData ���� FileDataHandler��д�������ļ���
        dataHandler.SaveData(gameData);
    }

    public void SaveGlobalGameData()
    {
        EnsureGameDataValid();

        // ����ȫ������ʱ�����޸ĵ�ǰ���ڴ浵���������ʱ��
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
            Debug.LogWarning("��ͼ����һ���մ浵�ۣ�" + mySlotIndex);
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
            Debug.LogWarning("��ͼ�ڷǷ��浵�����½���Ϸ��" + mySlotIndex);
            return false;
        }

        if (slot.IsEmpty() == false)
        {
            Debug.LogWarning("��ͼ�ڷǿմ浵�����½���Ϸ��" + mySlotIndex);
            return false;
        }

        slot.CreateNewRunData();

        SetCurrentGameRunData(mySlotIndex, slot.runData);

        // ����Ϸ���������̱���һ�Σ��������λ��ɷǿ�
        SaveGame();

        return true;
    }

    public bool DeleteGameRunDataSlot(int mySlotIndex)
    {
        GameDataSlot slot = GetGameDataSlot(mySlotIndex);

        if (slot == null)
        {
            Debug.LogWarning("��ͼɾ���Ƿ��浵�ۣ�" + mySlotIndex);
            return false;
        }

        bool isDeletingCurrentSlot = currentSelectedSlotIndex == mySlotIndex;

        slot.Clear();

        if (isDeletingCurrentSlot)
        {
            SetCurrentGameRunData(-1, null);
        }

        // ɾ����λʱ��ֻ���������ݣ���ˢ��������ǰ�浵���������ʱ��
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
            Debug.LogWarning("��ǰû��ѡ�еľ��ڴ浵���޷���¡��");
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

        // ͨ�� JsonUtility �����
        // ���� List��SerializableDictionary ������ݶ��Ḵ��һ���µģ������Ǽ�����������
        string json = JsonUtility.ToJson(mySourceData);
        GameRunData clonedData = JsonUtility.FromJson<GameRunData>(json);

        return clonedData;
    }

    public bool OverwriteCurrentGameRunData(bool mySaveImmediately = true)
    {
        if (clonedCurrentGameRunData == null)
        {
            Debug.LogWarning("��¡�浵Ϊ�գ��޷����ǵ�ǰ���ڴ浵��");
            return false;
        }

        if (IsSlotIndexValid(currentSelectedSlotIndex) == false)
        {
            Debug.LogWarning("��ǰû����Ч�Ĵ浵��λ���޷����ǵ�ǰ���ڴ浵��");
            return false;
        }

        EnsureGameDataValid();

        GameDataSlot currentSlot = gameData.GetGameDataSlot(currentSelectedSlotIndex);

        if (currentSlot == null)
        {
            Debug.LogWarning("��ǰ�浵��Ϊ�գ��޷����ǵ�ǰ���ڴ浵��");
            return false;
        }

        // ע�⣺���ﲻҪֱ�� currentSlot.runData = myClonedRunData;
        // �����ٿ�¡һ�Σ������ⲿ������������������ò��޸���
        GameRunData newRunData = CloneGameRunData(clonedCurrentGameRunData);

        currentSlot.hasRunData = true;
        currentSlot.runData = newRunData;

        SetCurrentGameRunData(currentSelectedSlotIndex, currentSlot.runData);

        if (mySaveImmediately)
        {
            SaveGame();
        }

        OnGameRunDataOverwrite?.Invoke();
        return true;
    }

    [ContextMenu("Delete Saved Data")]
    public void DeleteSavedData() // �༭����ʹ��
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);

        // ���� FileDataHandler ��ɾ��������ɾ���Ӧ·���Ĵ浵�ļ�
        dataHandler.Delete();

        gameData = new GameData();
        gameData.EnsureDataValid();

        SetCurrentGameRunData(-1, null);
    }
}