using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    // 音频设置（0~1，配合 AudioMixer 使用）
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    public float uiVolume = 1f;

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


    public List<string> unlockedMerchantItems= new List<string>();     // 已解锁购买资格的物品
    public SerializableDictionary<string,int> itemSellAmount = new SerializableDictionary<string,int>(); // 各物品出售次数
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