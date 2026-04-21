using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Database")]
public class AudioDatabaseSO : ScriptableObject
{
    public List<AudioClipData> SFX;
    public List<AudioClipData> BGM;

    public Dictionary<string, AudioClipData> clipCollection;

    private void OnEnable()
    {
        clipCollection = new Dictionary<string, AudioClipData>(); // 清空字典
        AddToCollection(SFX);
        AddToCollection(BGM);
    }

    public AudioClipData GetAudioClipDataByName(string groupName)
    {
        return clipCollection.TryGetValue(groupName, out AudioClipData audioClipData) ? audioClipData : null;
    }

    private void AddToCollection(List<AudioClipData> listToAdd)
    {
        foreach (var data in listToAdd)
        {
            if (data != null && clipCollection.ContainsKey(data.audioName) == false)
            {
                clipCollection.Add(data.audioName, data);
            }
        }
    }
}

[System.Serializable]
public class AudioClipData
{
    public string audioName; // 音频名
    public List<AudioClip> clips = new List<AudioClip>(); // 音频变体列表
    [Range(0f, 1f)]
    public float volume = 1f; // 音量


    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Count == 0)
        {
            return null;
        }

        return clips[Random.Range(0, clips.Count)];
    }
}

