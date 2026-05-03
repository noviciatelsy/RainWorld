using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusBarUI : MonoBehaviour
{
    [SerializeField] private Slider currentHealthSlider;
    [SerializeField] private Slider hungerBarSlider;
    [SerializeField] private RectTransform maxHealthHealthRect;
    [SerializeField] private RectTransform hungerBarBackgroundRect;
    [SerializeField] private Image hungerIcon;
    private PlayerVitals playerVitals;
    private int maxHealthAmount;

    private void OnEnable()
    {
        TrySubscribe(PlayerManager.Instance.TryGetCurrentPlayer());
        PlayerManager.Instance.OnPlayerRegistered += TrySubscribe;
    }

    private void OnDisable()
    {
        if(playerVitals != null)
        {
            playerVitals.CurrentHealthChanged -= UpdateCurrentHealth;
            playerVitals.HungerChanged -= UpdateHungerBar;
            playerVitals.MaxHealthChanged -= UpdateMaxHealth;
        }
    }

    private void TrySubscribe(Player player)
    {
        if (player == null)
        {
            return;
        }
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        if ( playerVitals!= null)
        {
            this.playerVitals = playerVitals;
            maxHealthAmount=playerVitals.BaseMaxHealth;
 
            playerVitals.CurrentHealthChanged += UpdateCurrentHealth;
            playerVitals.HungerChanged += UpdateHungerBar;
            playerVitals.MaxHealthChanged += UpdateMaxHealth;
        }
    }

    private void UpdateCurrentHealth(int currentHealth)
    {
        currentHealthSlider.value =(float)currentHealth/maxHealthAmount;

    }

    private void UpdateMaxHealth(int newMaxHealth)
    {
        maxHealthHealthRect.localScale = new Vector3((float)newMaxHealth / maxHealthAmount, 1, 1);
    }

    private void UpdateHungerBar(int newHungerAmount)
    {
        if(newHungerAmount==0)
        {
            hungerIcon.enabled = false;
        }
        else
        {
            hungerIcon.enabled=true;
        }
        hungerBarSlider.value = (float)newHungerAmount /maxHealthAmount;
        hungerBarBackgroundRect.localScale = new Vector3((float)newHungerAmount / maxHealthAmount, 1, 1);
    }
}
