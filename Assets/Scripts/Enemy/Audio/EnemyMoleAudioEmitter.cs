using UnityEngine;

/// <summary>鼹鼠：偷取预警、偷取循环、送礼、钻地。</summary>
public class EnemyMoleAudioEmitter : EnemyAudioEmitter
{
    [SerializeField] private Mole2D mole;
    [SerializeField] private float stealWarningVolume = 1f;
    [SerializeField] private float stealLoopVolume = 1f;
    [SerializeField] private float giftVolume = 1f;
    [SerializeField] private float digVolume = 1f;

    protected override void Awake()
    {
        if (mole == null)
        {
            mole = GetComponent<Mole2D>();
        }

        SetAudioPivotParent(ResolveAudioPivot());
        base.Awake();
    }

    public override void NotifyStomped()
    {
        StopAll();
    }

    private Transform ResolveAudioPivot()
    {
        Transform root = mole != null ? mole.transform : transform;
        Transform texture = root.Find("Texture");
        if (texture == null)
        {
            return root;
        }

        SpriteRenderer spriteRenderer = texture.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.transform : texture;
    }

    public void PlayStealWarning()
    {
        PlayOneShot(EnemyAudioPaths.MoleStealWarning, stealWarningVolume);
    }

    public void StartStealLoop()
    {
        StartLoop(EnemyAudioPaths.MoleStealLoop, stealLoopVolume);
    }

    public void StopStealLoop()
    {
        StopLoop();
    }

    public void PlayGift()
    {
        PlayOneShot(EnemyAudioPaths.MoleGift, giftVolume);
    }

    public void PlayDigOut()
    {
        PlayOneShot(EnemyAudioPaths.MoleDigOut, digVolume);
    }

    public void PlayDigIn()
    {
        PlayOneShot(EnemyAudioPaths.MoleDigIn, digVolume);
    }
}

