using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Система обнаружения объектов вокруг животного. 
/// Обнаруживает траву, угрозы, других животных и игрока.
/// </summary>
public class AnimalDetection : MonoBehaviour
{
    [Header("Detection Ranges")]
    [SerializeField] private float grassDetectionRange = 3f;
    [SerializeField] private float threatDetectionRange = 8f;
    [SerializeField] private float playerDetectionRange = 5f;
    [SerializeField] private float animalDetectionRange = 4f;
    
    [Header("Detection Settings")]
    [SerializeField] private float detectionInterval = 0.5f; // Как часто сканировать
    [SerializeField] private int maxDetectedObjects = 10;
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayerMask = 1;
    
    [Header("Detection Layers")]
    [SerializeField] private LayerMask grassLayerMask = 1 << 8;
    [SerializeField] private LayerMask threatLayerMask = 1 << 9;
    [SerializeField] private LayerMask playerLayerMask = 1 << 10;
    [SerializeField] private LayerMask animalLayerMask = 1 << 11;
    
    [Header("Detection Tags")]
    [SerializeField] private string grassTag = "Grass";
    [SerializeField] private string threatTag = "Threat";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string animalTag = "Animal";
    
    [Header("Detection Angles")]
    [SerializeField] private float grassDetectionAngle = 360f; // Полный обзор для травы
    [SerializeField] private float threatDetectionAngle = 270f; // Широкий обзор для угроз
    [SerializeField] private float playerDetectionAngle = 180f; // Передний обзор для игрока
    
    [Header("Visual Detection")]
    [SerializeField] private Transform eyePosition; // Точка "глаз" для line of sight
    [SerializeField] private float eyeHeight = 1.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRanges = false;
    [SerializeField] private bool showDetectedObjects = false;
    
    // Обнаруженные объекты
    private List<GameObject> detectedGrass = new List<GameObject>();
    private List<GameObject> detectedThreats = new List<GameObject>();
    private List<GameObject> detectedAnimals = new List<GameObject>();
    private GameObject detectedPlayer = null;
    
    // Система приоритетов
    private GameObject currentTarget = null;
    private DetectionType currentTargetType = DetectionType.None;
    
    // Компоненты и настройки
    private AnimalStats stats;
    private Coroutine detectionCoroutine;
    
    // События
    public System.Action<GameObject> OnGrassDetected;
    public System.Action<GameObject> OnGrassLost;
    public System.Action<GameObject> OnThreatDetected;
    public System.Action OnThreatLost;
    public System.Action<GameObject> OnPlayerDetected;
    public System.Action OnPlayerLost;
    public System.Action<GameObject> OnAnimalDetected;
    public System.Action<GameObject> OnAnimalLost;
    public System.Action<GameObject, DetectionType> OnTargetChanged;
    
    // Публичные свойства
    public List<GameObject> DetectedGrass => new List<GameObject>(detectedGrass);
    public List<GameObject> DetectedThreats => new List<GameObject>(detectedThreats);
    public List<GameObject> DetectedAnimals => new List<GameObject>(detectedAnimals);
    public GameObject DetectedPlayer => detectedPlayer;
    public GameObject CurrentTarget => currentTarget;
    public DetectionType CurrentTargetType => currentTargetType;
    public bool HasGrass => detectedGrass.Count > 0;
    public bool HasThreats => detectedThreats.Count > 0;
    public bool HasPlayer => detectedPlayer != null;
    public bool HasAnimals => detectedAnimals.Count > 0;
    
    public enum DetectionType
    {
        None,
        Grass,
        Threat,
        Player,
        Animal
    }
    
    private void Awake()
    {
        // Устанавливаем позицию глаз если не задана
        if (eyePosition == null)
        {
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = Vector3.up * eyeHeight;
            eyePosition = eyeObj.transform;
        }
    }
    
    private void Start()
    {
        // Запускаем систему обнаружения
        StartDetection();
    }
    
    /// <summary>
    /// Инициализация системы обнаружения
    /// </summary>
    public void Initialize(AnimalStats animalStats)
    {
        stats = animalStats;
        
        if (stats != null)
        {
            // Применяем настройки из ScriptableObject
            grassDetectionRange = stats.grassDetectionRange;
            threatDetectionRange = stats.threatDetectionRange;
            // Другие настройки можно добавить в AnimalStats
        }
    }
    
