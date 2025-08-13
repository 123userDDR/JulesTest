using UnityEngine;

/// <summary>
/// Управляет системой точек маршрута (waypoints) для животного.
/// </summary>
public class WaypointSystem
{
    private Transform[] waypoints;
    private Transform animalTransform;

    public WaypointSystem(Transform[] points, Transform owner)
    {
        waypoints = points;
        animalTransform = owner;
    }

    /// <summary>
    /// Возвращает случайную точку маршрута.
    /// </summary>
    public Vector3 GetRandomWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            // Если нет waypoints, возвращаем позицию рядом с животным
            return animalTransform.position + Random.insideUnitSphere * 5f;
        }

        int randomIndex = Random.Range(0, waypoints.Length);
        return waypoints[randomIndex].position;
    }

    /// <summary>
    /// Проверяет, есть ли у системы валидные точки маршрута.
    /// </summary>
    public bool HasWaypoints()
    {
        return waypoints != null && waypoints.Length > 0;
    }
}
