using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Setup/Enemy Information Data/Enemy Information Data", fileName = "EnemyInformationData - ")]
public class EnemyInformationDataSO : ScriptableObject
{
    public string enemyName;
    public Sprite enemyPicture;
    public EnemyIntelligenceDataSO[] enemyIntelligences;

    [SerializeField, HideInInspector] private string saveID;
    public string SaveID => saveID;

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        string newSaveID = AssetDatabase.AssetPathToGUID(path);

        if (saveID != newSaveID)
        {
            saveID = newSaveID;
            EditorUtility.SetDirty(this);
        }
#endif
    }

    public bool ContainsEnemyIntelligence(EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        if (enemyIntelligenceData == null || enemyIntelligences == null)
        {
            return false;
        }

        for (int i = 0; i < enemyIntelligences.Length; i++)
        {
            if (enemyIntelligences[i] == enemyIntelligenceData)
            {
                return true;
            }
        }

        return false;
    }
}