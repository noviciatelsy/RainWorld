using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Setup/Intelligence Data/Intelligence DataBase", fileName = "IntelligenceDataBase ")]
public class IntelligenceDataBaseSO : ScriptableObject
{
    public IntelligenceDataSO[] intelligenceDataBase;

    public IntelligenceDataSO GetIntelligenceData(string saveID)
    {
        return intelligenceDataBase.FirstOrDefault(item => item != null && item.SaveID == saveID); // 根据id获取对应SO
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill With All IntelligenceData")]
    public void CollectIntelligenceData()
    {
        string[] guids = AssetDatabase.FindAssets("t:IntelligenceDataSO");

        intelligenceDataBase = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<IntelligenceDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif
}