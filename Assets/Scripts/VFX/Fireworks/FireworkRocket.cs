using UnityEngine;

public class FireworkRocket : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem launchFlash;
    [SerializeField] private ParticleSystem rocketTrail;
    [SerializeField] private SpriteRenderer rocketVisual;

    [Header("Destroy Settings")]
    [SerializeField] private float destroyDelayAfterExplosion = 2f;

    private FireworkVariantDataSO variantData;
    private Vector3 startPosition;

    private float targetHeight;
    private float flightDuration;
    private float horizontalDrift;
    private float elapsedTime;

    private bool isFlying;

    public void Setup(FireworkVariantDataSO myVariantData, Vector3 myStartPosition)
    {
        variantData = myVariantData;
        startPosition = myStartPosition;

        targetHeight = variantData.GetRandomHeight();
        flightDuration = Mathf.Max(0.01f, variantData.GetRandomFlightDuration());
        horizontalDrift = variantData.GetRandomHorizontalDrift();

        elapsedTime = 0f;
        isFlying = true;

        transform.position = startPosition;
        AudioManager.Instance.PlaySFX("FireworkShootSFX");

        if (launchFlash != null)
        {
            launchFlash.Play(true);
        }

        if (rocketTrail != null)
        {
            rocketTrail.Play(true);
        }

        if (rocketVisual != null)
        {
            rocketVisual.enabled = true;
        }
    }

    private void Update()
    {
        if (isFlying == false)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        float normalizedTime = Mathf.Clamp01(elapsedTime / flightDuration);

        float curveValue = variantData.HeightCurve.Evaluate(normalizedTime);
        float currentY = curveValue * targetHeight;
        float currentX = Mathf.Lerp(0f, horizontalDrift, normalizedTime);

        transform.position = startPosition + new Vector3(currentX, currentY, 0f);

        if (normalizedTime >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        isFlying = false;
        AudioManager.Instance.PlaySFX("FireworkExplodeSFX");
        if (rocketVisual != null)
        {
            rocketVisual.enabled = false;
        }

        if (rocketTrail != null)
        {
            rocketTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        GameObject explosionObject = Instantiate(
            variantData.ExplosionPrefab,
            transform.position,
            Quaternion.identity
        );

        float explosionScale = variantData.GetRandomExplosionScale();
        explosionObject.transform.localScale = Vector3.one * explosionScale;

        ApplyVariantColor(explosionObject);
        PlayAllParticleSystems(explosionObject);

        Destroy(gameObject, destroyDelayAfterExplosion);
    }

    private void ApplyVariantColor(ParticleSystem myExplosionRoot)
    {
        if (variantData.OverrideExplosionStartColor == false)
        {
            return;
        }

        FireworkColorTarget[] colorTargets = myExplosionRoot.GetComponentsInChildren<FireworkColorTarget>(true);

        if (colorTargets.Length > 0)
        {
            foreach (FireworkColorTarget colorTarget in colorTargets)
            {
                ParticleSystem targetParticleSystem = colorTarget.GetComponent<ParticleSystem>();

                if (targetParticleSystem != null)
                {
                    ApplyStartColor(targetParticleSystem);
                }
            }

            return;
        }

        ApplyStartColor(myExplosionRoot);
    }

    private void ApplyStartColor(ParticleSystem myParticleSystem)
    {
        ParticleSystem.MainModule mainModule = myParticleSystem.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(
            variantData.FirstColor,
            variantData.SecondColor
        );
    }

    private void PlayAllParticleSystems(GameObject myExplosionObject)
    {
        ParticleSystem[] particleSystems = myExplosionObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play(true);
        }
    }

    private void ApplyVariantColor(GameObject myExplosionObject)
    {
        if (variantData.OverrideExplosionStartColor == false)
        {
            return;
        }

        FireworkColorTarget[] colorTargets = myExplosionObject.GetComponentsInChildren<FireworkColorTarget>(true);

        if (colorTargets.Length > 0)
        {
            foreach (FireworkColorTarget colorTarget in colorTargets)
            {
                ParticleSystem targetParticleSystem = colorTarget.GetComponent<ParticleSystem>();

                if (targetParticleSystem != null)
                {
                    ApplyStartColor(targetParticleSystem);
                }
            }

            return;
        }
    }
}