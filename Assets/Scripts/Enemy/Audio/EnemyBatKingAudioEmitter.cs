/// <summary>蝙蝠王：独立攻击音效，其余与蝙蝠相同。</summary>
public class EnemyBatKingAudioEmitter : EnemyBatAudioEmitter
{
    protected override string AttackClipPath => EnemyAudioPaths.BatKingAttack;
    protected override string SpotPlayerClipPath => EnemyAudioPaths.BatKingSpotPlayer;
}
