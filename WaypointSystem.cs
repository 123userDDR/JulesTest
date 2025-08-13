using UnityEngine;

/// <summary>
/// Система для управления точками маршрута (waypoints).
/// Предоставляет логику для выбора следующей точки для патрулирования.
/// </summary>
public class WaypointSystem
{
    private Transform[] waypoints;
    private Transform ownerTransform;
    private int currentWaypointIndex = -1;
    private bool isRandom;

    public Transform CurrentWaypoint { get; private set; }

    /// <summary>
    /// Конструктор системы waypoints.
    /// </summary>
    /// <param name="waypoints">Массив точек для патрулирования.</param>
    /// <param name="ownerTransform">Transform животного, использующего систему.</param>
    /// <param name="isRandom">Выбирать точки случайным образом?</param>
    public WaypointSystem(Transform[] waypoints, Transform ownerTransform, bool isRandom = true)
    {
        this.waypoints = waypoints;
        this.ownerTransform = ownerTransform;
        this.isRandom = isRandom;

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("WaypointSystem initialized with no waypoints.");
        }
        else
        {
            // Начинаем с ближайшей точки
            currentWaypointIndex = GetClosestWaypointIndex();
            CurrentWaypoint = waypoints[currentWaypointIndex];
        }
    }

    /// <summary>
    /// Получить следующую точку маршрута.
    /// </summary>
    public Transform GetNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            // Если нет waypoints, возвращаем случайную точку вокруг
            return GetRandomPointAroundOwner(10f);
        }

        if (isRandom)
        {
            int newIndex = currentWaypointIndex;
            // Убеждаемся, что не выбираем ту же самую точку дважды подряд
            if (waypoints.Length > 1)
            {
                while (newIndex == currentWaypointIndex)
                {
                    newIndex = Random.Range(0, waypoints.Length);
                }
            }
            currentWaypointIndex = newIndex;
        }
        else
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        CurrentWaypoint = waypoints[currentWaypointIndex];
        return CurrentWaypoint;
    }

    /// <summary>
    /// Проверяет, достигнута ли текущая точка.
    /// </summary>
    public bool HasReachedCurrentWaypoint(float reachRadius)
    {
        if (CurrentWaypoint == null) return true; // Если цели нет, считаем, что достигли

        float distance = Vector3.Distance(ownerTransform.position, CurrentWaypoint.position);
        return distance <= reachRadius;
    }

    /// <summary>
    /// Находит индекс ближайшей точки к животному.
    /// </summary>
    private int GetClosestWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return -1;

        int closestIndex = 0;
        float minDistance = Vector3.Distance(ownerTransform.position, waypoints[0].position);

        for (int i = 1; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(ownerTransform.position, waypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    /// <summary>
    /// Генерирует случайную точку в заданном радиусе от животного.
    /// Используется, когда нет заданных waypoints.
    /// </summary>
    private Transform GetRandomPointAroundOwner(float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        Vector3 randomPosition = ownerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Создаем временный Transform для возврата
        GameObject tempWaypoint = new GameObject("TempWaypoint");
        tempWaypoint.transform.position = randomPosition;
        // Этот объект стоит уничтожать после использования
        Object.Destroy(tempWaypoint, 5f);

        return tempWaypoint.transform;
    }
}
