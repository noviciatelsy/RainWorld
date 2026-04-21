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

}

[Serializable]
public class SerializableDictionary<Tkey, TValue> : Dictionary<Tkey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<Tkey> keys = new List<Tkey>();
    [SerializeField] private List<TValue> values = new List<TValue>();
    // [SerializeField]：让 Unity 知道要把这俩字段当作可序列化数据来保存

    public void OnAfterDeserialize() // 在反序列化之后，把 keys 和 values 两个 List 还原回来，再把它们重新组装成字典
    {
        this.Clear();
        if (keys.Count == values.Count)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                this.Add(keys[i], values[i]);
            }
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
