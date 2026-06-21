using UnityEngine;

/// <summary>
/// 怪物情报「操作触发解锁」的统一入口。
/// </summary>
public static class EnemyIntelligenceUnlockUtility
{
    public static bool TryUnlock(EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        if (enemyIntelligenceData == null)
        {
            return false;
        }

        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null)
        {
            Debug.LogWarning(
                $"[IntelligenceUnlock] IntelligenceArchiveManager 缺失，无法解锁 {enemyIntelligenceData.intelligenceName}"
            );
            return false;
        }

        return archiveManager.UnlockEnemyIntelligence(enemyIntelligenceData);
    }

    public static bool TryUnlockByName(string intelligenceName)
    {
        if (string.IsNullOrWhiteSpace(intelligenceName))
        {
            return false;
        }

        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager == null)
        {
            Debug.LogWarning(
                $"[IntelligenceUnlock] IntelligenceArchiveManager 缺失，无法解锁 {intelligenceName}"
            );
            return false;
        }

        return archiveManager.TryUnlockEnemyIntelligenceByName(intelligenceName);
    }
}
