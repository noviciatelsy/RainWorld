using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Enemy Information Data/Enemy Information DataBase", fileName = "EnemyInformationDataBase")]
public class EnemyInformationDataBaseSO : ScriptableObject
{
    public EnemyInformationDataSO[] enemyInformationDataBase;

    public EnemyInformationDataSO GetEnemyInformationData(string saveID)
    {
        return enemyInformationDataBase.FirstOrDefault(item => item != null && item.SaveID == saveID); // 根据id获取对应SO
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill With All EnemyInformationData")]
    public void CollectEnemyInformationData()
    {
        string[] guids = AssetDatabase.FindAssets("t:EnemyInformationDataSO");
        enemyInformationDataBase = guids
         .Select(guid => AssetDatabase.LoadAssetAtPath<EnemyInformationDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
         .Where(item => item != null)
         .ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif
}
