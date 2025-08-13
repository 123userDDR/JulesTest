using UnityEngine;

/// <summary>
/// Система управления анимациями животного. Связывает состояния и движение с Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimalAnimator : MonoBehaviour
{
    [Header("Animator Settings")]
    [SerializeField] private float speedSmoothTime = 0.1f;
    [SerializeField] private float turnSmoothTime = 0.2f;
    [SerializeField] private bool useRootMotion = false;
    
    [Header("Animation Overrides")]
    [SerializeField] private bool overrideTransitions = false;
    [SerializeField] private float customTransitionDuration = 0.25f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Компоненты
    private Animator animator;
    private AnimalMovement movement;
    private AnimalHealth health;
    
    // Параметры аниматора (хеш ID для производительности)
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsEatingParam = Animator.StringToHash("IsEating");
    private static readonly int IsDeadParam = Animator.StringToHash("IsDead");
    private static readonly int IsHurtParam = Animator.StringToHash("IsHurt");
    private static readonly int TurnAngleParam = Animator.StringToHash("TurnAngle");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
    private static readonly int HealthPercentParam = Animator.StringToHash("HealthPercent");
    
    // Дополнительные триггеры
    private static readonly int HurtTrigger = Animator.StringToHash("HurtTrigger");
    private static readonly int DeathTrigger = Animator.StringToHash("DeathTrigger");
    private static readonly int EatTrigger = Animator.StringToHash("EatTrigger");
    private static readonly int IdleTrigger = Animator.StringToHash("IdleTrigger");
    
    // Текущие значения для сглаживания
    private float currentSpeedValue = 0f;
    private float currentTurnValue = 0f;
    private float speedVelocity = 0f;
    private float turnVelocity = 0f;
    
    // Состояние анимаций
    private bool isEating = false;
    private bool isDead = false;
    private bool isHurt = false;
    private float lastHurtTime = 0f;
    private float hurtAnimationDuration = 1f;
    
    // События
    public System.Action<string> OnAnimationStateChanged;
    public System.Action<string> OnAnimationEvent; // Для Animation Events
    
    // Публичные свойства
    public Animator Animator => animator;
    public float CurrentAnimationSpeed => currentSpeedValue;
    public bool IsEating => isEating;
    public bool IsDead => isDead;
    public bool IsInHurtAnimation => isHurt;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<AnimalMovement>();
        health = GetComponent<AnimalHealth>();
        
        // Настраиваем аниматор
        if (animator != null)
        {
            animator.applyRootMotion = useRootMotion;
        }
    }
    
    private void Update()
    {
        if (animator == null) return;
        
        UpdateAnimationParameters();
        HandleHurtAnimation();
        
        if (showDebugInfo)
        {
            DisplayDebugInfo();
        }
    }
    
    /// <summary>
    /// Инициализация компонента
    /// </summary>
    public void Initialize()
    {
        if (animator == null)
        {
            Debug.LogError($"Animator not found on {gameObject.name}!");
            return;
        }
        
        // Подписываемся на события других компонентов
        SubscribeToEvents();
        
        // Устанавливаем начальные значения
        SetAnimatorParameter(SpeedParam, 0f);
        SetAnimatorParameter(IsEatingParam, false);
        SetAnimatorParameter(IsDeadParam, false);
        SetAnimatorParameter(IsHurtParam, false);
        SetAnimatorParameter(IsGroundedParam, true);
        SetAnimatorParameter(HealthPercentParam, 1f);
        
        Debug.Log($"AnimalAnimator initialized for {gameObject.name}");
    }
    
    #region Animation Parameter Updates
    
    private void UpdateAnimationParameters()
    {
        UpdateSpeedParameter();
        UpdateTurnParameter();
        UpdateGroundedParameter();
        UpdateHealthParameter();
    }
    
    private void UpdateSpeedParameter()
    {
        if (movement == null) return;
        
        // Получаем целевую скорость для аниматора
        float targetSpeed = movement.GetAnimatorSpeed();
        
        // Сглаживаем изменение скорости
        currentSpeedValue = Mathf.SmoothDamp(currentSpeedValue, targetSpeed, 
            ref speedVelocity, speedSmoothTime);
        
        SetAnimatorParameter(SpeedParam, currentSpeedValue);
    }
    
    private void UpdateTurnParameter()
    {
        if (movement == null) return;
        
        // Вычисляем угол поворота
        Vector3 moveDirection = movement.MoveDirection;
        float targetTurnValue = 0f;
        
        if (moveDirection != Vector3.zero)
        {
            Vector3 forward = transform.forward;
            float angle = Vector3.SignedAngle(forward, moveDirection, Vector3.up);
            targetTurnValue = Mathf.Clamp(angle / 90f, -1f, 1f);
        }
        
        // Сглаживаем поворот
        currentTurnValue = Mathf.SmoothDamp(currentTurnValue, targetTurnValue, 
            ref turnVelocity, turnSmoothTime);
        
        SetAnimatorParameter(TurnAngleParam, currentTurnValue);
    }
    
    private void UpdateGroundedParameter()
    {
        if (movement == null) return;
        
        SetAnimatorParameter(IsGroundedParam, movement.IsGrounded);
    }
    
    private void UpdateHealthParameter()
    {
        if (health == null) return;
        
        SetAnimatorParameter(HealthPercentParam, health.HealthPercentage);
    }
    
    #endregion
    
    #region State Animations
    
    /// <summary>
    /// Запустить анимацию еды
    /// </summary>
    public void StartEating()
    {
        if (isDead) return;
        
        isEating = true;
        SetAnimatorParameter(IsEatingParam, true);
        SetAnimatorTrigger(EatTrigger);
        
        OnAnimationStateChanged?.Invoke("Eating");
    }
    
    /// <summary>
    /// Остановить анимацию еды
    /// </summary>
    public void StopEating()
    {
        isEating = false;
        SetAnimatorParameter(IsEatingParam, false);
        
        OnAnimationStateChanged?.Invoke("StoppedEating");
    }
    
    /// <summary>
    /// Запустить анимацию получения урона
    /// </summary>
    public void PlayHurtAnimation()
    {
        if (isDead) return;
        
        isHurt = true;
        lastHurtTime = Time.time;
        
        SetAnimatorParameter(IsHurtParam, true);
        SetAnimatorTrigger(HurtTrigger);
        
        OnAnimationStateChanged?.Invoke("Hurt");
    }
    
    /// <summary>
    /// Запустить анимацию смерти
    /// </summary>
    public void PlayDeathAnimation()
    {
        isDead = true;
        isEating = false;
        isHurt = false;
        
        SetAnimatorParameter(IsDeadParam, true);
        SetAnimatorParameter(IsEatingParam, false);
        SetAnimatorParameter(IsHurtParam, false);
        SetAnimatorTrigger(DeathTrigger);
        
        OnAnimationStateChanged?.Invoke("Death");
    }
    
    /// <summary>
    /// Вернуться к нормальному состоянию
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (isDead) return;
        
        isEating = false;
        SetAnimatorParameter(IsEatingParam, false);
        SetAnimatorTrigger(IdleTrigger);
        
        OnAnimationStateChanged?.Invoke("Idle");
    }
    
    #endregion
    
    #region Animation Events
    
    /// <summary>
    /// Вызывается из Animation Events
    /// </summary>
    public void OnEatBite()
    {
        OnAnimationEvent?.Invoke("EatBite");
    }
    
    /// <summary>
    /// Вызывается из Animation Events
    /// </summary>
    public void OnFootstep()
    {
        OnAnimationEvent?.Invoke("Footstep");
    }
    
    /// <summary>
    /// Вызывается из Animation Events
    /// </summary>
    public void OnHurtComplete()
    {
        isHurt = false;
        SetAnimatorParameter(IsHurtParam, false);
        OnAnimationEvent?.Invoke("HurtComplete");
    }
    
    /// <summary>
    /// Вызывается из Animation Events
    /// </summary>
    public void OnDeathComplete()
    {
        OnAnimationEvent?.Invoke("DeathComplete");
    }
    
    /// <summary>
    /// Общий метод для Animation Events
    /// </summary>
    public void OnAnimationEventReceived(string eventName)
    {
        OnAnimationEvent?.Invoke(eventName);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void SubscribeToEvents()
    {
        if (health != null)
        {
            health.OnDamageTaken += HandleDamageTaken;
            health.OnDeath += HandleDeath;
            health.OnRevived += HandleRevived;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (health != null)
        {
            health.OnDamageTaken -= HandleDamageTaken;
            health.OnDeath -= HandleDeath;
            health.OnRevived -= HandleRevived;
        }
    }
    
    private void HandleDamageTaken(float damage)
    {
        PlayHurtAnimation();
    }
    
    private void HandleDeath()
    {
        PlayDeathAnimation();
    }
    
    private void HandleRevived()
    {
        isDead = false;
        SetAnimatorParameter(IsDeadParam, false);
        PlayIdleAnimation();
        OnAnimationStateChanged?.Invoke("Revived");
    }
    
    #endregion
    
    #region Hurt Animation Management
    
    private void HandleHurtAnimation()
    {
        if (isHurt && Time.time - lastHurtTime >= hurtAnimationDuration)
        {
            isHurt = false;
            SetAnimatorParameter(IsHurtParam, false);
        }
    }
    
    #endregion
    
    #region Animation Control
    
    /// <summary>
    /// Установить скорость воспроизведения аниматора
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = Mathf.Max(0f, speed);
        }
    }
    
    /// <summary>
    /// Получить информацию о текущем состоянии анимации
    /// </summary>
    public AnimatorStateInfo GetCurrentStateInfo(int layerIndex = 0)
    {
        if (animator != null)
        {
            return animator.GetCurrentAnimatorStateInfo(layerIndex);
        }
        return default(AnimatorStateInfo);
    }
    
    /// <summary>
    /// Проверить, проигрывается ли определенная анимация
    /// </summary>
    public bool IsPlayingAnimation(string animationName, int layerIndex = 0)
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        return stateInfo.IsName(animationName);
    }
    
    /// <summary>
    /// Принудительно перейти к состоянию
    /// </summary>
    public void CrossFadeToState(string stateName, float transitionDuration = 0.25f, int layerIndex = 0)
    {
        if (animator != null)
        {
            animator.CrossFade(stateName, transitionDuration, layerIndex);
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private void SetAnimatorParameter(int parameterHash, float value)
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat(parameterHash, value);
        }
    }
    
    private void SetAnimatorParameter(int parameterHash, bool value)
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetBool(parameterHash, value);
        }
    }
    
    private void SetAnimatorTrigger(int triggerHash)
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger(triggerHash);
        }
    }
    
    /// <summary>
    /// Проверить, существует ли параметр в аниматоре
    /// </summary>
    public bool HasParameter(string parameterName)
    {
        if (animator == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == parameterName)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Сбросить все триггеры
    /// </summary>
    public void ResetAllTriggers()
    {
        if (animator == null) return;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(param.name);
            }
        }
    }
    
    #endregion
    
    #region Debug
    
    private void DisplayDebugInfo()
    {
        if (!showDebugInfo) return;
        
        Vector3 position = transform.position + Vector3.up * 3f;
        
        #if UNITY_EDITOR
        string debugText = $"Speed: {currentSpeedValue:F2}\n" +
                          $"Turn: {currentTurnValue:F2}\n" +
                          $"Eating: {isEating}\n" +
                          $"Hurt: {isHurt}\n" +
                          $"Dead: {isDead}";
        
        if (animator != null && animator.enabled)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            debugText += $"\nState: {currentState.shortNameHash}";
        }
        
        UnityEditor.Handles.Label(position, debugText);
        #endif
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !showDebugInfo) return;
        
        // Показываем состояние анимации цветом
        Color stateColor = Color.white;
        if (isDead) stateColor = Color.black;
        else if (isHurt) stateColor = Color.red;
        else if (isEating) stateColor = Color.green;
        else if (currentSpeedValue > 0.1f) stateColor = Color.blue;
        
        Gizmos.color = stateColor;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 3.5f, Vector3.one * 0.3f);
        
        // Показываем направление поворота
        if (Mathf.Abs(currentTurnValue) > 0.1f)
        {
            Vector3 turnDirection = transform.right * currentTurnValue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up, turnDirection * 2f);
        }
    }
    
    /// <summary>
    /// Получить список всех параметров аниматора для отладки
    /// </summary>
    [ContextMenu("Log Animator Parameters")]
    public void LogAnimatorParameters()
    {
        if (animator == null)
        {
            Debug.Log("No Animator found!");
            return;
        }
        
        Debug.Log($"Animator parameters for {gameObject.name}:");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            string value = "";
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    value = animator.GetFloat(param.name).ToString("F2");
                    break;
                case AnimatorControllerParameterType.Bool:
                    value = animator.GetBool(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Int:
                    value = animator.GetInteger(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Trigger:
                    value = "Trigger";
                    break;
            }
            Debug.Log($"  {param.name} ({param.type}): {value}");
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}