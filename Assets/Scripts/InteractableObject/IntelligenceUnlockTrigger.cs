using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntelligenceUnlockTrigger : MonoBehaviour
{
    [SerializeField] private IntelligenceDataSO normalIntelligenceData;
    [SerializeField] private EnemyInformationDataSO enemyInformationData;
    [SerializeField] private EnemyIntelligenceDataSO enemyIntelligenceData;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<Player>()!=null)
        {
            if(normalIntelligenceData!=null)
            {
                IntelligenceArchiveManager.Instance.UnlockIntelligence(normalIntelligenceData);
            }
            if(enemyInformationData!=null)
            {
                IntelligenceArchiveManager.Instance.UnlockEnemy(enemyInformationData);
            }
            if (enemyIntelligenceData!=null)
            {
                IntelligenceArchiveManager.Instance.UnlockEnemyIntelligence(enemyIntelligenceData);
            }

        }
    }
}
