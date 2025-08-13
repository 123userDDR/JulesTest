using UnityEngine;

/// <summary>
/// Система движения животного. Управляет перемещением, поворотами и физикой.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AnimalMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float stopDistance = 0.1f;
    
    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayerMask = 1;
    [SerializeField] private float slopeLimit = 45f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = false;
    
    // Компоненты
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private AnimalStats stats;
    
    // Состояние движения
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float healthMultiplier = 1f;
    private bool isGrounded = true;
    private Vector3 groundNormal = Vector3.up;
    
    // События
    public System.Action<float> OnSpeedChanged;
    public System.Action<Vector3> OnMovementStarted;
    public System.Action OnMovementStopped;
    public System.Action OnReachedTarget;
    
    // Публичные свойства
    public float CurrentSpeed => currentSpeed;
    public float TargetSpeed => targetSpeed;
    public bool IsMoving => currentSpeed > 0.01f;
    public bool HasTarget => hasTarget;
    public Vector3 TargetPosition => targetPosition;
    public bool IsGrounded => isGrounded;
    public Vector3 MoveDirection => moveDirection;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        // Настраиваем Rigidbody
        rb.freezeRotation = true; // Предотвращаем кувырки
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }
    
    private void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
        HandleRotation();
    }
    
    /// <summary>
    /// Инициализация компонента с настройками
    /// </summary>
    public void Initialize(AnimalStats animalStats)
    {
        stats = animalStats;
        
        if (stats != null)
        {
            // Применяем настройки из ScriptableObject
            rotationSpeed = stats.rotationSpeed;
        }
    }
    
    #region Public Movement Methods
    
    /// <summary>
    /// Начать движение к точке
    /// </summary>
    public void MoveToPosition(Vector3 target)
    {
        targetPosition = target;
        hasTarget = true;
        
        Vector3 direction = (target - transform.position).normalized;
        moveDirection = new Vector3(direction.x, 0, direction.z).normalized;
        
        OnMovementStarted?.Invoke(target);
    }
    
    /// <summary>
    /// Движение в направлении
    /// </summary>
    public void MoveInDirection(Vector3 direction)
    {
        moveDirection = new Vector3(direction.x, 0, direction.z).normalized;
        hasTarget = false;
        
        if (direction != Vector3.zero)
        {
            OnMovementStarted?.Invoke(transform.position + direction * 10f);
        }
    }
    
    /// <summary>
    /// Остановить движение
    /// </summary>
    public void StopMovement()
    {
        hasTarget = false;
        moveDirection = Vector3.zero;
        targetSpeed = 0f;
        
        OnMovementStopped?.Invoke();
    }
    
    /// <summary>
    /// Установить целевую скорость
    /// </summary>
    public void SetTargetSpeed(float speed)
    {
        targetSpeed = Mathf.Clamp(speed, 0f, GetMaxSpeed()) * healthMultiplier;
    }
    
    /// <summary>
    /// Установить скорость ходьбы
    /// </summary>
    public void SetWalkSpeed()
    {
        SetTargetSpeed(stats != null ? stats.walkSpeed : 2f);
    }
    
    /// <summary>
    /// Установить скорость бега
    /// </summary>
    public void SetRunSpeed()
    {
        SetTargetSpeed(stats != null ? stats.runSpeed : 5f);
    }
    
    /// <summary>
    /// Установить максимальную скорость (спринт)
    /// </summary>
    public void SetSprintSpeed()
    {
        SetTargetSpeed(stats != null ? stats.sprintSpeed : 8f);
    }
    
    /// <summary>
    /// Установить множитель скорости от здоровья
    /// </summary>
    public void SetHealthMultiplier(float multiplier)
    {
        healthMultiplier = Mathf.Clamp01(multiplier);
    }
    
    /// <summary>
    /// Проверить, достигли ли цели
    /// </summary>
    public bool HasReachedTarget()
    {
        if (!hasTarget) return false;
        
        float distance = Vector3.Distance(transform.position, targetPosition);
        return distance <= stopDistance;
    }
    
    /// <summary>
    /// Получить расстояние до цели
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (!hasTarget) return 0f;
        return Vector3.Distance(transform.position, targetPosition);
    }
    
    #endregion
    
    #region Movement Logic
    
    private void HandleMovement()
    {
        if (!isGrounded) return;
        
        // Проверяем достижение цели
        if (hasTarget && HasReachedTarget())
        {
            StopMovement();
            OnReachedTarget?.Invoke();
            return;
        }
        
        // Плавное изменение скорости
        float acceleration = targetSpeed > currentSpeed ? this.acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        
        // Применяем движение
        if (currentSpeed > 0.01f && moveDirection != Vector3.zero)
        {
            // Учитываем наклон поверхности
            Vector3 moveVector = GetSlopeAdjustedMovement(moveDirection * currentSpeed);
            
            // Применяем движение через Rigidbody
            rb.MovePosition(transform.position + moveVector * Time.fixedDeltaTime);
        }
        
        // Уведомляем о изменении скорости
        OnSpeedChanged?.Invoke(currentSpeed);
    }
    
    private void HandleRotation()
    {
        if (moveDirection == Vector3.zero || !isGrounded) return;
        
        // Целевое направление поворота
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        
        // Плавный поворот
        float rotationStep = rotationSpeed * Time.fixedDeltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
    }
    
    private Vector3 GetSlopeAdjustedMovement(Vector3 movement)
    {
        // Проецируем движение на поверхность
        Vector3 slopeMovement = Vector3.ProjectOnPlane(movement, groundNormal).normalized * movement.magnitude;
        
        // Проверяем угол наклона
        float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
        if (slopeAngle > slopeLimit)
        {
            // Слишком крутой склон, не можем двигаться
            return Vector3.zero;
        }
        
        return slopeMovement;
    }
    
    #endregion
    
    #region Ground Detection
    
    private void CheckGrounded()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(rayOrigin, Vector3.down);
        
        if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance, groundLayerMask))
        {
            isGrounded = true;
            groundNormal = hit.normal;
            
            // Притягиваем к земле если слишком высоко
            float distanceToGround = hit.distance - 0.1f;
            if (distanceToGround > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, 
                    hit.point + Vector3.up * (capsuleCollider.height * 0.5f), 
                    Time.fixedDeltaTime * 5f);
            }
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
        
        if (showDebugRays)
        {
            Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, 
                isGrounded ? Color.green : Color.red);
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private float GetMaxSpeed()
    {
        if (stats == null) return 5f;
        return Mathf.Max(stats.walkSpeed, stats.runSpeed, stats.sprintSpeed);
    }
    
    /// <summary>
    /// Получить нормализованную скорость (0-1)
    /// </summary>
    public float GetNormalizedSpeed()
    {
        float maxSpeed = GetMaxSpeed();
        return maxSpeed > 0 ? currentSpeed / maxSpeed : 0f;
    }
    
    /// <summary>
    /// Получить тип скорости для аниматора
    /// </summary>
    public float GetAnimatorSpeed()
    {
        if (stats == null) return GetNormalizedSpeed() * 3f;
        
        if (currentSpeed <= 0.1f) return 0f; // Idle
        if (currentSpeed <= stats.walkSpeed + 0.5f) return 1f; // Walk
        if (currentSpeed <= stats.runSpeed + 0.5f) return 2f; // Run
        return 3f; // Sprint
    }
    
    /// <summary>
    /// Проверить, может ли животное двигаться в направлении
    /// </summary>
    public bool CanMoveToDirection(Vector3 direction, float distance = 1f)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * (capsuleCollider.height * 0.5f);
        Vector3 rayDirection = direction.normalized;
        
        // Проверяем препятствия
        if (Physics.Raycast(rayOrigin, rayDirection, distance, ~groundLayerMask))
        {
            return false;
        }
        
        // Проверяем, есть ли земля в направлении движения
        Vector3 groundCheckPos = transform.position + rayDirection * distance;
        Ray groundRay = new Ray(groundCheckPos + Vector3.up, Vector3.down);
        
        return Physics.Raycast(groundRay, groundCheckDistance * 2f, groundLayerMask);
    }
    
    /// <summary>
    /// Толкнуть животное (например, при ударе)
    /// </summary>
    public void ApplyKnockback(Vector3 force)
    {
        if (rb != null)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Показываем текущую цель
        if (hasTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, stopDistance);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        // Показываем направление движения
        if (moveDirection != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }
        
        // Показываем зону обнаружения земли
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(rayOrigin, Vector3.down * groundCheckDistance);
        
        // Показываем нормаль поверхности
        if (isGrounded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, groundNormal * 2f);
        }
    }
    
    #endregion
}