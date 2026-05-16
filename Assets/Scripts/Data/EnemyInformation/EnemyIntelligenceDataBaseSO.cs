using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Enemy Intelligence Data/Enemy Intelligence DataBase", fileName = "EnemyIntelligenceDataBase ")]
public class EnemyIntelligenceDataBaseSO : ScriptableObject
{
    public EnemyIntelligenceDataSO[] enemyIntelligenceDataBase;

    public EnemyIntelligenceDataSO GetEnemyIntelligenceData(string saveID)
    {
        return enemyIntelligenceDataBase.FirstOrDefault(item => item != null && item.SaveID == saveID); // 根据id获取对应SO
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill With All EnemyIntelligenceData")]
    public void CollectEnemyIntelligenceData()
    {
        string[] guids = AssetDatabase.FindAssets("t:EnemyIntelligenceDataSO");
        enemyIntelligenceDataBase = guids
         .Select(guid => AssetDatabase.LoadAssetAtPath<EnemyIntelligenceDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
         .Where(item => item != null)
         .ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif
}