    #region Detection Control
    
    /// <summary>
    /// Запустить систему обнаружения
    /// </summary>
    public void StartDetection()
    {
        if (detectionCoroutine == null)
        {
            detectionCoroutine = StartCoroutine(DetectionLoop());
        }
    }
    
    /// <summary>
    /// Остановить систему обнаружения
    /// </summary>
    public void StopDetection()
    {
        if (detectionCoroutine != null)
        {
            StopCoroutine(detectionCoroutine);
            detectionCoroutine = null;
        }
    }
    
    /// <summary>
    /// Основной цикл обнаружения
    /// </summary>
    private IEnumerator DetectionLoop()
    {
        while (true)
        {
            PerformDetection();
            UpdateTarget();
            yield return new WaitForSeconds(detectionInterval);
        }
    }
    
    #endregion
    
    #region Detection Logic
    
    private void PerformDetection()
    {
        DetectGrass();
        DetectThreats();
        DetectPlayer();
        DetectAnimals();
        
        CleanupLostObjects();
    }
    
    private void DetectGrass()
    {
        var newGrass = DetectObjectsInRange(grassDetectionRange, grassLayerMask, grassTag, grassDetectionAngle);
        UpdateDetectedList(detectedGrass, newGrass, OnGrassDetected, OnGrassLost, DetectionType.Grass);
    }
    
    private void DetectThreats()
    {
        var newThreats = DetectObjectsInRange(threatDetectionRange, threatLayerMask, threatTag, threatDetectionAngle);
        UpdateDetectedList(detectedThreats, newThreats, OnThreatDetected, OnThreatLost, DetectionType.Threat);
    }
    
    private void DetectPlayer()
    {
        var players = DetectObjectsInRange(playerDetectionRange, playerLayerMask, playerTag, playerDetectionAngle);
        
        GameObject newPlayer = players.Count > 0 ? players[0] : null;
        
        if (detectedPlayer != newPlayer)
        {
            if (detectedPlayer != null)
            {
                OnPlayerLost?.Invoke();
            }
            
            detectedPlayer = newPlayer;
            
            if (detectedPlayer != null)
            {
                OnPlayerDetected?.Invoke(detectedPlayer);
            }
        }
    }
    
    private void DetectAnimals()
    {
        var newAnimals = DetectObjectsInRange(animalDetectionRange, animalLayerMask, animalTag, 360f);
        
        // Исключаем себя из списка
        newAnimals.RemoveAll(animal => animal == gameObject);
        
        UpdateDetectedList(detectedAnimals, newAnimals, OnAnimalDetected, OnAnimalLost, DetectionType.Animal);
    }
    
    private List<GameObject> DetectObjectsInRange(float range, LayerMask layerMask, string tag, float detectionAngle)
    {
        List<GameObject> detectedObjects = new List<GameObject>();
        
        // Получаем все коллайдеры в радиусе
        Collider[] colliders = Physics.OverlapSphere(transform.position, range, layerMask);
        
        foreach (var collider in colliders)
        {
            if (collider.gameObject == gameObject) continue; // Исключаем себя
            
            // Проверяем тег если задан
            if (!string.IsNullOrEmpty(tag) && !collider.CompareTag(tag)) continue;
            
            // Проверяем угол обзора
            if (!IsInDetectionAngle(collider.transform.position, detectionAngle)) continue;
            
            // Проверяем line of sight если включено
            if (useLineOfSight && !HasLineOfSight(collider.transform.position)) continue;
            
            detectedObjects.Add(collider.gameObject);
            
            // Ограничиваем количество объектов
            if (detectedObjects.Count >= maxDetectedObjects) break;
        }
        
        return detectedObjects;
    }
    
