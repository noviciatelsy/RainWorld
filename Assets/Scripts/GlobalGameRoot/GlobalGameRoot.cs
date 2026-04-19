using UnityEngine;

public class GlobalGameRoot : MonoBehaviour
{
    public static GlobalGameRoot Instance { get; private set; }

    public static bool IsReady
    {
        get
        {
            return Instance != null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 让整个全局根跨场景保留
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}