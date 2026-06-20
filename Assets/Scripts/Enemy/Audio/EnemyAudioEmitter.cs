using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物空间音效基类：每只怪挂 OneShot + Loop 两个 AudioSource，由远及近衰减。
/// </summary>
[DisallowMultipleComponent]
public abstract class EnemyAudioEmitter : MonoBehaviour
{
    private static Dictionary<string, AudioClip> clipCache;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource loopSource;

    [Header("Spatial")]
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField] private float spatialBlend = 1f;

    private Transform audioPivotParent;

    protected AudioSource OneShotSource => oneShotSource;
    protected AudioSource LoopSource => loopSource;

    protected virtual void Awake()
    {
        EnsureAudioSources();
        ApplySpatialSettings();
    }

    protected virtual void OnDisable()
    {
        StopAll();
    }

    public virtual void NotifyStomped()
    {
    }

    protected void PlayOneShot(string resourcePath, float volumeScale = 1f)
    {
        AudioClip clip = LoadClip(resourcePath);
        PlayOneShotClip(clip, volumeScale);
    }

    protected void PlayOneShotClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || oneShotSource == null)
        {
            return;
        }

        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    protected bool StartLoop(string resourcePath, float volumeScale = 1f)
    {
        AudioClip clip = LoadClip(resourcePath);

        if (clip == null || loopSource == null)
        {
            return false;
        }

        if (loopSource.clip == clip && loopSource.isPlaying)
        {
            loopSource.volume = Mathf.Clamp01(volumeScale);
            return true;
        }

        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.volume = Mathf.Clamp01(volumeScale);
        loopSource.Play();
        return true;
    }

    protected void StopLoop()
    {
        if (loopSource == null)
        {
            return;
        }

        loopSource.Stop();
        loopSource.clip = null;
    }

    protected void StopAll()
    {
        StopLoop();

        if (oneShotSource != null)
        {
            oneShotSource.Stop();
        }
    }

    protected bool IsLoopPlaying()
    {
        return loopSource != null && loopSource.isPlaying;
    }

    protected static AudioClip LoadClip(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        clipCache ??= new Dictionary<string, AudioClip>();

        if (clipCache.TryGetValue(resourcePath, out AudioClip cachedClip) && cachedClip != null)
        {
            return cachedClip;
        }

        AudioClip loadedClip = Resources.Load<AudioClip>(resourcePath);

        if (loadedClip == null)
        {
            Debug.LogWarning($"[EnemyAudio] 未找到音频: {resourcePath}", null);
            return null;
        }

        clipCache[resourcePath] = loadedClip;
        return loadedClip;
    }

    protected void SetAudioPivotParent(Transform parent)
    {
        audioPivotParent = parent;
    }

    private Transform GetAudioPivotParent()
    {
        return audioPivotParent != null ? audioPivotParent : transform;
    }

    private void EnsureAudioSources()
    {
        Transform parent = GetAudioPivotParent();
        Transform pivot = parent.Find("AudioPivot");

        if (pivot == null)
        {
            GameObject pivotObject = new GameObject("AudioPivot");
            pivotObject.transform.SetParent(parent, false);
            pivot = pivotObject.transform;
        }

        if (oneShotSource == null)
        {
            oneShotSource = CreateSource(pivot, "OneShot");
        }

        if (loopSource == null)
        {
            loopSource = CreateSource(pivot, "Loop");
        }
    }

    private static AudioSource CreateSource(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        GameObject sourceObject;

        if (existing != null)
        {
            sourceObject = existing.gameObject;
        }
        else
        {
            sourceObject = new GameObject(childName);
            sourceObject.transform.SetParent(parent, false);
        }

        AudioSource source = sourceObject.GetComponent<AudioSource>();

        if (source == null)
        {
            source = sourceObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        return source;
    }

    private void ApplySpatialSettings()
    {
        ConfigureSource(oneShotSource);
        ConfigureSource(loopSource);
    }

    private void ConfigureSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
    }
}
