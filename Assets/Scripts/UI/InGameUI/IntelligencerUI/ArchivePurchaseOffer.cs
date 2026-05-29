public enum ArchivePurchaseOfferType
{
    NormalIntelligence,
    EnemyIntelligence
}

public class ArchivePurchaseOffer
{
    public ArchivePurchaseOfferType offerType;

    public IntelligenceDataSO intelligenceData;

    public EnemyInformationDataSO enemyInformationData;
    public EnemyIntelligenceDataSO enemyIntelligenceData;

    public int Price
    {
        get
        {
            if (offerType == ArchivePurchaseOfferType.NormalIntelligence)
            {
                if (intelligenceData == null)
                {
                    return 0;
                }

                return intelligenceData.priceToPurchase;
            }

            if (enemyIntelligenceData == null)
            {
                return 0;
            }

            return enemyIntelligenceData.priceToPurchase;
        }
    }

    public string DisplayName
    {
        get
        {
            if (offerType == ArchivePurchaseOfferType.NormalIntelligence)
            {
                if (intelligenceData == null)
                {
                    return "???";
                }

                if (!string.IsNullOrEmpty(intelligenceData.intelligenceName))
                {
                    return intelligenceData.intelligenceName;
                }

                return intelligenceData.name;
            }

            if (enemyIntelligenceData == null)
            {
                return "???";
            }

            if (!string.IsNullOrEmpty(enemyIntelligenceData.intelligenceName))
            {
                return enemyIntelligenceData.intelligenceName;
            }

            return enemyIntelligenceData.name;
        }
    }

    public static ArchivePurchaseOffer CreateNormalIntelligenceOffer(IntelligenceDataSO intelligenceData)
    {
        return new ArchivePurchaseOffer
        {
            offerType = ArchivePurchaseOfferType.NormalIntelligence,
            intelligenceData = intelligenceData
        };
    }

    public static ArchivePurchaseOffer CreateEnemyIntelligenceOffer(EnemyInformationDataSO enemyInformationData, EnemyIntelligenceDataSO enemyIntelligenceData)
    {
        return new ArchivePurchaseOffer
        {
            offerType = ArchivePurchaseOfferType.EnemyIntelligence,
            enemyInformationData = enemyInformationData,
            enemyIntelligenceData = enemyIntelligenceData
        };
    }
}