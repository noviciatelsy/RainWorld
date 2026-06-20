using UnityEngine;

/// <summary>蝙蝠：翅膀循环、攻击、发现玩家。</summary>
public class EnemyBatAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] protected Bat2D bat;
    [SerializeField] private float wingLoopVolume = 1f;
    [SerializeField] private float attackVolume = 1f;
    [SerializeField] private float spotPlayerVolume = 1f;

    protected virtual string WingLoopPath => EnemyAudioPaths.BatWingLoop;
    protected virtual string AttackClipPath => EnemyAudioPaths.BatAttack;
    protected virtual string SpotPlayerClipPath => EnemyAudioPaths.BatSpotPlayer;

    protected override void Awake()
    {
        if (bat == null)
        {
            bat = GetComponent<Bat2D>();
        }

        base.Awake();
    }

    private void LateUpdate()
    {
        if (bat == null || bat.IsStompPaused)
        {
            StopLoop();
            return;
        }

        bool shouldPlayWings = !bat.IsCoolingDown
            && (!bat.Arrived || bat.IsInAttackSequence);

        if (shouldPlayWings)
        {
            StartLoop(WingLoopPath, wingLoopVolume);
        }
        else
        {
            StopLoop();
        }
    }

    public void PlayAttack()
    {
        PlayOneShot(AttackClipPath, attackVolume);
    }

    public void PlaySpotPlayer()
    {
        PlayOneShot(SpotPlayerClipPath, spotPlayerVolume);
    }
}