    private void UpdateDetectedList(List<GameObject> currentList, List<GameObject> newList, 
        System.Action<GameObject> onDetected, System.Action<GameObject> onLost, DetectionType detectionType)
    {
        // Находим потерянные объекты
        for (int i = currentList.Count - 1; i >= 0; i--)
        {
            if (!newList.Contains(currentList[i]))
            {
                GameObject lostObject = currentList[i];
                currentList.RemoveAt(i);
                onLost?.Invoke(lostObject);
            }
        }
        
        // Находим новые объекты
        foreach (var newObject in newList)
        {
            if (!currentList.Contains(newObject))
            {
                currentList.Add(newObject);
                onDetected?.Invoke(newObject);
            }
        }
    }
    
    private bool IsInDetectionAngle(Vector3 targetPosition, float detectionAngle)
    {
        if (detectionAngle >= 360f) return true;
        
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        
        return angle <= detectionAngle * 0.5f;
    }
    
    private bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector3 rayStart = eyePosition != null ? eyePosition.position : transform.position + Vector3.up * eyeHeight;
        Vector3 rayDirection = targetPosition - rayStart;
        float rayDistance = rayDirection.magnitude;
        
        // Немного поднимаем целевую точку чтобы не упираться в коллайдер цели
        Vector3 targetPoint = targetPosition + Vector3.up * 0.5f;
        rayDirection = (targetPoint - rayStart).normalized;
        rayDistance = Vector3.Distance(rayStart, targetPoint);
        
