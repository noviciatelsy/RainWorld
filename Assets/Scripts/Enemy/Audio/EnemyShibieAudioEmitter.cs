using UnityEngine;

/// <summary>尸鳖：移动循环、被踩。</summary>
public class EnemyShibieAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private SurfaceWalker2D walker;
    [SerializeField] private float moveLoopVolume = 1f;
    [SerializeField] private float stompVolume = 1f;

    private void LateUpdate()
    {
        if (walker == null || !walker.gameObject.activeInHierarchy || walker.IsStompPaused)
        {
            StopLoop();
            return;
        }

        bool moving = !walker.Arrived;

        if (moving)
        {
            StartLoop(EnemyAudioPaths.ShibieMoveLoop, moveLoopVolume);
        }
        else
        {
            StopLoop();
        }
    }

    public override void NotifyStomped()
    {
        PlayOneShot(EnemyAudioPaths.ShibieStomp, stompVolume);
        StopLoop();
    }

    protected override void Awake()
    {
        if (walker == null)
        {
            walker = GetComponent<SurfaceWalker2D>();
        }

        base.Awake();
    }
}
