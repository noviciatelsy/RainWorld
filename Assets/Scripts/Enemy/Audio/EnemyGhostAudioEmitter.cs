using UnityEngine;

/// <summary>幽灵：待机循环、死亡。</summary>
public class EnemyGhostAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private float idleLoopVolume = 0.75f;
    [SerializeField] private float deathVolume = 1f;

    private void OnEnable()
    {
        StartLoop(EnemyAudioPaths.GhostIdleLoop, idleLoopVolume);
    }

    public void PlayDeath()
    {
        StopLoop();
        PlayOneShot(EnemyAudioPaths.GhostDeath, deathVolume);
    }
}
