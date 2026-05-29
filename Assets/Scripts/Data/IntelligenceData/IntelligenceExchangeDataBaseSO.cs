using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Setup/Intelligence Exchange/Intelligence Exchange DataBase", fileName = "IntelligenceExchangeDataBase")]
public class IntelligenceExchangeDataBaseSO : ScriptableObject
{
    public IntelligenceExchangeDataSO[] intelligenceExchangeDataBase;

    public List<IntelligenceExchangeDataSO> GetAllExchangeData()
    {
        if (intelligenceExchangeDataBase == null)
        {
            return new List<IntelligenceExchangeDataSO>();
        }

        return intelligenceExchangeDataBase
            .Where(item => item != null)
            .ToList();
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill With All IntelligenceExchangeData")]
    public void CollectIntelligenceExchangeData()
    {
        string[] guids = AssetDatabase.FindAssets("t:IntelligenceExchangeDataSO");

        intelligenceExchangeDataBase = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<IntelligenceExchangeDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif
}