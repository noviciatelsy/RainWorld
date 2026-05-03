using System;
using System.Collections;
using UnityEngine;


public class PlayerVitals : MonoBehaviour
{
    [Header("生命设置")]

    private int baseMaxHealth = 100;

    [SerializeField]
    private bool startWithFullHealth = true;

    [SerializeField, Min(0)]
    private int customStartHealth = 100;

    [Header("饥饿设置")]
    [SerializeField, Min(0)]
    private int startHunger = 0;

    [SerializeField]
    private bool autoIncreaseHunger = true;

    [SerializeField, Min(0.01f)]
    private float hungerIncreaseInterval = 15f;

    [SerializeField, Min(0)]
    private int hungerIncreaseAmount = 1;


    public event Action<int> CurrentHealthChanged;

    public event Action<int> MaxHealthChanged;

    public event Action<int> HungerChanged;

    public event Action PlayerDied;

    private int currentHealth;
    private int currentHunger;
    private bool isDead;
    private Coroutine hungerCoroutine;

    public int BaseMaxHealth => baseMaxHealth;

    public int CurrentHealth => currentHealth;

    public int CurrentHunger => currentHunger;

    public int CurrentMaxHealth => Mathf.Max(0, baseMaxHealth - currentHunger);

    public bool IsDead => isDead;

    public float HealthRate
    {
        get
        {
            if (CurrentMaxHealth <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)currentHealth / CurrentMaxHealth);
        }
    }

    private void Awake()
    {
        currentHunger = Mathf.Clamp(startHunger, 0, baseMaxHealth);

        if (startWithFullHealth)
        {
            currentHealth = CurrentMaxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(customStartHealth, 0, CurrentMaxHealth);
        }

        isDead = currentHealth <= 0;
    }

    private void OnEnable()
    {
        if (autoIncreaseHunger)
        {
            StartAutoIncreaseHunger();
        }
    }

    private void OnDisable()
    {
        StopAutoIncreaseHunger();
    }

    private void Start()
    {
        CurrentHealthChanged?.Invoke(currentHealth);
        HungerChanged?.Invoke(currentHunger);
        MaxHealthChanged?.Invoke(baseMaxHealth);
    }

    /// <summary>
    /// 开始自动增加饥饿度。
    /// </summary>
    public void StartAutoIncreaseHunger()
    {
        StopAutoIncreaseHunger();

        hungerCoroutine = StartCoroutine(AutoIncreaseHungerCoroutine());
    }

    /// <summary>
    /// 停止自动增加饥饿度。
    /// </summary>
    public void StopAutoIncreaseHunger()
    {
        if (hungerCoroutine != null)
        {
            StopCoroutine(hungerCoroutine);
            hungerCoroutine = null;
        }
    }

    private IEnumerator AutoIncreaseHungerCoroutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(hungerIncreaseInterval);

            AddHunger(hungerIncreaseAmount);
        }
    }

    /// <summary>
    /// 增加饥饿度。
    /// 饥饿度越高，血上限越低。
    /// </summary>
    public void AddHunger(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        ChangeHunger(amount);
    }

    /// <summary>
    /// 减少饥饿度。
    /// 饥饿度降低会提高血上限，但不会自动恢复当前血量。
    /// </summary>
    public void ReduceHunger(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        ChangeHunger(-amount);
    }

    /// <summary>
    /// 减少当前血量。
    /// </summary>
    public void ReduceHealth(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        SetCurrentHealth(currentHealth - amount);
    }

    /// <summary>
    /// 增加当前血量。
    /// 回血不会超过当前血上限。
    /// </summary>
    public void AddHealth(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        int newHealth = Mathf.Min(currentHealth + amount, CurrentMaxHealth);

        SetCurrentHealth(newHealth);
    }

    /// <summary>
    /// 直接设置当前血量。
    /// 适合存档读取、调试、特殊道具等情况。
    /// </summary>
    public void SetCurrentHealth(int value)
    {
        int oldHealth = currentHealth;

        currentHealth = Mathf.Clamp(value, 0, CurrentMaxHealth);

        if (currentHealth != oldHealth)
        {
            CurrentHealthChanged?.Invoke(currentHealth);
        }

        CheckDeath();
    }

    /// <summary>
    /// 直接设置饥饿度。
    /// </summary>
    public void SetHunger(int value)
    {
        int targetHunger = Mathf.Clamp(value, 0, baseMaxHealth);
        int delta = targetHunger - currentHunger;

        if (delta == 0)
        {
            return;
        }

        ChangeHunger(delta);
    }

    ///// 复活。
    //public void Revive(int reviveHealth = -1)
    //{
    //    if (!isDead)
    //    {
    //        return;
    //    }

    //    isDead = false;

    //    int targetHealth = reviveHealth < 0 ? CurrentMaxHealth : reviveHealth;
    //    currentHealth = Mathf.Clamp(targetHealth, 1, Mathf.Max(1, CurrentMaxHealth));

    //    CurrentHealthChanged?.Invoke(currentHealth);

    //    if (autoIncreaseHunger)
    //    {
    //        StartAutoIncreaseHunger();
    //    }
    //}

    private void ChangeHunger(int delta)
    {
        int oldHunger = currentHunger;
        int oldMaxHealth = CurrentMaxHealth;
        int oldHealth = currentHealth;

        currentHunger = Mathf.Clamp(currentHunger + delta, 0, baseMaxHealth);

        int newMaxHealth = CurrentMaxHealth;

        if (currentHunger != oldHunger)
        {
            HungerChanged?.Invoke(currentHunger);
        }

        if (newMaxHealth != oldMaxHealth)
        {
            MaxHealthChanged?.Invoke(newMaxHealth);
        }

        if (currentHealth > newMaxHealth)
        {
            currentHealth = newMaxHealth;
        }

        if (currentHealth != oldHealth)
        {
            CurrentHealthChanged?.Invoke(currentHealth);
        }


        CheckDeath();
    }

    private void CheckDeath()
    {
        if (isDead)
        {
            return;
        }

        if (currentHealth > 0)
        {
            return;
        }

        isDead = true;

        StopAutoIncreaseHunger();

        PlayerDied?.Invoke();
    }

}