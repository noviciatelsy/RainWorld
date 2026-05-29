using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum IntelligenceExchangeRewardType
{
    NormalIntelligence,
    EnemyIntelligence
}

[CreateAssetMenu(menuName = "Setup/Intelligence Exchange/Intelligence Exchange Data", fileName = "IntelligenceExchangeData - ")]
public class IntelligenceExchangeDataSO : ScriptableObject
{
    [Header("Mission Target")]
    public EnemyInformationDataSO requiredEnemyInformationData;

    [Min(1)]
    public int requiredNonImportantEnemyIntelligenceCount = 5;

    [TextArea]
    public string missionDescription;

    [Header("Reward")]
    public IntelligenceExchangeRewardType rewardType = IntelligenceExchangeRewardType.NormalIntelligence;

    public IntelligenceDataSO rewardIntelligenceData;

    public EnemyInformationDataSO rewardEnemyInformationData;
    public EnemyIntelligenceDataSO rewardEnemyIntelligenceData;

    [SerializeField, HideInInspector] private string saveID;
    public string SaveID => saveID;

    public string GetRewardName()
    {
        if (rewardType == IntelligenceExchangeRewardType.NormalIntelligence)
        {
            if (rewardIntelligenceData == null)
            {
                return "???";
            }

            if (!string.IsNullOrEmpty(rewardIntelligenceData.intelligenceName))
            {
                return rewardIntelligenceData.intelligenceName;
            }

            return rewardIntelligenceData.name;
        }

        if (rewardEnemyIntelligenceData == null)
        {
            return "???";
        }

        if (!string.IsNullOrEmpty(rewardEnemyIntelligenceData.intelligenceName))
        {
            return rewardEnemyIntelligenceData.intelligenceName;
        }

        return rewardEnemyIntelligenceData.name;
    }

    public bool HasValidReward()
    {
        if (rewardType == IntelligenceExchangeRewardType.NormalIntelligence)
        {
            return rewardIntelligenceData != null;
        }

        return rewardEnemyIntelligenceData != null;
    }

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

        if (requiredNonImportantEnemyIntelligenceCount < 1)
        {
            requiredNonImportantEnemyIntelligenceCount = 1;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}