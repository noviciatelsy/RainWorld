using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Firework/Firework Variant Data", fileName = "FireworkVariantData")]
public class FireworkVariantDataSO : ScriptableObject
{
    [Header("Prefabs")]
    [SerializeField] private FireworkRocket rocketPrefab;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Launch Settings")]
    [SerializeField] private Vector2 heightRange = new Vector2(4f, 7f);
    [SerializeField] private Vector2 flightDurationRange = new Vector2(0.8f, 1.2f);
    [SerializeField] private Vector2 horizontalDriftRange = new Vector2(-0.8f, 0.8f);
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Explosion Random Scale")]
    [SerializeField] private Vector2 explosionScaleRange = new Vector2(1f, 1.2f);

    [Header("Explosion Color Override")]
    [SerializeField] private bool overrideExplosionStartColor = true;
    [SerializeField] private Color firstColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color secondColor = new Color(1f, 0.25f, 0.08f, 1f);

    public FireworkRocket RocketPrefab => rocketPrefab;
    public GameObject ExplosionPrefab => explosionPrefab;
    public AnimationCurve HeightCurve => heightCurve;
    public bool OverrideExplosionStartColor => overrideExplosionStartColor;
    public Color FirstColor => firstColor;
    public Color SecondColor => secondColor;

    public float GetRandomHeight()
    {
        return Random.Range(heightRange.x, heightRange.y);
    }

    public float GetRandomFlightDuration()
    {
        return Random.Range(flightDurationRange.x, flightDurationRange.y);
    }

    public float GetRandomHorizontalDrift()
    {
        return Random.Range(horizontalDriftRange.x, horizontalDriftRange.y);
    }

    public float GetRandomExplosionScale()
    {
        return Random.Range(explosionScaleRange.x, explosionScaleRange.y);
    }
}