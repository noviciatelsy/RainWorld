using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 注册场景中的水体视觉实例与波纹模拟器；玩家扰动通过此查找对应水体。
/// </summary>
[DisallowMultipleComponent]
public sealed class WaterVisualManager : MonoBehaviour
{
    private static WaterVisualManager instance;

    private readonly List<WaterVisualBody> activeBodies = new List<WaterVisualBody>();
    private readonly Dictionary<WaterVolume2D, WaterSurfaceRippleSimulator> rippleSimulators =
        new Dictionary<WaterVolume2D, WaterSurfaceRippleSimulator>();

    public static bool HasActiveWater => instance != null && instance.activeBodies.Count > 0;

    public static IReadOnlyList<WaterVisualBody> ActiveBodies =>
        instance != null ? instance.activeBodies : System.Array.Empty<WaterVisualBody>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void Register(WaterVisualBody body)
    {
        EnsureInstance();
        if (body == null || instance.activeBodies.Contains(body))
        {
            return;
        }

        instance.activeBodies.Add(body);
    }

    public static void Unregister(WaterVisualBody body)
    {
        if (instance == null || body == null)
        {
            return;
        }

        instance.activeBodies.Remove(body);
    }

    public static void RegisterRippleSimulator(WaterVolume2D volume, WaterSurfaceRippleSimulator simulator)
    {
        EnsureInstance();
        if (volume == null || simulator == null)
        {
            return;
        }

        instance.rippleSimulators[volume] = simulator;
    }

    public static void UnregisterRippleSimulator(WaterVolume2D volume, WaterSurfaceRippleSimulator simulator)
    {
        if (instance == null || volume == null)
        {
            return;
        }

        if (instance.rippleSimulators.TryGetValue(volume, out WaterSurfaceRippleSimulator existing)
            && existing == simulator)
        {
            instance.rippleSimulators.Remove(volume);
        }
    }

    public static bool TryGetRippleSimulator(WaterVolume2D volume, out WaterSurfaceRippleSimulator simulator)
    {
        simulator = null;
        if (instance == null || volume == null)
        {
            return false;
        }

        return instance.rippleSimulators.TryGetValue(volume, out simulator) && simulator != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        WaterVisualManager existing = FindObjectOfType<WaterVisualManager>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject host = new GameObject(nameof(WaterVisualManager));
        DontDestroyOnLoad(host);
        host.AddComponent<WaterVisualManager>();
    }
}
