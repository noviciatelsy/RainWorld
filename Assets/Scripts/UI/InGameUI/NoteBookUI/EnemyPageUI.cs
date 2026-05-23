using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyPageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image enemyPictureImage;
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text enemyInformationText;

    public void SetUnknown(Sprite unknownEnemySprite, string unknownEnemyName, string unknownLineText, int unknownLineCount)
    {
        if (enemyPictureImage != null)
        {
            enemyPictureImage.sprite = unknownEnemySprite;
            enemyPictureImage.enabled = unknownEnemySprite != null;
        }

        if (enemyNameText != null)
        {
            enemyNameText.text = unknownEnemyName;
        }

        if (enemyInformationText != null)
        {
            enemyInformationText.text = BuildUnknownInformationText(unknownLineText, unknownLineCount);
        }
    }

    public void SetEnemyData(
        EnemyInformationDataSO enemyData,
        IntelligenceArchiveManager archiveManager,
        Sprite unknownEnemySprite,
        Sprite lockedEnemyPictureSprite,
        string unknownEnemyName,
        string unknownLineText,
        int unknownLineCount)
    {
        if (enemyData == null)
        {
            SetUnknown(unknownEnemySprite, unknownEnemyName, unknownLineText, unknownLineCount);
            return;
        }

        if (archiveManager == null)
        {
            Debug.LogWarning("EnemyPageUI …Ë÷√ ß∞‹£∫IntelligenceArchiveManager Œ™ø’°£");
            SetUnknown(unknownEnemySprite, unknownEnemyName, unknownLineText, unknownLineCount);
            return;
        }

        if (!archiveManager.IsEnemyUnlocked(enemyData))
        {
            SetUnknown(unknownEnemySprite, unknownEnemyName, unknownLineText, unknownLineCount);
            return;
        }

        if (enemyPictureImage != null)
        {
            bool pictureUnlocked = archiveManager.IsEnemyPictureUnlocked(enemyData);

            Sprite pictureSprite = pictureUnlocked ? enemyData.enemyPicture : lockedEnemyPictureSprite;

            if (pictureSprite == null)
            {
                pictureSprite = unknownEnemySprite;
            }

            enemyPictureImage.sprite = pictureSprite;
            enemyPictureImage.enabled = pictureSprite != null;
        }

        if (enemyNameText != null)
        {
            enemyNameText.text = enemyData.enemyName;
        }

        if (enemyInformationText != null)
        {
            enemyInformationText.text = BuildEnemyInformationText(enemyData, archiveManager, unknownLineText);
        }
    }

    private string BuildEnemyInformationText(EnemyInformationDataSO enemyData, IntelligenceArchiveManager archiveManager, string unknownLineText)
    {
        if (enemyData.enemyIntelligences == null || enemyData.enemyIntelligences.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < enemyData.enemyIntelligences.Length; i++)
        {
            EnemyIntelligenceDataSO intelligenceData = enemyData.enemyIntelligences[i];

            if (intelligenceData != null && archiveManager.IsEnemyIntelligenceUnlocked(intelligenceData))
            {
                builder.Append(intelligenceData.intelligenceText);
            }
            else
            {
                builder.Append(unknownLineText);
            }

            if (i < enemyData.enemyIntelligences.Length - 1)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private string BuildUnknownInformationText(string unknownLineText, int unknownLineCount)
    {
        if (unknownLineCount <= 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < unknownLineCount; i++)
        {
            builder.Append(unknownLineText);

            if (i < unknownLineCount - 1)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}