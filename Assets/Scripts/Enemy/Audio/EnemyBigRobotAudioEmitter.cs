using UnityEngine;

/// <summary>巨型机器人：待机循环、出刀、关机。</summary>
public class EnemyBigRobotAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private BigRobot2D bigRobot;
    [SerializeField] private float idleLoopVolume = 0.85f;
    [SerializeField] private float slashVolume = 1f;
    [SerializeField] private float shutdownVolume = 1f;

    protected override void Awake()
    {
        if (bigRobot == null)
        {
            bigRobot = GetComponent<BigRobot2D>();
        }

        base.Awake();
    }

    private void OnEnable()
    {
        RefreshIdleLoop();
    }

    private void LateUpdate()
    {
        RefreshIdleLoop();
    }

    public void PlaySlash()
    {
        PlayOneShot(EnemyAudioPaths.BigRobotSlash, slashVolume);
    }

    public void PlayShutdown()
    {
        StopLoop();
        PlayOneShot(EnemyAudioPaths.BigRobotShutdown, shutdownVolume);
    }

    private void RefreshIdleLoop()
    {
        if (bigRobot == null || bigRobot.IsShutdown)
        {
            StopLoop();
            return;
        }

        StartLoop(EnemyAudioPaths.BigRobotIdleLoop, idleLoopVolume);
    }
}
