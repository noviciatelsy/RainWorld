using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    MainMenu,
    Base,
    Game
}

public sealed class SceneSwitchManager : MonoBehaviour
{
    public static SceneSwitchManager Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenuScene";
    public string gameSceneName = "GameScene";
    public string baseSceneName = "BaseScene";

    private Dictionary<SceneType, string> sceneNameMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitSceneNameMap();
    }

    private void InitSceneNameMap()
    {
        sceneNameMap = new Dictionary<SceneType, string>()
        {
            { SceneType.MainMenu, mainMenuSceneName },
            { SceneType.Game, gameSceneName },
            { SceneType.Base, baseSceneName }
        };
    }

    public void SwitchToScene(SceneType sceneType)
    {
        string sceneName = GetSceneName(sceneType);

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"场景名为空，无法切换到：{sceneType}");
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }


    public string GetSceneName(SceneType sceneType)
    {
        if (sceneNameMap == null)
        {
            InitSceneNameMap();
        }

        if (sceneNameMap.TryGetValue(sceneType, out string sceneName))
        {
            return sceneName;
        }

        Debug.LogError($"没有找到对应的场景类型：{sceneType}");
        return string.Empty;
    }
}