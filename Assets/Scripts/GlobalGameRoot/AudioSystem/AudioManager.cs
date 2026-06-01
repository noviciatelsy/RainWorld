using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get;
        private set;
    }

    [Header("音频数据库引用")]
    [SerializeField] private AudioDatabaseSO audioDatabase;

    [Header("BGM 与 UI 音源")]
    [SerializeField] private AudioSource bgmSource; // 专门用于 BGM 的 AudioSource（2D）
    [SerializeField] private AudioSource uiSource;  // 专门用于 UI 音效的 AudioSource（2D）

    [Header("AudioMixer 引用")]
    [SerializeField] private AudioMixer audioMixer;
    [Header("Mixer 分组引用（在 Inspector 里拖 BGM / SFX / UI 三个 Group）")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;

    [Header("AudioMixer 参数名（需与 Mixer 内 Exposed Parameter 一致）")]
    [SerializeField] private string bgmVolumeParameter = "BGM_Volume";
    [SerializeField] private string sfxVolumeParameter = "SFX_Volume";
    [SerializeField] private string uiVolumeParameter = "UI_Volume";

    [Header("BGM 渐变设置")]
    [SerializeField] private float bgmFadeDuration = 1f; // 默认 BGM 淡入淡出时长（秒）
    [Tooltip("是否在 Time.timeScale 变化时仍然保持 BGM 渐变速度不受影响。")]
    [SerializeField] private bool bgmFadeUseUnscaledTime = true;

    [Header("一次性 SFX 音效池设置（PlayOneShot 用）")]
    [SerializeField] private int sfxPoolSize = 16; // 可同时使用的音效源数量上限
    private AudioSource[] sfxPool; // SFX AudioSource 池
    private int sfxPoolIndex;      // 当前轮询到的池子下标

    [Header("循环 SFX 音效池设置（脚步 / 环境 Loop 等）")]
    [SerializeField] private int loopSfxPoolSize = 16; // 可同时使用的循环 SFX 数量上限
    private AudioSource[] loopSfxPool;                // 循环 SFX AudioSource 池
    private int loopSfxPoolIndex;                     // 当前轮询到的循环池子下标

    // 当前 BGM 渐变协程的句柄（避免多次叠加）
    private Coroutine bgmFadeCoroutine;

    #region Mono 生命周期

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitSfxPool();
        InitLoopSfxPool();

        if (bgmSource != null && bgmMixerGroup != null && bgmSource.outputAudioMixerGroup == null)
        {
            bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        }

        if (uiSource != null && uiMixerGroup != null && uiSource.outputAudioMixerGroup == null)
        {
            uiSource.outputAudioMixerGroup = uiMixerGroup;
        }


    }

    private void Start()
    {
        LoadVolume();
    }


    #endregion

    #region 初始化 SFX 池


    private void InitSfxPool()
    {

        sfxPool = new AudioSource[sfxPoolSize];
        sfxPoolIndex = 0;

        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxGo = new GameObject("SFX_OneShot_AudioSource_" + i);
            sfxGo.transform.SetParent(transform);

            AudioSource src = sfxGo.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false; // 一次性音效默认不循环
            src.spatialBlend = 0;

            if (sfxMixerGroup != null)
            {
                src.outputAudioMixerGroup = sfxMixerGroup;
            }


            sfxPool[i] = src;
        }
    }

    private void InitLoopSfxPool()
    {

        loopSfxPool = new AudioSource[loopSfxPoolSize];
        loopSfxPoolIndex = 0;

        for (int i = 0; i < loopSfxPoolSize; i++)
        {
            GameObject sfxGo = new GameObject("SFX_Loop_AudioSource_" + i);
            sfxGo.transform.SetParent(transform);

            AudioSource src = sfxGo.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true; // 循环池默认用来循环播放
            src.spatialBlend = 0;


            if (sfxMixerGroup != null)
            {
                src.outputAudioMixerGroup = sfxMixerGroup;
            }

            loopSfxPool[i] = src;
        }
    }

    /// <summary>
    /// 从“一次性 SFX 池”中取出下一个 AudioSource。
    /// 简单轮询实现：如果正在播放中的也会被覆盖
    /// </summary>
    private AudioSource GetNextSfxSource()
    {
        AudioSource src = sfxPool[sfxPoolIndex];

        sfxPoolIndex++;
        if (sfxPoolIndex >= sfxPool.Length)
        {
            sfxPoolIndex = 0;
        }

        return src;
    }

    /// <summary>
    /// 从“循环 SFX 池”中取出一个空闲的 AudioSource。
    /// 不会覆盖正在播放中的循环音效；如果池子都在用，返回 null。
    /// </summary>
    private AudioSource GetFreeLoopSfxSource()
    {
        if (loopSfxPool == null || loopSfxPool.Length == 0)
        {
            return null;
        }

        int length = loopSfxPool.Length;

        // 从 loopSfxPoolIndex 开始轮询一圈，找第一个 !isPlaying 的
        for (int i = 0; i < length; i++)
        {
            int index = loopSfxPoolIndex + i;

            if (index >= length)
            {
                index -= length;
            }

            AudioSource src = loopSfxPool[index];

            if (src != null && !src.isPlaying)
            {
                // 下一个起点从这个后面开始，避免每次都从 0 扫
                loopSfxPoolIndex = index + 1;
                if (loopSfxPoolIndex >= length)
                {
                    loopSfxPoolIndex = 0;
                }

                return src;
            }
        }

        // 找了一圈都没有空闲的
        return null;
    }

    #endregion



    #region Clip 数据查询

    /// <summary>
    /// 根据 audioName 从数据库里拿 AudioClipData。
    /// 统一做空指针 / 找不到的判断。
    /// </summary>
    private AudioClipData GetClipData(string audioName)
    {
        if (audioDatabase == null)
        {
            Debug.LogWarning("[AudioManager] AudioDatabaseSO 未赋值，无法通过名字播放音频。");
            return null;
        }

        if (string.IsNullOrEmpty(audioName))
        {
            Debug.LogWarning("[AudioManager] 传入的 audioName 为空。");
            return null;
        }

        AudioClipData data = audioDatabase.GetAudioClipDataByName(audioName);
        if (data == null)
        {
            Debug.LogWarning("[AudioManager] 在 AudioDatabase 中找不到名为 \"" + audioName + "\" 的 AudioClipData。");
            return null;
        }

        return data;
    }

    #endregion

    #region BGM 播放（带淡入淡出，Master 由 Mixer 控制）

    /// <summary>
    /// 播放 BGM（通过音频名，从数据库取随机变体）。
    /// 可选淡入淡出：若 fadeDuration 小于等于 0，则立刻切换；否则淡出当前，再淡入新 BGM。
    /// </summary>
    public void PlayBGM(string audioName, bool loop = true, float fadeDuration = -1f)
    {
        AudioClipData data = GetClipData(audioName);
        if (data == null)
        {
            return;
        }

        AudioClip clip = data.GetRandomClip();
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] AudioClipData \"" + audioName + "\" 中没有任何 AudioClip。");
            return;
        }

        // 如果正在播放的就是同一首，并且还在播，就不重复切
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        // 如果没传入自定义的淡入淡出时间，则使用默认配置
        if (fadeDuration < 0f)
        {
            fadeDuration = bgmFadeDuration;
        }

        // 如果之前有尚未结束的 BGM 渐变协程，先停掉
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        // 启动新的淡出 + 淡入协程
        bgmFadeCoroutine = StartCoroutine(FadeToNewBgmCoroutine(clip, data.volume, loop, fadeDuration));
    }


    /// <summary>
    /// 停止当前 BGM。
    /// 如果 fadeDuration 大于 0，则淡出到 0 再停止；否则立刻 Stop。
    /// </summary>
    public void StopBGM(float fadeDuration = -1f)
    {
        if (bgmSource == null)
        {
            return;
        }

        if (!bgmSource.isPlaying)
        {
            return;
        }

        if (fadeDuration < 0f)
        {
            fadeDuration = bgmFadeDuration;
        }

        // 不希望淡出：直接停
        if (fadeDuration <= 0f)
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
                bgmFadeCoroutine = null;
            }

            bgmSource.Stop();
            return;
        }

        // 正在跑别的渐变的话，打断它
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        bgmFadeCoroutine = StartCoroutine(FadeOutAndStopBgmCoroutine(fadeDuration));
    }

    /// <summary>
    /// 协程：将当前 BGM 淡出，再淡入新的 BGM。
    /// 注意：BGM 的“全局音量”由 AudioMixer 控制，这里只在 0 ~ currentBgmLocalVolume 之间做插值。
    /// </summary>
    private System.Collections.IEnumerator FadeToNewBgmCoroutine(AudioClip newClip, float newLocalVolume, bool loop, float fadeDuration)
    {
        // 防御：空 clip 直接结束
        if (newClip == null)
        {
            yield break;
        }

        // 没有设置淡入淡出时间就直接切歌
        if (fadeDuration <= 0f)
        {
            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.loop = loop;
            bgmSource.spatialBlend = 0f;
            bgmSource.pitch = 1f;
            bgmSource.volume = newLocalVolume; // 只用本地音量
            bgmSource.Play();

            bgmFadeCoroutine = null;
            yield break;
        }

        float halfDuration = fadeDuration * 0.5f;

        // 1. 先淡出（如果当前正在播放）
        float startVolume = bgmSource.isPlaying ? bgmSource.volume : 0f;
        float time = 0f;

        while (time < halfDuration)
        {
            time += GetBgmDeltaTime();
            float t = Mathf.Clamp01(halfDuration <= 0f ? 1f : time / halfDuration);
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();

        // 2. 切换到新 BGM

        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        bgmSource.spatialBlend = 0f;
        bgmSource.pitch = 1f;

        float targetVolume = newLocalVolume;

        bgmSource.volume = 0f;
        bgmSource.Play();

        // 3. 再淡入到目标音量
        time = 0f;

        while (time < halfDuration)
        {
            time += GetBgmDeltaTime();
            float t = Mathf.Clamp01(halfDuration <= 0f ? 1f : time / halfDuration);
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        bgmSource.volume = targetVolume;

        // 协程结束，清空句柄
        bgmFadeCoroutine = null;
    }

    /// <summary>
    /// 协程：将当前 BGM 从当前音量淡出到 0，然后停止。
    /// </summary>
    private System.Collections.IEnumerator FadeOutAndStopBgmCoroutine(float fadeDuration)
    {
        if (!bgmSource.isPlaying || fadeDuration <= 0f)
        {
            bgmSource.Stop();
            bgmFadeCoroutine = null;
            yield break;
        }

        float startVolume = bgmSource.volume;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += GetBgmDeltaTime();
            float t = Mathf.Clamp01(fadeDuration <= 0f ? 1f : time / fadeDuration);
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();

        bgmFadeCoroutine = null;
    }

    private float GetBgmDeltaTime()
    {
        return bgmFadeUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    #endregion

    #region UI / SFX 一次性播放

    /// <summary>
    /// 播放一个 UI 音效（通过数据库中的 audioName）。
    /// 增加了 pitch 参数，用于支持 AudioEventSO 的随机音调。
    /// UI Master 音量交给 AudioMixer 控制，这里只设置 Clip 自身音量。
    /// </summary>
    public void PlayUI(string audioName, bool randomPitch = true)
    {
        AudioClipData data = GetClipData(audioName);
        if (data == null)
        {
            return;
        }

        AudioClip clip = data.GetRandomClip();
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] UI AudioClipData \"" + audioName + "\" 中没有任何 AudioClip。");
            return;
        }

        // UI Source 一般只负责 2D UI 声音
        uiSource.volume = data.volume;
        uiSource.spatialBlend = 0f;
        float pitch = 1;
        if (randomPitch)
        {
            pitch = Random.Range(0.95f, 1.05f);
        }
        uiSource.pitch = pitch;
        uiSource.PlayOneShot(clip);
    }


    /// <summary>
    /// 播放  SFX（走数据库），例如：按钮音效、系统提示音等。
    /// 增加 pitch 参数，用于支持随机音调。
    /// SFX Master 音量交给 AudioMixer 控制，这里只设置 Clip 自身音量
    /// </summary>
    public void PlaySFX(string audioName, bool randomPitch = true)
    {
        AudioClipData data = GetClipData(audioName);
        if (data == null)
        {
            return;
        }

        AudioClip clip = data.GetRandomClip();
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] SFX AudioClipData \"" + audioName + "\" 中没有任何 AudioClip。");
            return;
        }

        AudioSource src = GetNextSfxSource();
        src.transform.position = Vector3.zero;
        src.volume = data.volume;
        float pitch = 1;
        if (randomPitch)
        {
            pitch = Random.Range(0.95f, 1.05f);
        }
        src.pitch = pitch;
        src.PlayOneShot(clip);
    }


    #endregion

    #region 循环 SFX 播放接口（脚步声 / 环境 Loop 等）

    public AudioSource PlayLoopSFX(string audioName, bool randomPitch = false)
    {
        AudioClipData data = GetClipData(audioName);
        if (data == null)
        {
            return null;
        }

        AudioClip clip = data.GetRandomClip();
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Loop SFX AudioClipData \"" + audioName + "\" 中没有任何 AudioClip。");
            return null;
        }

        AudioSource src = GetFreeLoopSfxSource();
        if (src == null)
        {
            Debug.LogWarning("[AudioManager] 循环 SFX 池已满，无法为 \"" + audioName + "\" 分配循环音源。");
            return null;
        }

        src.transform.position = Vector3.zero;
        src.clip = clip;
        src.loop = true;
        src.volume = data.volume;
        float pitch = 1;
        if (randomPitch)
        {
            pitch = Random.Range(0.95f, 1.05f);
        }
        src.pitch = pitch;
        src.Play();

        return src;
    }

    /// <summary>
    /// 停止指定的循环 SFX，并释放该 AudioSource。
    /// 调用方应保证传入的是从 PlayLoopSFX2D/3D 返回的 AudioSource。
    /// </summary>
    public void StopLoopSFX(AudioSource loopSource)
    {
        if (loopSource == null)
        {
            return;
        }

        loopSource.Stop();
        loopSource.clip = null;
        // 这里不强制改 spatialBlend / volume / pitch，
        // 下次重新分配播放时会重新设置。
    }

    /// <summary>
    /// 停止所有循环 SFX。
    /// </summary>
    public void StopAllLoopSFX()
    {
        if (loopSfxPool == null)
        {
            return;
        }

        for (int i = 0; i < loopSfxPool.Length; i++)
        {
            AudioSource src = loopSfxPool[i];
            if (src == null)
            {
                continue;
            }

            src.Stop();
            src.clip = null;
        }
    }


    #endregion

    #region AudioMixer 相关工具

    /// <summary>
    /// 线性 0~1 音量 → dB。0 对应 -80dB（近似静音）。
    /// </summary>
    private float LinearToDecibel(float value)
    {
        if (value <= 0f)
        {
            return -80f;
        }

        return Mathf.Log10(value) * 20f;
    }

    /// <summary>
    /// dB → 线性 0~1 音量。-80dB 视为 0。
    /// 暂时没用到，留给以后从 Mixer 读回时使用。
    /// </summary>
    private float DecibelToLinear(float dB)
    {
        if (dB <= -80f)
        {
            return 0f;
        }

        return Mathf.Pow(10f, dB / 20f);
    }

    /// <summary>
    /// 设置 BGM 总音量（0~1），内部转换为 dB 并设置到 AudioMixer。
    /// 同时更新 bgmMasterVolume 字段，供存档使用。
    /// </summary>
    public void SetBgmMasterVolume(float value)
    {
        float bgmMasterVolume = Mathf.Clamp01(value);

        if (audioMixer != null && !string.IsNullOrEmpty(bgmVolumeParameter))
        {
            audioMixer.SetFloat(bgmVolumeParameter, LinearToDecibel(bgmMasterVolume));
        }
    }

    /// <summary>
    /// 设置 SFX 总音量（0~1）。
    /// </summary>
    public void SetSfxMasterVolume(float value)
    {
        float sfxMasterVolume = Mathf.Clamp01(value);

        if (audioMixer != null && !string.IsNullOrEmpty(sfxVolumeParameter))
        {
            audioMixer.SetFloat(sfxVolumeParameter, LinearToDecibel(sfxMasterVolume));
        }
    }

    /// <summary>
    /// 设置 UI 总音量（0~1）。
    /// </summary>
    public void SetUiMasterVolume(float value)
    {
        float uiMasterVolume = Mathf.Clamp01(value);

        if (audioMixer != null && !string.IsNullOrEmpty(uiVolumeParameter))
        {
            audioMixer.SetFloat(uiVolumeParameter, LinearToDecibel(uiMasterVolume));
        }
    }
    public void LoadVolume()
    {
        GlobalGameData data = SaveManager.Instance.GetGlobalGameData();
        if (data == null)
        {
            return;
        }

        SetBgmMasterVolume(data.bgmVolume);
        SetSfxMasterVolume(data.sfxVolume);
        SetUiMasterVolume(data.uiVolume);
    }

    #endregion
}
