using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IntelligencePageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI[] intelligenceTexts = new TextMeshProUGUI[4];

    public void SetPageData(List<IntelligenceDataSO> unlockedIntelligences, int startIndex)
    {
        for (int i = 0; i < intelligenceTexts.Length; i++)
        {
            if (intelligenceTexts[i] == null)
            {
                continue;
            }

            int dataIndex = startIndex + i;

            if (unlockedIntelligences != null && dataIndex >= 0 && dataIndex < unlockedIntelligences.Count)
            {
                IntelligenceDataSO intelligenceData = unlockedIntelligences[dataIndex];

                if (intelligenceData != null)
                {
                    intelligenceTexts[i].text = intelligenceData.intelligenceText;
                }
                else
                {
                    intelligenceTexts[i].text = string.Empty;
                }
            }
            else
            {
                intelligenceTexts[i].text = string.Empty;
            }
        }
    }
}