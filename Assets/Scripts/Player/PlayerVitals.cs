using System;
using System.Collections;
using UnityEngine;


public class PlayerVitals : MonoBehaviour
{
    [Header("生命设置")]

    private float baseMaxHealth = 100;

    [SerializeField]
    private bool startWithFullHealth = true;

    [SerializeField, Min(0)]
    private float customStartHealth = 100;

    [Header("防御设置")]
    [SerializeField, Min(0)]
    private float startDefense = 0;

    [Header("饥饿设置")]
    [SerializeField, Min(0)]
    private float startHunger = 0;

    [SerializeField]
    private bool autoIncreaseHunger = true;

    [SerializeField, Min(0.01f)]
    private float hungerIncreaseInterval = 15f;

    [SerializeField, Min(0)]
    private float hungerIncreaseAmount = 1;

    [Header("死亡设置")]
    [SerializeField] private SpriteRenderer playerBackpackSprite;


    public event Action<float> CurrentHealthChanged;

    public event Action<float> MaxHealthChanged;

    public event Action<float> HungerChanged;


    public event Action PlayerDied;

    private float currentHealth;
    private float currentHunger;
    private float currentDefense;
    private bool isDead;
    private Coroutine hungerCoroutine;
    private Coroutine pauseAutoIncreaseHungerCoroutine;
    private bool isAutoIncreaseHungerPaused;
    private InventoryPlayer playerInventory;

    public float BaseMaxHealth => baseMaxHealth;

    public float CurrentHealth => currentHealth;

    public float CurrentHunger => currentHunger;

    public float CurrentDefense => currentDefense;

    public float CurrentMaxHealth => Mathf.Max(0, baseMaxHealth - currentHunger);

    public bool IsDead => isDead;

    private bool hasStartedAutoIncreaseHunger = false;

    private InvisibleCloakPassiveEffect InvisibleCloakPassiveEffect;

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
        currentDefense = Mathf.Max(0, startDefense);

        if (startWithFullHealth)
        {
            currentHealth = CurrentMaxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(customStartHealth, 0, CurrentMaxHealth);
        }