        return !Physics.Raycast(rayStart, rayDirection, rayDistance - 0.1f, obstacleLayerMask);
    }
    
    private void CleanupLostObjects()
    {
        // Удаляем уничтоженные объекты из списков
        detectedGrass.RemoveAll(obj => obj == null);
        detectedThreats.RemoveAll(obj => obj == null);
        detectedAnimals.RemoveAll(obj => obj == null);
        
        if (detectedPlayer == null)
        {
            detectedPlayer = null;
        }
    }
    
    #endregion
    
    #region Target Management
    
    private void UpdateTarget()
    {
        GameObject newTarget = null;
        DetectionType newTargetType = DetectionType.None;
        
        // Приоритет: Угрозы > Трава > Игрок > Животные
        if (detectedThreats.Count > 0)
        {
            newTarget = GetClosestObject(detectedThreats);
            newTargetType = DetectionType.Threat;
        }
        else if (detectedGrass.Count > 0)
        {
            newTarget = GetClosestObject(detectedGrass);
            newTargetType = DetectionType.Grass;
        }
        else if (detectedPlayer != null)
        {
            newTarget = detectedPlayer;
            newTargetType = DetectionType.Player;
        }
        else if (detectedAnimals.Count > 0)
        {
            newTarget = GetClosestObject(detectedAnimals);
            newTargetType = DetectionType.Animal;
        }
        
        // Обновляем текущую цель
        if (currentTarget != newTarget || currentTargetType != newTargetType)
        {
            currentTarget = newTarget;
            currentTargetType = newTargetType;
            OnTargetChanged?.Invoke(currentTarget, currentTargetType);
        }
    }
    
    private GameObject GetClosestObject(List<GameObject> objects)
    {
        if (objects.Count == 0) return null;
        
        GameObject closest = objects[0];
        float closestDistance = Vector3.Distance(transform.position, closest.transform.position);
        
        for (int i = 1; i < objects.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, objects[i].transform.position);
            if (distance < closestDistance)
            {
                closest = objects[i];
                closestDistance = distance;
            }
        }
        
        return closest;
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Получить ближайшую траву
    /// </summary>
    public GameObject GetNearestGrass()
    {
        return GetClosestObject(detectedGrass);
    }
    
    /// <summary>
    /// Получить ближайшую угрозу
    /// </summary>
    public GameObject GetNearestThreat()
    {
        return GetClosestObject(detectedThreats);
    }
    
    /// <summary>
    /// Получить ближайшее животное
    /// </summary>
    public GameObject GetNearestAnimal()
    {
        return GetClosestObject(detectedAnimals);
    }
    
    /// <summary>
    /// Проверить, находится ли объект в зоне обнаружения
    /// </summary>
    public bool IsObjectInRange(GameObject obj, float range)
    {
        if (obj == null) return false;
        return Vector3.Distance(transform.position, obj.transform.position) <= range;
    }
    
    /// <summary>
    /// Получить расстояние до объекта
    /// </summary>
    public float GetDistanceTo(GameObject obj)
    {
        if (obj == null) return float.MaxValue;
        return Vector3.Distance(transform.position, obj.transform.position);
    }
    
    /// <summary>
    /// Принудительно обновить обнаружение
    /// </summary>
    public void ForceDetectionUpdate()
    {
        PerformDetection();
        UpdateTarget();
    }
    
    /// <summary>
    /// Очистить все обнаруженные объекты
    /// </summary>
    public void ClearAllDetected()
    {
        detectedGrass.Clear();
        detectedThreats.Clear();
        detectedAnimals.Clear();
        detectedPlayer = null;
        currentTarget = null;
        currentTargetType = DetectionType.None;
    }
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugRanges) return;
        
        Vector3 position = transform.position;
        
        // Зоны обнаружения
        Gizmos.color = Color.green;
        DrawDetectionRange(position, grassDetectionRange, grassDetectionAngle);
        
        Gizmos.color = Color.red;
        DrawDetectionRange(position, threatDetectionRange, threatDetectionAngle);
        
        Gizmos.color = Color.blue;
        DrawDetectionRange(position, playerDetectionRange, playerDetectionAngle);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, animalDetectionRange);
        
        // Показываем обнаруженные объекты
        if (showDetectedObjects && Application.isPlaying)
        {
            // Трава
            Gizmos.color = Color.green;
            foreach (var grass in detectedGrass)
            {
                if (grass != null)
                {
                    Gizmos.DrawLine(position, grass.transform.position);
                    Gizmos.DrawWireSphere(grass.transform.position, 0.3f);
                }
            }
            
            // Угрозы
            Gizmos.color = Color.red;
            foreach (var threat in detectedThreats)
            {
                if (threat != null)
                {
                    Gizmos.DrawLine(position, threat.transform.position);
                    Gizmos.DrawWireCube(threat.transform.position, Vector3.one * 0.5f);
                }
            }
            
            // Игрок
            if (detectedPlayer != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(position, detectedPlayer.transform.position);
                Gizmos.DrawWireSphere(detectedPlayer.transform.position, 0.5f);
            }
            
            // Текущая цель
            if (currentTarget != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(currentTarget.transform.position, 1f);
            }
        }
        
        // Позиция глаз для line of sight
        if (eyePosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eyePosition.position, 0.1f);
        }
    }
    
    private void DrawDetectionRange(Vector3 center, float range, float angle)
    {
        if (angle >= 360f)
        {
            Gizmos.DrawWireSphere(center, range);
            return;
        }
        
        // Рисуем сектор
        Vector3 forward = transform.forward;
        Vector3 rightBound = Quaternion.Euler(0, angle * 0.5f, 0) * forward * range;
        Vector3 leftBound = Quaternion.Euler(0, -angle * 0.5f, 0) * forward * range;
        
        Gizmos.DrawLine(center, center + rightBound);
        Gizmos.DrawLine(center, center + leftBound);
        
        // Дуга
        int segments = Mathf.RoundToInt(angle / 10f);
        Vector3 prevPoint = center + rightBound;
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -angle * 0.5f + (angle / segments) * i;
            Vector3 currentPoint = center + Quaternion.Euler(0, currentAngle, 0) * forward * range;
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
    
    /// <summary>
    /// Вывести информацию об обнаруженных объектах в консоль
    /// </summary>
    [ContextMenu("Log Detection Info")]
    public void LogDetectionInfo()
    {
        Debug.Log($"=== Detection Info for {gameObject.name} ===");
        Debug.Log($"Grass: {detectedGrass.Count} objects");
        Debug.Log($"Threats: {detectedThreats.Count} objects");
        Debug.Log($"Animals: {detectedAnimals.Count} objects");
        Debug.Log($"Player: {(detectedPlayer != null ? "Detected" : "Not detected")}");
        Debug.Log($"Current Target: {(currentTarget != null ? currentTarget.name : "None")} ({currentTargetType})");
    }
    
    #endregion
    
    private void OnDestroy()
    {
        StopDetection();
    }
}