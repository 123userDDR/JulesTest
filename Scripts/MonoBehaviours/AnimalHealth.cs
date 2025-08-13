using UnityEngine;

/// <summary>
/// Система здоровья животного. Управляет HP, уроном, смертью и регенерацией.
/// </summary>
public class AnimalHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool canRegenerate = true;
    [SerializeField] private float regenerationRate = 1f; // HP в секунду
    [SerializeField] private float regenerationDelay = 5f; // Задержка после урона
    
    [Header("Damage Settings")]
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Color damageColor = Color.red;
    
    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 2f; // Время до начала разложения
    [SerializeField] private float fadeOutDuration = 5f; // Время исчезновения тела
    [SerializeField] private bool destroyOnDeath = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private Transform effectSpawnPoint;
    
    // Состояние здоровья
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float lastDamageTime = 0f;
    private float regenerationTimer = 0f;
    private bool isRegenerating = false;
    
    // Компоненты для эффектов при смерти
    private Renderer[] renderers;
    private Collider[] colliders;
    private float deathTimer = 0f;
    private bool deathProcessStarted = false;
    
    // События
    public System.Action<float, float> OnHealthChanged; // (current, max)
    public System.Action<float> OnDamageTaken; // (damage amount)
    public System.Action<float> OnHealed; // (heal amount)
    public System.Action OnDeath;
    public System.Action OnRevived;
    public System.Action<float> OnHealthPercentageChanged; // (percentage 0-1)
    
    // Публичные свойства
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsFullHealth => currentHealth >= maxHealth;
    public bool IsCriticalHealth => HealthPercentage <= 0.25f;
    public bool IsLowHealth => HealthPercentage <= 0.5f;
    
    private void Awake()
    {
        // Получаем компоненты для эффектов при смерти
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        
        // Устанавливаем точку спавна эффектов если не задана
        if (effectSpawnPoint == null)
            effectSpawnPoint = transform;
    }
    
    private void Start()
    {
        // Инициализируем здоровье если не инициализировано
        if (currentHealth <= 0 && !isDead)
        {
            Initialize(maxHealth);
        }
    }
    
    private void Update()
    {
        if (isDead)
        {
            HandleDeathProcess();
            return;
        }
        
        HandleRegeneration();
        HandleInvulnerability();
    }
    
    /// <summary>
    /// Инициализация системы здоровья
    /// </summary>
    public void Initialize(float startingHealth)
    {
        maxHealth = startingHealth;
        currentHealth = startingHealth;
        isDead = false;
        isInvulnerable = false;
        deathProcessStarted = false;
        deathTimer = 0f;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(HealthPercentage);
    }
    
    #region Damage System
    
    /// <summary>
    /// Нанести урон
    /// </summary>
    public bool TakeDamage(float damage, GameObject damageSource = null)
    {
        if (isDead || isInvulnerable || damage <= 0) return false;
        
        // Применяем урон
        float actualDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Сбрасываем регенерацию
        lastDamageTime = Time.time;
        isRegenerating = false;
        regenerationTimer = 0f;
        
        // Включаем неуязвимость
        if (invulnerabilityDuration > 0)
        {
            StartInvulnerability();
        }
        
        // Визуальные эффекты
        SpawnDamageEffect(actualDamage);
        ShowDamageNumber(actualDamage);
        
        // События
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(HealthPercentage);
        
        // Проверяем смерть
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        
        Debug.Log($"{gameObject.name} took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");
        return true;
    }
    
    /// <summary>
    /// Нанести процентный урон от максимального здоровья
    /// </summary>
    public bool TakePercentageDamage(float percentage, GameObject damageSource = null)
    {
        float damage = maxHealth * Mathf.Clamp01(percentage);
        return TakeDamage(damage, damageSource);
    }
    
    /// <summary>
    /// Мгновенное убийство
    /// </summary>
    public void Kill()
    {
        if (isDead) return;
        
        currentHealth = 0;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(HealthPercentage);
        Die();
    }
    
    #endregion
    
    #region Healing System
    
    /// <summary>
    /// Восстановить здоровье
    /// </summary>
    public bool Heal(float healAmount)
    {
        if (isDead || healAmount <= 0 || IsFullHealth) return false;
        
        float actualHeal = Mathf.Min(healAmount, maxHealth - currentHealth);
        currentHealth += actualHeal;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        // События
        OnHealed?.Invoke(actualHeal);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(HealthPercentage);
        
        Debug.Log($"{gameObject.name} healed for {actualHeal}. Health: {currentHealth}/{maxHealth}");
        return true;
    }
    
    /// <summary>
    /// Восстановить процент от максимального здоровья
    /// </summary>
    public bool HealPercentage(float percentage)
    {
        float healAmount = maxHealth * Mathf.Clamp01(percentage);
        return Heal(healAmount);
    }
    
    /// <summary>
    /// Полное восстановление здоровья
    /// </summary>
    public void FullHeal()
    {
        if (isDead) return;
        Heal(maxHealth);
    }
    
    #endregion
    
    #region Regeneration System
    
    private void HandleRegeneration()
    {
        if (!canRegenerate || isDead || IsFullHealth) return;
        
        // Проверяем задержку после урона
        if (Time.time - lastDamageTime < regenerationDelay)
        {
            isRegenerating = false;
            return;
        }
        
        // Начинаем регенерацию
        if (!isRegenerating)
        {
            isRegenerating = true;
            regenerationTimer = 0f;
        }
        
        // Регенерируем здоровье
        regenerationTimer += Time.deltaTime;
        if (regenerationTimer >= 1f) // Каждую секунду
        {
            Heal(regenerationRate);
            regenerationTimer = 0f;
        }
    }
    
    /// <summary>
    /// Включить/выключить регенерацию
    /// </summary>
    public void SetRegeneration(bool enabled)
    {
        canRegenerate = enabled;
        if (!enabled)
        {
            isRegenerating = false;
        }
    }
    
    #endregion
    
    #region Invulnerability System
    
    private void HandleInvulnerability()
    {
        // Автоматически выключаем неуязвимость через время
        if (isInvulnerable && Time.time - lastDamageTime >= invulnerabilityDuration)
        {
            isInvulnerable = false;
        }
    }
    
    /// <summary>
    /// Включить неуязвимость на определенное время
    /// </summary>
    public void StartInvulnerability(float duration = -1f)
    {
        isInvulnerable = true;
        if (duration > 0)
        {
            invulnerabilityDuration = duration;
        }
        lastDamageTime = Time.time;
    }
    
    /// <summary>
    /// Выключить неуязвимость
    /// </summary>
    public void StopInvulnerability()
    {
        isInvulnerable = false;
    }
    
    #endregion
    
    #region Death System
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        deathProcessStarted = false;
        deathTimer = 0f;
        
        // Отключаем регенерацию
        isRegenerating = false;
        canRegenerate = false;
        
        // Визуальные эффекты смерти
        SpawnDeathEffect();
        
        // Событие смерти
        OnDeath?.Invoke();
        
        Debug.Log($"{gameObject.name} has died.");
    }
    
    private void HandleDeathProcess()
    {
        if (!isDead) return;
        
        deathTimer += Time.deltaTime;
        
        // Начинаем процесс исчезновения через deathDelay
        if (!deathProcessStarted && deathTimer >= deathDelay)
        {
            deathProcessStarted = true;
            StartDeathFade();
        }
        
        // Процесс исчезновения
        if (deathProcessStarted)
        {
            float fadeProgress = (deathTimer - deathDelay) / fadeOutDuration;
            fadeProgress = Mathf.Clamp01(fadeProgress);
            
            // Делаем тело прозрачным
            FadeBody(1f - fadeProgress);
            
            // Уничтожаем объект в конце
            if (fadeProgress >= 1f && destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
    
    private void StartDeathFade()
    {
        // Отключаем коллайдеры чтобы нельзя было взаимодействовать с телом
        foreach (var col in colliders)
        {
            if (col != null && !col.isTrigger)
            {
                col.enabled = false;
            }
        }
    }
    
    private void FadeBody(float alpha)
    {
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
                
                // Для стандартных материалов включаем прозрачность
                if (material.HasProperty("_Mode"))
                {
                    material.SetInt("_Mode", 3); // Transparent mode
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                }
            }
        }
    }
    
    /// <summary>
    /// Воскресить животное
    /// </summary>
    public void Revive(float healthAmount = -1f)
    {
        if (!isDead) return;
        
        isDead = false;
        deathProcessStarted = false;
        deathTimer = 0f;
        canRegenerate = true;
        
        // Восстанавливаем здоровье
        float reviveHealth = healthAmount > 0 ? healthAmount : maxHealth;
        currentHealth = Mathf.Min(reviveHealth, maxHealth);
        
        // Включаем коллайдеры обратно
        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
        
        // Восстанавливаем прозрачность
        FadeBody(1f);
        
        // События
        OnRevived?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(HealthPercentage);
        
        Debug.Log($"{gameObject.name} has been revived with {currentHealth} health.");
    }
    
    #endregion
    
    #region Visual Effects
    
    private void SpawnDamageEffect(float damage)
    {
        if (bloodEffect != null && effectSpawnPoint != null)
        {
            GameObject effect = Instantiate(bloodEffect, effectSpawnPoint.position, effectSpawnPoint.rotation);
            
            // Автоматически уничтожаем эффект
            Destroy(effect, 2f);
        }
    }
    
    private void SpawnDeathEffect()
    {
        if (deathEffect != null && effectSpawnPoint != null)
        {
            GameObject effect = Instantiate(deathEffect, effectSpawnPoint.position, effectSpawnPoint.rotation);
            
            // Автоматически уничтожаем эффект
            Destroy(effect, 5f);
        }
    }
    
    private void ShowDamageNumber(float damage)
    {
        if (!showDamageNumbers) return;
        
        // Здесь можно добавить систему отображения урона как UI текст
        // Пока что просто выводим в консоль
        Debug.Log($"-{damage:F1}", gameObject);
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Установить максимальное здоровье
    /// </summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        float healthRatio = HealthPercentage;
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = maxHealth * healthRatio;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(HealthPercentage);
    }
    
    /// <summary>
    /// Проверить, может ли получить урон от источника
    /// </summary>
    public bool CanTakeDamageFrom(GameObject source)
    {
        if (isDead || isInvulnerable) return false;
        
        // Здесь можно добавить логику для проверки типов урона, иммунитета и т.д.
        return true;
    }
    
    /// <summary>
    /// Получить статус здоровья как строку
    /// </summary>
    public string GetHealthStatus()
    {
        if (isDead) return "Dead";
        if (IsCriticalHealth) return "Critical";
        if (IsLowHealth) return "Injured";
        if (IsFullHealth) return "Healthy";
        return "Good";
    }
    
    #endregion
    
    #region Debug
    
    private void OnValidate()
    {
        // Проверяем корректность значений в инспекторе
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        regenerationRate = Mathf.Max(0f, regenerationRate);
        regenerationDelay = Mathf.Max(0f, regenerationDelay);
        invulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Показываем статус здоровья цветом
        Color healthColor = Color.green;
        if (isDead) healthColor = Color.black;
        else if (IsCriticalHealth) healthColor = Color.red;
        else if (IsLowHealth) healthColor = Color.yellow;
        
        Gizmos.color = healthColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
        
        // Показываем регенерацию
        if (isRegenerating)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2.8f, Vector3.one * 0.2f);
        }
    }
    
    #endregion
}