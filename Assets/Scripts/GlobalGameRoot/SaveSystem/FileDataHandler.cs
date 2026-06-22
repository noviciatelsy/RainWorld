using System;
using System.IO;
using UnityEngine;

public class FileDataHandler
{
    private string fullPath;
    private bool encryptData; // �Ƿ���Ҫ����
    private string codeWord = "RainWorld";

    public FileDataHandler(string dataDirPath, string dataFileName, bool encryptData)
    {
        // dataDirPath��Ŀ¼·�������� Application.persistentDataPath��
        fullPath = Path.Combine(dataDirPath, dataFileName);
        this.encryptData = encryptData;
    }

    public void SaveData(GameData gameData)
    {
        try
        {
            // Directory.CreateDirectory(Ŀ¼)��
            // ���Ŀ¼������ �� �ᴴ�����������м�������Ŀ¼��
            // ���Ŀ¼�Ѿ����� �� ʲôҲ���������ᱨ���
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToSave = JsonUtility.ToJson(gameData, true); // ת�� JSON ���Ե�һ���ı�

            if (encryptData) // �����Ҫ����
            {
                dataToSave = EncryptDecrypt(dataToSave); // �����ݼ���
            }

            // using (...) { ... }��
            // C# ����﷨��
            // ȷ��������� stream ֮�󣬻��Զ����� stream.Dispose()���ͷ���Դ���ر��ļ������
            // ��ʹ��;���쳣��Ҳ�ܱ�֤�������ǹ��ļ�
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            // FileMode.Create��
            // ����ļ������� �� �������ļ�
            // ����ļ��Ѵ��� �� ֱ�Ӹ���ԭ�ļ�
            {
                // StreamWriter���������á��ı���ʽ��������д�ַ���
                // ����װ��ǰ��� FileStream stream
                // ��Ϲ�ϵ��StreamWriter �� FileStream �� �����ϵ�ʵ���ļ�
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToSave);
                    //  �Ѹո����л��õ� JSON �ַ���д���ļ���
                    // ��һ�������󣬴����Ͼͳ�����һ�������Ĵ浵�ļ������ݾ��� JSON��
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

        if (File.Exists(fullPath)) // �ڶ��ļ�ǰ�ȼ��һ�����·�����Ƿ�������ļ�
        {
            try
            {
                string dataToLoad = "";

                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream)) // �� StreamWriter ����������������������ı��ַ�����
                    {
                        dataToLoad = reader.ReadToEnd(); // һ�����������ļ����ݶ��꣬�����ַ���
                    }
                }

                if (encryptData)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                loadData = JsonUtility.FromJson<GameData>(dataToLoad);
                // ����һ���µ� GameData ����,��JSON����ֶ����ȥ
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
            File.Delete(fullPath); // �����Ӵ�����ɾ����� JSON �浵
        }
    }

    private string EncryptDecrypt(string data)
    {
        char[] result = new char[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (char)(data[i] ^ codeWord[i % codeWord.Length]);
            // ^��C# �İ�λ�������� XOR
            // �� % �� codeWord �������ѭ������Կ
            // ���������ַ��� �� �� XOR һ�� �� �õ�����
            // ����ͬһ��������ͬһ�� key ȥ����������� �� �� XOR һ�� �� �ỹԭ��ԭʼ����
        }

        return new string(result);
    }

}
