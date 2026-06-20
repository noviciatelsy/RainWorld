using UnityEngine;

/// <summary>鼹鼠爷爷：睡觉循环、收到金块惊醒。</summary>
public class EnemyMoleParentAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private MoleParentAni moleParentAni;
    [SerializeField] private float sleepLoopVolume = 0.85f;
    [SerializeField] private float wakeVolume = 1f;

    protected override void Awake()
    {
        if (moleParentAni == null)
        {
            moleParentAni = GetComponent<MoleParentAni>();
        }

        base.Awake();
    }

    public void StartSleepLoop()
    {
        StartLoop(EnemyAudioPaths.MoleParentSleepLoop, sleepLoopVolume);
    }

    public void StopSleepLoop()
    {
        StopLoop();
    }

    public void PlayWake()
    {
        StopSleepLoop();
        PlayOneShot(EnemyAudioPaths.MoleParentWake, wakeVolume);
    }
}
