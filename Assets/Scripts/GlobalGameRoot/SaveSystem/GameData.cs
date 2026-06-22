using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    public const int GameDataSlotCount = 3;

    [Header("????????????")]
    public GlobalGameData globalGameData = new GlobalGameData();

    [Header("???????????????��??")]
    public List<GameDataSlot> gameDataSlots = new List<GameDataSlot>();

    public void EnsureDataValid()
    {
        if (globalGameData == null)
        {
            globalGameData = new GlobalGameData();
        }

        if (gameDataSlots == null)
        {
            gameDataSlots = new List<GameDataSlot>();
        }

        while (gameDataSlots.Count < GameDataSlotCount)
        {
            gameDataSlots.Add(new GameDataSlot());
        }

        while (gameDataSlots.Count > GameDataSlotCount)
        {
            gameDataSlots.RemoveAt(gameDataSlots.Count - 1);
        }

        for (int i = 0; i < gameDataSlots.Count; i++)
        {
            if (gameDataSlots[i] == null)
            {
                gameDataSlots[i] = new GameDataSlot();
            }

            gameDataSlots[i].EnsureDataValid();
        }
    }

    public GameDataSlot GetGameDataSlot(int mySlotIndex)
    {
        EnsureDataValid();

        if (mySlotIndex < 0 || mySlotIndex >= GameDataSlotCount)
        {
            return null;
        }

        return gameDataSlots[mySlotIndex];
    }
}

[Serializable]
public class GlobalGameData
{
    // ????????0~1????? AudioMixer ????
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    public float uiVolume = 1f;

 
}

[Serializable]
public class GameDataSlot
{
    public bool hasRunData = false;

    public GameRunData runData;

    public bool IsEmpty()
    {
        return hasRunData == false || runData == null;
    }

    public void CreateNewRunData()
    {
        hasRunData = true;
        runData = new GameRunData();
    }

    public void Clear()
    {
        hasRunData = false;
        runData = null;
    }

    public void EnsureDataValid()
    {
        if (hasRunData == false)
        {
            runData = null;
            return;
        }

        if (runData == null)
        {
            hasRunData = false;
        }

        runData.EnsureDataValid();
    }
}

[Serializable]
public class GameRunData
{
    // ?????��??????????
    // ?? string ?? ISO ??????? DateTime ?? JsonUtility ?????��??????
    public string lastSaveTimeIso = "";

    // ??????�lid
    public List<string> unlockedIntelligences = new List<string>();

    // ??????????�lid
    public List<string> unlockedEnemyIntelligences = new List<string>();

    // ?????EnemyInformation??id
    public List<string> unlockedEnemies = new List<string>();

    // ?????????????
    // key = EnemyInformationDataSO.SaveID
    // value = ?????????????
    public SerializableDictionary<string, bool> unlockedEnemyPicture = new SerializableDictionary<string, bool>();

    // ????????????????
    public List<string> unlockedMerchantItems = new List<string>();

    // ????????????
    public SerializableDictionary<string, int> itemSellAmount = new SerializableDictionary<string, int>();

    // ?????????????
    // key = RoomController.roomSaveID
    // value = ???????????
    public SerializableDictionary<string, bool> visitedRooms = new SerializableDictionary<string, bool>();

    [Header("??????????")]
    public PlayerInventorySaveData playerInventorySaveData = new PlayerInventorySaveData();

    [Header("????/??????????")]
    public SerializableDictionary<string, InventorySaveData> inventorySaveDataMap =
        new SerializableDictionary<string, InventorySaveData>();

    // ???????????????Ground ??????????????��??????????
    public List<string> unlockedElevatorFloors = new List<string>();

    // 已永久解锁的密码门 SaveID 列表
    public List<string> unlockedPasswordDoors = new List<string>();

    // 已触发过的对话Trigger
    public List<string> triggeredDialogues = new List<string>();

    // 已进入永久开心状态的鼹鼠爷爷 SaveID 列表
    public List<string> moleParentHappyTriggeredIds = new List<string>();

    // 已触发爆炸的 Boom SaveID 列表
    public List<string> triggeredBoomIds = new List<string>();

    public Vector3 playerDiePosition = new Vector3(0,0,0);

    public Vector3 retrieveBackpackSpawnPosition = new Vector3(0,0,0);

    public bool hasPassedMerchantTutorialDialogue=false;
    public bool hasPassedIntelligencerTutorialDialogue = false;
    public bool hasFirstDeath=false;
    public bool hasPassedMerchantFirstDeathDialogue=false;

    public void EnsureDataValid()
    {
        if (unlockedIntelligences == null)
        {
            unlockedIntelligences = new List<string>();
        }

        if (unlockedEnemyIntelligences == null)
        {
            unlockedEnemyIntelligences = new List<string>();
        }

        if (unlockedEnemies == null)
        {
            unlockedEnemies = new List<string>();
        }

        if (unlockedEnemyPicture == null)
        {
            unlockedEnemyPicture = new SerializableDictionary<string, bool>();
        }

        if (unlockedMerchantItems == null)
        {
            unlockedMerchantItems = new List<string>();
        }

        if (itemSellAmount == null)
        {
            itemSellAmount = new SerializableDictionary<string, int>();
        }

        if (visitedRooms == null)
        {
            visitedRooms = new SerializableDictionary<string, bool>();
        }

        if (playerInventorySaveData == null)
        {
            playerInventorySaveData = new PlayerInventorySaveData();
        }

        playerInventorySaveData.EnsureDataValid();

        if (inventorySaveDataMap == null)
        {
            inventorySaveDataMap = new SerializableDictionary<string, InventorySaveData>();
        }

        if (unlockedElevatorFloors == null)
        {
            unlockedElevatorFloors = new List<string>();
        }

        if (unlockedPasswordDoors == null)
        {
            unlockedPasswordDoors = new List<string>();
        }

        if (triggeredDialogues == null)
        {
            triggeredDialogues = new List<string>();
        }

        if (moleParentHappyTriggeredIds == null)
        {
            moleParentHappyTriggeredIds = new List<string>();
        }

        if (triggeredBoomIds == null)
        {
            triggeredBoomIds = new List<string>();
        }

        foreach (KeyValuePair<string, InventorySaveData> pair in inventorySaveDataMap)
        {
            if (pair.Value != null)
            {
                pair.Value.EnsureDataValid();
            }
        }

        if(playerDiePosition == null)
        {
            playerDiePosition = new Vector3(0, 0, 0);
        }

        if(retrieveBackpackSpawnPosition == null)
        {
            retrieveBackpackSpawnPosition = new Vector3(0, 0, 0);
        }
    }
}

[Serializable]
public class SerializableDictionary<Tkey, TValue> : Dictionary<Tkey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<Tkey> keys = new List<Tkey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public void OnAfterDeserialize() // ??????��????? keys ?? values ???? List ????????????????????????????
    {
        this.Clear();

        if (keys.Count != values.Count)
        {
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            // ?? this[key] = value ?????????? key ??????
            this[keys[i]] = values[i];
        }
    }

    public void OnBeforeSerialize() // ?????��????????????????????????????? List??
    {
        keys.Clear();
        values.Clear();

        foreach (KeyValuePair<Tkey, TValue> pairs in this)
        {
            keys.Add(pairs.Key);     // ??????????? keys
            values.Add(pairs.Value); // ??????????? values
        }
    }
}