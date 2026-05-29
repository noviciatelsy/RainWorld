using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntelligenceToPurchaseUI : MonoBehaviour
{
    [Header("References")]
    
    [SerializeField] private TextMeshProUGUI intelligenceNameText;
    [SerializeField] private TextMeshProUGUI priceText;

    private ArchivePurchaseOffer purchaseOffer;
    private Action<ArchivePurchaseOffer> onClicked;



    public void Setup(ArchivePurchaseOffer newPurchaseOffer, Action<ArchivePurchaseOffer> newOnClicked)
    {
        purchaseOffer = newPurchaseOffer;
        onClicked = newOnClicked;

        RefreshView();

    }

    private void RefreshView()
    {
        if (purchaseOffer == null)
        {
            return;
        }

        if (intelligenceNameText != null)
        {
            intelligenceNameText.text = purchaseOffer.DisplayName;
        }

        if (priceText != null)
        {
            priceText.text = purchaseOffer.Price.ToString();
        }
    }

    public void HandleClick()
    {
        if (purchaseOffer == null)
        {
            return;
        }

        onClicked?.Invoke(purchaseOffer);
    }

}