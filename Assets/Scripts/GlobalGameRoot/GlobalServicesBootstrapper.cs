using UnityEngine;

public static class GlobalServicesBootstrapper
{
    private const string GlobalRootResourcePath = "GlobalGameRoot";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureGlobalGameRootExists()
    {
        // 场上已经有全局根，就不再重复生成
        if (Object.FindFirstObjectByType<GlobalGameRoot>() != null)
        {
            return;
        }

        // 从 Resources 加载预制体
        GlobalGameRoot prefab = Resources.Load<GlobalGameRoot>(GlobalRootResourcePath);

        if (prefab == null)
        {
            Debug.LogError(
                $"未找到 GlobalGameRoot 预制体，请确认路径是否为 Resources/{GlobalRootResourcePath}.prefab"
            );
            return;
        }

        // 生成全局根
        Object.Instantiate(prefab);
    }
}