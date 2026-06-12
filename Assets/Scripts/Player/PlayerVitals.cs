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

    [Header("防御设置")]
    [SerializeField, Min(0)]
    private int startDefense = 0;

    [Header("饥饿设置")]
    [SerializeField, Min(0)]
    private int startHunger = 0;

    [SerializeField]
    private bool autoIncreaseHunger = true;

    [SerializeField, Min(0.01f)]
    private float hungerIncreaseInterval = 15f;

    [SerializeField, Min(0)]
    private int hungerIncreaseAmount = 1;

    [Header("死亡设置")]
    [SerializeField] private SpriteRenderer playerBackpackSprite;


    public event Action<int> CurrentHealthChanged;

    public event Action<int> MaxHealthChanged;

    public event Action<int> HungerChanged;


    public event Action PlayerDied;

    private int currentHealth;
    private int currentHunger;
    private int currentDefense;
    private bool isDead;
    private Coroutine hungerCoroutine;
    private Coroutine pauseAutoIncreaseHungerCoroutine;
    private bool isAutoIncreaseHungerPaused;
    private InventoryPlayer playerInventory;

    public int BaseMaxHealth => baseMaxHealth;

    public int CurrentHealth => currentHealth;

    public int CurrentHunger => currentHunger;

    public int CurrentDefense => currentDefense;

    public int CurrentMaxHealth => Mathf.Max(0, baseMaxHealth - currentHunger);

    public bool IsDead => isDead;

    private bool hasStartedAutoIncreaseHunger = false;

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

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0 || isDead)
        {
            return;
        }

        int actualDamageAmount = Mathf.Max(0, damageAmount - currentDefense);

        ReduceHealth(actualDamageAmount);
    }

    /// <summary>
    /// 增加防御力。
    /// 防御力会减少 TakeDamage 中实际受到的伤害。
    /// </summary>
    public void AddDefense(int amount)
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
    public void ReduceDefense(int amount)
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
    public void AddDefenseTemporarily(int amount, float time)
    {
        if (amount <= 0 || time <= 0f || isDead)
        {
            return;
        }

        StartCoroutine(AddDefenseTemporarilyCoroutine(amount, time));
    }

    private IEnumerator AddDefenseTemporarilyCoroutine(int amount, float time)
    {
        AddDefense(amount);

        yield return new WaitForSeconds(time);

        ReduceDefense(amount);
    }

    /// <summary>
    /// 临时减少防御力。
    /// 指定时间结束后，会恢复本次实际减少的防御力。
    /// </summary>
    public void ReduceDefenseTemporarily(int amount, float time)
    {
        if (amount <= 0 || time <= 0f || isDead)
        {
            return;
        }

        StartCoroutine(ReduceDefenseTemporarilyCoroutine(amount, time));
    }

    private IEnumerator ReduceDefenseTemporarilyCoroutine(int amount, float time)
    {
        int defenseBeforeReduce = currentDefense;

        ReduceDefense(amount);

        int actualReducedAmount = defenseBeforeReduce - currentDefense;

        yield return new WaitForSeconds(time);

        AddDefense(actualReducedAmount);
    }

    /// <summary>
    /// 直接设置防御力。
    /// 防御力不会低于 0。
    /// </summary>
    public void SetDefense(int value)
    {
        int oldDefense = currentDefense;

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