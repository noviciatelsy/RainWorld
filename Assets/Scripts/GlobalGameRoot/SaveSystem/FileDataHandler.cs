using System;
using System.IO;
using UnityEngine;

public class FileDataHandler
{
    private string fullPath;
    private bool encryptData; // 是否需要加密
    private string codeWord = "RainWorld";

    public FileDataHandler(string dataDirPath, string dataFileName, bool encryptData)
    {
        // dataDirPath：目录路径（比如 Application.persistentDataPath）
        fullPath = Path.Combine(dataDirPath, dataFileName);
        this.encryptData = encryptData;
    }

    public void SaveData(GameData gameData)
    {
        try
        {
            // Directory.CreateDirectory(目录)：
            // 如果目录不存在 → 会创建它（包含中间所有子目录）
            // 如果目录已经有了 → 什么也不做（不会报错）
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToSave = JsonUtility.ToJson(gameData, true); // 转成 JSON 语言的一串文本

            if (encryptData) // 如果需要加密
            {
                dataToSave = EncryptDecrypt(dataToSave); // 对数据加密
            }

            // using (...) { ... }：
            // C# 里的语法糖
            // 确保用完这个 stream 之后，会自动调用 stream.Dispose()，释放资源（关闭文件句柄）
            // 即使中途抛异常，也能保证不会忘记关文件
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            // FileMode.Create：
            // 如果文件不存在 → 创建新文件
            // 如果文件已存在 → 直接覆盖原文件
            {
                // StreamWriter：帮助你用“文本方式”往流里写字符串
                // 它包装了前面的 FileStream stream
                // 组合关系：StreamWriter → FileStream → 磁盘上的实际文件
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToSave);
                    //  把刚刚序列化好的 JSON 字符串写入文件。
                    // 这一步结束后，磁盘上就出现了一个真正的存档文件（内容就是 JSON）
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error on trying to save data on file:" + fullPath + "\n" + e);
        }
    }

    public GameData loadData()
    {
        GameData loadData = null;

        if (File.Exists(fullPath)) // 在读文件前先检查一下这个路径上是否真的有文件
        {
            try
            {
                string dataToLoad = "";

                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream)) // 和 StreamWriter 反过来，是用来从流里读文本字符串的
                    {
                        dataToLoad = reader.ReadToEnd(); // 一口气把整个文件内容读完，返回字符串
                    }
                }

                if (encryptData)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                loadData = JsonUtility.FromJson<GameData>(dataToLoad);
                // 创建一个新的 GameData 对象,把JSON里的字段填进去
            }
            catch (Exception e)
            {
                Debug.Log("Error on trying to load data from file:" + fullPath + "\n" + e);
            }
        }

        return loadData;
    }

    public void Delete()
    {
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath); // 真正从磁盘上删掉这份 JSON 存档
        }
    }

    private string EncryptDecrypt(string data)
    {
        char[] result = new char[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (char)(data[i] ^ codeWord[i % codeWord.Length]);
            // ^：C# 的按位异或运算符 XOR
            // 用 % 把 codeWord 变成无限循环的密钥
            // 传入明文字符串 → 它 XOR 一遍 → 得到密文
            // 再用同一个函数、同一个 key 去处理这个密文 → 又 XOR 一遍 → 会还原成原始明文
        }

        return new string(result);
    }

}
