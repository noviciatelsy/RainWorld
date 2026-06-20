using UnityEngine;

/// <summary>跳蛛：攻击、跳跃、落地、被踩。</summary>
public class EnemyWolfSpiderAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private float attackVolume = 1f;
    [SerializeField] private float jumpVolume = 1f;
    [SerializeField] private float landVolume = 1f;
    [SerializeField] private float stompVolume = 1f;

    public void PlayAttack()
    {
        PlayOneShot(EnemyAudioPaths.WolfSpiderAttack, attackVolume);
    }

    public void PlayJump()
    {
        PlayOneShot(EnemyAudioPaths.WolfSpiderJump, jumpVolume);
    }

    public void PlayLand()
    {
        PlayOneShot(EnemyAudioPaths.WolfSpiderLand, landVolume);
    }

    public override void NotifyStomped()
    {
        PlayOneShot(EnemyAudioPaths.WolfSpiderStomp, stompVolume);
    }
}
