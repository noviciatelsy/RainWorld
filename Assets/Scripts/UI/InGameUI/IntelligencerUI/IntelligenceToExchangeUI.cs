using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntelligenceToExchangeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image enemyPictureImage;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI missionProgressText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI intelligenceNameText;

    private IntelligenceExchangeDataSO exchangeData;
    private IntelligenceArchiveManager archiveManager;
    private Action<IntelligenceExchangeDataSO> onClicked;

  
    public void Setup(
        IntelligenceExchangeDataSO newExchangeData,
        IntelligenceArchiveManager newArchiveManager,
        Sprite unknownEnemySprite,
        Sprite lockedEnemyPictureSprite,
        Action<IntelligenceExchangeDataSO> newOnClicked)
    {
        exchangeData = newExchangeData;
        archiveManager = newArchiveManager;
        onClicked = newOnClicked;

        RefreshView(unknownEnemySprite, lockedEnemyPictureSprite);

    }

    public void RefreshView(Sprite unknownEnemySprite, Sprite lockedEnemyPictureSprite)
    {
        if (exchangeData == null || archiveManager == null)
        {
            return;
        }

        EnemyInformationDataSO enemyData = exchangeData.requiredEnemyInformationData;

        SetupEnemyPicture(enemyData, unknownEnemySprite, lockedEnemyPictureSprite);
        SetupEnemyName(enemyData);
        SetupMissionProgress(enemyData);
        SetupDescription();
        SetupRewardName();
    }

    private void SetupEnemyPicture(EnemyInformationDataSO enemyData, Sprite unknownEnemySprite, Sprite lockedEnemyPictureSprite)
    {
        if (enemyPictureImage == null)
        {
            return;
        }

        Sprite pictureToShow = unknownEnemySprite;

        if (enemyData != null && archiveManager.IsEnemyUnlocked(enemyData))
        {
            if (archiveManager.IsEnemyPictureUnlocked(enemyData) && enemyData.enemyPicture != null)
            {
                pictureToShow = enemyData.enemyPicture;
            }
            else if (lockedEnemyPictureSprite != null)
            {
                pictureToShow = lockedEnemyPictureSprite;
            }
        }

        enemyPictureImage.sprite = pictureToShow;
        enemyPictureImage.enabled = pictureToShow != null;
    }

    private void SetupEnemyName(EnemyInformationDataSO enemyData)
    {
        if (enemyNameText == null)
        {
            return;
        }

        if (enemyData == null)
        {
            enemyNameText.text = "???";
            return;
        }

        enemyNameText.text = enemyData.enemyName;
    }

    private void SetupMissionProgress(EnemyInformationDataSO enemyData)
    {
        if (missionProgressText == null)
        {
            return;
        }

        int currentCount = archiveManager.GetUnlockedNonImportantEnemyIntelligenceCount(enemyData);
        int requiredCount = Mathf.Max(1, exchangeData.requiredNonImportantEnemyIntelligenceCount);

        missionProgressText.text = $"{currentCount}/{requiredCount}";
    }

    private void SetupDescription()
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.text = exchangeData.missionDescription;
    }

    private void SetupRewardName()
    {
        string rewardName = exchangeData.GetRewardName();

        if (rewardText != null)
        {
            rewardText.text = "½±Àø£º";
        }

        if (intelligenceNameText != null)
        {
            intelligenceNameText.text = rewardName;
        }
    }

    public void HandleClick()
    {
        if (exchangeData == null)
        {
            return;
        }

        onClicked?.Invoke(exchangeData);
    }


}