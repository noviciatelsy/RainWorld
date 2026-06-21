using UnityEngine;

/// <summary>蝙蝠：翅膀音效（播完一段 → 间隔 → 再播）；攻击、发现玩家。</summary>
public class EnemyBatAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] protected Bat2D bat;
    [SerializeField] private float wingMoveGapAfterClip = 1f;
    [SerializeField] private float wingMoveVolume = 1f;
    [SerializeField] private float attackVolume = 1f;
    [SerializeField] private float spotPlayerVolume = 1f;

    private float wingMoveCooldown;

    protected virtual string WingMoveClipPath => EnemyAudioPaths.BatWingLoop;
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
            wingMoveCooldown = 0f;
            StopLoop();
            return;
        }

        if (!ShouldPlayWingMoveSound())
        {
            wingMoveCooldown = 0f;
            StopLoop();
            return;
        }

        wingMoveCooldown -= Time.deltaTime;

        if (wingMoveCooldown > 0f)
        {
            return;
        }

        AudioClip wingClip = LoadClip(WingMoveClipPath);

        if (wingClip == null)
        {
            wingMoveCooldown = wingMoveGapAfterClip;
            return;
        }

        PlayOneShotClip(wingClip, wingMoveVolume);
        wingMoveCooldown = wingClip.length + wingMoveGapAfterClip;
    }

    private bool ShouldPlayWingMoveSound()
    {
        if (bat.IsCoolingDown)
        {
            return false;
        }

        if (bat.IsInAttackSequence)
        {
            return true;
        }

        if (bat.CurrentBehavior == BatBehavior.Hunt)
        {
            return true;
        }

        return bat.CurrentBehavior == BatBehavior.Idle && !bat.Arrived;
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