        isDead = currentHealth <= 0;
        playerInventory = GetComponent<InventoryPlayer>();
        InvisibleCloakPassiveEffect=GetComponentInChildren<InvisibleCloakPassiveEffect>();
    }

    private void OnEnable()
    {

        if (autoIncreaseHunger)
        {
            if (!hasStartedAutoIncreaseHunger)
            {
                return;
            }
            StartAutoIncreaseHunger();
        }
    }

    private void OnDisable()
    {
        StopAutoIncreaseHunger();
        StopAutoIncreaseHungerPause();
    }

    private void Start()
    {
        CurrentHealthChanged?.Invoke(currentHealth);
        HungerChanged?.Invoke(currentHunger);
        MaxHealthChanged?.Invoke(baseMaxHealth);

        if (autoIncreaseHunger)
        {
            hasStartedAutoIncreaseHunger = true;
            StartAutoIncreaseHunger();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            KillPlayer();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (damageAmount <= 0 || isDead)
        {
            return;
        }

        if(InvisibleCloakPassiveEffect.isInvisible)
        {
            return;
        }

        float actualDamageAmount = Mathf.Max(0, damageAmount - currentDefense);

        ReduceHealth(actualDamageAmount);
    }

    /// <summary>
    /// 增加防御力。
    /// 防御力会减少 TakeDamage 中实际受到的伤害。
    /// </summary>
    public void AddDefense(float amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        SetDefense(currentDefense + amount);
    }

    /// <summary>
    /// 减少防御力。
    /// 防御力不会低于 0。
    /// </summary>
    public void ReduceDefense(float amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        SetDefense(currentDefense - amount);
    }

    /// <summary>
    /// 临时增加防御力。
    /// 指定时间结束后，会移除本次增加的防御力。
    /// </summary>
    public void AddDefenseTemporarily(float amount, float time)
    {
        if (amount <= 0 || time <= 0f || isDead)
        {
            return;
        }

        StartCoroutine(AddDefenseTemporarilyCoroutine(amount, time));
    }

    private IEnumerator AddDefenseTemporarilyCoroutine(float amount, float time)
    {
        AddDefense(amount);

        yield return new WaitForSeconds(time);

        ReduceDefense(amount);
    }

    /// <summary>
    /// 临时减少防御力。
    /// 指定时间结束后，会恢复本次实际减少的防御力。
    /// </summary>
    public void ReduceDefenseTemporarily(float amount, float time)
    {
        if (amount <= 0 || time <= 0f || isDead)
        {
            return;
        }

        StartCoroutine(ReduceDefenseTemporarilyCoroutine(amount, time));
    }

    private IEnumerator ReduceDefenseTemporarilyCoroutine(float amount, float time)
    {
        float defenseBeforeReduce = currentDefense;

        ReduceDefense(amount);

        float actualReducedAmount = defenseBeforeReduce - currentDefense;

        yield return new WaitForSeconds(time);

        AddDefense(actualReducedAmount);
    }

    /// <summary>
    /// 直接设置防御力。
    /// 防御力不会低于 0。
    /// </summary>
    public void SetDefense(float value)
    {
        float oldDefense = currentDefense;

        currentDefense = Mathf.Max(0, value);

    }

    /// <summary>
    /// 开始自动增加饥饿度。
    /// </summary>
    public void StartAutoIncreaseHunger()
    {
        StopAutoIncreaseHunger();

        if (isAutoIncreaseHungerPaused)
        {
            return;
        }

        if (GameStateManager.Instance.currentGameState == GameState.Base)
        {
            return;
        }

        hungerCoroutine = StartCoroutine(AutoIncreaseHungerCoroutine());
    }

    /// <summary>
    /// 暂停自动增加饥饿度。
    /// 在指定秒数结束后，如果条件允许，会继续自动增加饥饿度。
    /// </summary>
    public void PauseAutoIncreaseHunger(float pauseTime)
    {
        if (pauseTime <= 0f || isDead)
        {
            return;
        }

        if (pauseAutoIncreaseHungerCoroutine != null)
        {
            StopCoroutine(pauseAutoIncreaseHungerCoroutine);
            pauseAutoIncreaseHungerCoroutine = null;
        }

        pauseAutoIncreaseHungerCoroutine = StartCoroutine(PauseAutoIncreaseHungerCoroutine(pauseTime));
    }

    private IEnumerator PauseAutoIncreaseHungerCoroutine(float pauseTime)
    {
        isAutoIncreaseHungerPaused = true;

        StopAutoIncreaseHunger();

        yield return new WaitForSeconds(pauseTime);

        pauseAutoIncreaseHungerCoroutine = null;
        isAutoIncreaseHungerPaused = false;

        if (!autoIncreaseHunger || isDead || !isActiveAndEnabled || !hasStartedAutoIncreaseHunger)
        {
            yield return null;
        }

        StartAutoIncreaseHunger();
    }

    private void StopAutoIncreaseHungerPause()
    {
        if (pauseAutoIncreaseHungerCoroutine != null)
        {
            StopCoroutine(pauseAutoIncreaseHungerCoroutine);
            pauseAutoIncreaseHungerCoroutine = null;
        }

        isAutoIncreaseHungerPaused = false;
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
    public void AddHunger(float amount)
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
    public void ReduceHunger(float amount)
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
    public void ReduceHealth(float amount)
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
    public void AddHealth(float amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        float newHealth = Mathf.Min(currentHealth + amount, CurrentMaxHealth);

        SetCurrentHealth(newHealth);
    }

    /// <summary>
    /// 直接设置当前血量。
    /// 适合存档读取、调试、特殊道具等情况。
    /// </summary>
    public void SetCurrentHealth(float value)
    {
        float oldHealth = currentHealth;

        currentHealth = Mathf.Clamp(value, 0, CurrentMaxHealth);

        if (currentHealth != oldHealth)
        {
            CurrentHealthChanged?.Invoke(currentHealth);
        }

        CheckDeath();
    }

    /// <summary>
    /// 在指定时间内持续减少当前血量。
    /// healthAmount 表示这段时间内总共减少的血量。
    /// 例如：time 为 5，healthAmount 为 20，表示 5 秒内合计减少 20 点生命值。
    /// </summary>
    public void ReduceHealthOverTime(float time, int healthAmount)
    {
        if (time <= 0f || healthAmount <= 0 || isDead)
        {
            return;
        }

        StartCoroutine(ReduceHealthOverTimeCoroutine(time, healthAmount));
    }

    private IEnumerator ReduceHealthOverTimeCoroutine(float time, int healthAmount)
    {
        float elapsedTime = 0f;
        float reducedAmount = 0;

        while (elapsedTime < time && reducedAmount < healthAmount && !isDead)
        {
            elapsedTime += Time.deltaTime;

            float targetReducedAmount = Mathf.FloorToInt(healthAmount * Mathf.Clamp01(elapsedTime / time));
            float amountThisFrame = targetReducedAmount - reducedAmount;

            if (amountThisFrame > 0)
            {
                ReduceHealth(amountThisFrame);
                reducedAmount += amountThisFrame;
            }

            yield return null;
        }

        float remainingAmount = healthAmount - reducedAmount;

        if (remainingAmount > 0 && !isDead)
        {
            ReduceHealth(remainingAmount);
        }
    }

    /// <summary>
    /// 在指定时间内持续增加当前血量。
    /// healthAmount 表示这段时间内总共增加的血量。
    /// 回血不会超过当前血上限。
    /// 例如：time 为 5，healthAmount 为 20，表示 5 秒内合计恢复 20 点生命值。
    /// </summary>
    public void AddHealthOverTime(float time, float healthAmount)
    {
        if (time <= 0f || healthAmount <= 0 || isDead)
        {
            return;
        }

        StartCoroutine(AddHealthOverTimeCoroutine(time, healthAmount));
    }

    private IEnumerator AddHealthOverTimeCoroutine(float time, float healthAmount)
    {
        float elapsedTime = 0f;
        float addedAmount = 0;

        while (elapsedTime < time && addedAmount < healthAmount && !isDead)
        {
            elapsedTime += Time.deltaTime;

            float targetAddedAmount = Mathf.FloorToInt(healthAmount * Mathf.Clamp01(elapsedTime / time));
            float amountThisFrame = targetAddedAmount - addedAmount;

            if (amountThisFrame > 0)
            {
                AddHealth(amountThisFrame);
                addedAmount += amountThisFrame;
            }

            yield return null;
        }

        float remainingAmount = healthAmount - addedAmount;

        if (remainingAmount > 0 && !isDead)
        {
            AddHealth(remainingAmount);
        }
    }

    public void AddHungerIncreaseAmount(float amount)
    {
        hungerIncreaseAmount += amount;
    }

    public void ReduceHungerIncreaseAmount(float amount)
    {
        hungerIncreaseAmount -= amount;
        if(hungerIncreaseAmount < 0)
        {
            hungerIncreaseAmount = 0;
        }
    }

    /// <summary>
    /// 直接设置饥饿度。
    /// </summary>
    public void SetHunger(float value)
    {
        float targetHunger = Mathf.Clamp(value, 0, baseMaxHealth);
        float delta = targetHunger - currentHunger;

        if (delta == 0)
        {
            return;
        }

        ChangeHunger(delta);
    }


    private void ChangeHunger(float delta)
    {
        float oldHunger = currentHunger;
        float oldMaxHealth = CurrentMaxHealth;
        float oldHealth = currentHealth;

        currentHunger = Mathf.Clamp(currentHunger + delta, 0, baseMaxHealth);

        float newMaxHealth = CurrentMaxHealth;

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

    public void KillPlayer()
    {
        ReduceHealth(currentHealth);
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

        if (GameStateManager.Instance.currentGameState != GameState.Game)
        {
            return;
        }

        isDead = true;


        StopAutoIncreaseHunger();
        SaveManager.Instance.GetRunTimeGameData().playerDiePosition = transform.position;
        playerInventory.SaveCurrentItemsToRetrieveInventoryAndClearSelf(); // 记录遗失物品
        SaveManager.Instance.SaveGame();
        PlayerDied?.Invoke();
        DeathEffect();
    }

    private void DeathEffect()
    {
        Instantiate(playerBackpackSprite, transform.position, Quaternion.identity);
        GlobalUI.Instance.fadeScreenUI.PlayPlayerDeathFade(() =>
        {
            SceneSwitchManager.Instance.SwitchToScene(SceneType.Base);
        });
        Destroy(gameObject);
    }


}