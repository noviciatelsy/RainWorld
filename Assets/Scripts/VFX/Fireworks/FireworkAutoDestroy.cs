using UnityEngine;

public class FireworkAutoDestroy : MonoBehaviour
{
    private ParticleSystem[] particleSystems;

    private void Awake()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Update()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem != null && particleSystem.IsAlive(true))
            {
                return;
            }
        }

        Destroy(gameObject);
    }
}