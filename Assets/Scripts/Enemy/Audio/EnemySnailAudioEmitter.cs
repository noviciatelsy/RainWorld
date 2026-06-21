using UnityEngine;

/// <summary>蜗牛：爬行循环、进食。</summary>
public class EnemySnailAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private Snail2D snail;
    [SerializeField] private float crawlLoopVolume = 1f;
    [SerializeField] private float eatVolume = 1f;

    protected override void Awake()
    {
        if (snail == null)
        {
            snail = GetComponent<Snail2D>();
        }

        base.Awake();
    }

    private void LateUpdate()
    {
        if (snail == null || snail.IsStompPaused)
        {
            StopLoop();
            return;
        }

        bool moving = !snail.Arrived;

        if (moving)
        {
            StartLoop(EnemyAudioPaths.SnailCrawlLoop, crawlLoopVolume);
        }
        else
        {
            StopLoop();
        }
    }

    public void PlayEat()
    {
        PlayOneShot(EnemyAudioPaths.SnailEat, eatVolume);
    }
}
