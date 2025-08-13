using UnityEngine;

/// <summary>
/// Основной контроллер животного. Координирует все системы и компоненты.
/// Точка входа для внешних взаимодействий с животным.
/// </summary>
public class AnimalController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AnimalStats animalStats;
    [SerializeField] private Transform[] waypoints;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Компоненты системы
    private AnimalMovement movement;
    private AnimalHealth health;
    private AnimalAnimator animalAnimator;
    private AnimalDetection detection;
    private AnimalAudio audioSystem;
    private LootDropper lootDropper;
    
    // Системы
    private AnimalStateMachine stateMachine;
    private WaypointSystem waypointSystem;
    
    // Публичные свойства для доступа к компонентам
    public AnimalMovement Movement => movement;
    public AnimalHealth Health => health;
    public AnimalAnimator AnimalAnimator => animalAnimator;
    public AnimalDetection Detection => detection;
    public AnimalAudio AudioSystem => audioSystem;
    public AnimalStateMachine StateMachine => stateMachine;
    public WaypointSystem WaypointSystem => waypointSystem;
    public AnimalStats Stats => animalStats;
    
    // События
    public System.Action<AnimalController> OnAnimalInitialized;
    public System.Action<AnimalController> OnAnimalDestroyed;
    
    private void Awake()
    {
        InitializeComponents();
        InitializeSystems();
    }
    
    private void Start()
    {
        if (animalStats == null)
        {
            Debug.LogError($"AnimalStats not assigned to {gameObject.name}!");
            return;
        }
        
        StartBehavior();
        OnAnimalInitialized?.Invoke(this);
    }
    
    private void Update()
    {
        if (health.IsDead) return;
        
        // Обновляем машину состояний
        stateMachine?.Update();
        
        // Debug информация
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }
    
    private void FixedUpdate()
    {
        if (health.IsDead) return;

        // Обновляем физику в машине состояний
        stateMachine?.FixedUpdate();
    }

    private void InitializeComponents()
    {
        // Получаем или добавляем необходимые компоненты
        movement = GetComponent<AnimalMovement>();
        if (movement == null) movement = gameObject.AddComponent<AnimalMovement>();
        
        health = GetComponent<AnimalHealth>();
        if (health == null) health = gameObject.AddComponent<AnimalHealth>();
        
        animalAnimator = GetComponent<AnimalAnimator>();
        if (animalAnimator == null) animalAnimator = gameObject.AddComponent<AnimalAnimator>();
        
        detection = GetComponent<AnimalDetection>();
        if (detection == null) detection = gameObject.AddComponent<AnimalDetection>();
        
        audioSystem = GetComponent<AnimalAudio>();
        if (audioSystem == null) audioSystem = gameObject.AddComponent<AnimalAudio>();
        
        lootDropper = GetComponent<LootDropper>();
        if (lootDropper == null) lootDropper = gameObject.AddComponent<LootDropper>();
    }
    
    private void InitializeSystems()
    {
        // Инициализируем системы
        waypointSystem = new WaypointSystem(waypoints, transform);
        stateMachine = new AnimalStateMachine(this);
        
        // Подписываемся на события
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDeath += HandleDeath;
            health.OnDamageTaken += HandleDamage;
        }
        
        if (detection != null)
        {
            detection.OnGrassDetected += HandleGrassDetected;
            detection.OnThreatDetected += HandleThreatDetected;
            detection.OnThreatLost += HandleThreatLost;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath -= HandleDeath;
            health.OnDamageTaken -= HandleDamage;
        }
        
        if (detection != null)
        {
            detection.OnGrassDetected -= HandleGrassDetected;
            detection.OnThreatDetected -= HandleThreatDetected;
            detection.OnThreatLost -= HandleThreatLost;
        }
    }
    
    private void StartBehavior()
    {
        // Инициализируем компоненты с нашими настройками
        health.Initialize(animalStats.maxHealth);
        movement.Initialize(animalStats);
        animalAnimator.Initialize();
        detection.Initialize(animalStats);
        audioSystem.Initialize(animalStats);
        
        // Запускаем машину состояний
        stateMachine.Start();
    }
    
    #region Event Handlers
    
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // Здесь можно добавить логику реакции на изменение здоровья
        // Например, изменение скорости при низком здоровье
        if (currentHealth < maxHealth * 0.3f)
        {
            // Животное ранено, движется медленнее
            movement.SetHealthMultiplier(0.7f);
        }
        else
        {
            movement.SetHealthMultiplier(1f);
        }
    }
    
    private void HandleDeath()
    {
        stateMachine.TransitionToDead();
        
        if (lootDropper != null)
        {
            lootDropper.DropLoot();
        }
    }
    
    private void HandleDamage(float damage)
    {
        stateMachine.TransitionToHurt();
    }
    
    private void HandleGrassDetected(GameObject grass)
    {
        stateMachine.TransitionToEating(grass);
    }
    
    private void HandleThreatDetected(GameObject threat)
    {
        stateMachine.TransitionToFleeing(threat);
    }
    
    private void HandleThreatLost()
    {
        // Возвращаемся к нормальному поведению, если убегали
        if (stateMachine.CurrentState is FleeingState)
        {
            stateMachine.TransitionToIdle();
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Нанести урон животному
    /// </summary>
    public void TakeDamage(float damage)
    {
        health.TakeDamage(damage);
    }
    
    
    /// <summary>
    /// Получить текущее состояние
    /// </summary>
    public string GetCurrentStateName()
    {
        return stateMachine.CurrentState?.GetType().Name ?? "None";
    }
    
    /// <summary>
    /// Проверить, живо ли животное
    /// </summary>
    public bool IsAlive => !health.IsDead;
    
    #endregion
    
    #region Debug
    
    private void DrawDebugInfo()
    {
        if (!showDebugInfo) return;
        
        // Показываем текущее состояние над животным
        Vector3 position = transform.position + Vector3.up * 2f;
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(position, 
            $"State: {GetCurrentStateName()}\n" +
            $"Health: {health.CurrentHealth:F1}/{health.MaxHealth:F1}\n" +
            $"Speed: {movement.CurrentSpeed:F1}");
        #endif
    }
    
    private void OnDrawGizmosSelected()
    {
        // Показываем waypoints
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.blue;
            foreach (var waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawWireSphere(waypoint.position, 0.5f);
                }
            }
            
            // Соединяем waypoints линиями
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }
        }
        
        // Показываем радиус домашней зоны
        if (animalStats != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, animalStats.maxDistanceFromHome);
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        OnAnimalDestroyed?.Invoke(this);
    }
}