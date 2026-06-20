using UnityEngine;

/// <summary>小机器人：冲刺循环、攻击命中。</summary>
public class EnemyRobotAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private Robot2D robot;
    [SerializeField] private float dashLoopVolume = 1f;
    [SerializeField] private float hitVolume = 1f;

    private bool dashLoopActive;

    protected override void Awake()
    {
        if (robot == null)
        {
            robot = GetComponent<Robot2D>();
        }

        base.Awake();
    }

    public void StartDashLoop()
    {
        dashLoopActive = true;
        StartLoop(EnemyAudioPaths.RobotDashLoop, dashLoopVolume);
    }

    public void StopDashLoop()
    {
        dashLoopActive = false;
        StopLoop();
    }

    public void PlayHit()
    {
        PlayOneShot(EnemyAudioPaths.RobotHit, hitVolume);
    }

    private void LateUpdate()
    {
        if (!dashLoopActive || robot == null || robot.IsDrinkFrozen || robot.IsStompPaused)
        {
            if (!dashLoopActive)
            {
                StopLoop();
            }

            return;
        }

        if (robot.CurrentBehavior != RobotBehavior.Charge)
        {
            StopDashLoop();
        }
    }
}
