using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    public const int GameDataSlotCount = 3;

    [Header("全局游戏外数据")]
    public GlobalGameData globalGameData = new GlobalGameData();

    [Header("三个独立的游戏内存档槽")]
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
    // 音频设置（0~1，配合 AudioMixer 使用）
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
    }
}

[Serializable]
public class GameRunData
{
    // 最后一次保存的现实时间
    // 用 string 存 ISO 时间，避免 DateTime 在 JsonUtility 里序列化不稳定
    public string lastSaveTimeIso = "";

    // 已解锁情报id
    public List<string> unlockedIntelligences = new List<string>();

    // 已解锁敌人情报id
    public List<string> unlockedEnemyIntelligences = new List<string>();

    // 已解锁EnemyInformation的id
    public List<string> unlockedEnemies = new List<string>();

    // 已解锁的敌人照片
    // key = EnemyInformationDataSO.SaveID
    // value = 是否已经解锁照片
    public SerializableDictionary<string, bool> unlockedEnemyPicture = new SerializableDictionary<string, bool>();

    // 已解锁购买资格的物品
    public List<string> unlockedMerchantItems = new List<string>();

    // 各物品出售次数
    public SerializableDictionary<string, int> itemSellAmount = new SerializableDictionary<string, int>();
}

[Serializable]
public class SerializableDictionary<Tkey, TValue> : Dictionary<Tkey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<Tkey> keys = new List<Tkey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public void OnAfterDeserialize() // 在反序列化之后，把 keys 和 values 两个 List 还原回来，再把它们重新组装成字典
    {
        this.Clear();

        if (keys.Count != values.Count)
        {
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            // 用 this[key] = value 可以避免重复 key 直接报错
            this[keys[i]] = values[i];
        }
    }

    public void OnBeforeSerialize() // 在序列化之前，把当前字典里的所有数据拆成两个 List：
    {
        keys.Clear();
        values.Clear();

        foreach (KeyValuePair<Tkey, TValue> pairs in this)
        {
            keys.Add(pairs.Key);     // 把键按顺序放入 keys
            values.Add(pairs.Value); // 把值按顺序放入 values
        }
    }
}