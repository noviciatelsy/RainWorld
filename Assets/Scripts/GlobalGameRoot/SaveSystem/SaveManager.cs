
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private FileDataHandler dataHandler;      // 专门负责“文件读写”的工具类
    [SerializeField] private GameData gameData;               // 当前这一局游戏在内存中的存档

    [SerializeField] private string fileName = "RainWorldYC.json";   // 存档文件名（会和 persistentDataPath 组合成完整路径）
    [SerializeField] private bool encryptData = true; // 是否需要加密


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 用 路径 + 文件名 生成一个 FileDataHandler
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);

        // 从文件里读出 GameData（可能是 null，也可能是一份完整存档）
        gameData = dataHandler.loadData();
        // 如果没有读到存档（比如第一次进入游戏）
        if (gameData == null)
        {
            // 创建一份新的默认 GameData（此时里面的字段都是默认值）
            gameData = new GameData();

        }

    }

    public void SaveGame()
    {

        if (gameData == null)
        {
            // 防御：万一在任何时候 gameData 还是空的，先 new 一份
            gameData = new GameData();
        }

        // 把最终收集好的 GameData 交给 FileDataHandler，写到磁盘文件中
        dataHandler.SaveData(gameData);
    }

    [ContextMenu("Delete Saved Data")]
    public void DeleteSavedData() // 编辑器内使用
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);

        // 调用 FileDataHandler 的删除方法，删掉对应路径的存档文件
        dataHandler.Delete();
    }

    public GameData GetRunTimeGameData()
    {
        return gameData;
    }

}
