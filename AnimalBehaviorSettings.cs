using UnityEngine;

/// <summary>
/// ScriptableObject для хранения настроек поведения животного.
/// Позволяет тонко настраивать логику принятия решений и тайминги.
/// </summary>
[CreateAssetMenu(fileName = "NewAnimalBehavior", menuName = "Animal/Animal Behavior Settings", order = 2)]
public class AnimalBehaviorSettings : ScriptableObject
{
    [Header("Idle State")]
    [Tooltip("Минимальное и максимальное время в состоянии покоя (Idle)")]
    public Vector2 idleDurationRange = new Vector2(3f, 8f);

    [Header("Wandering State")]
    [Tooltip("Минимальное и максимальное время паузы на точке маршрута (waypoint)")]
    public Vector2 waypointPauseDurationRange = new Vector2(1f, 5f);
    [Tooltip("Радиус, в котором точка считается достигнутой")]
    public float waypointReachedRadius = 1.5f;

    [Header("Eating State")]
    [Tooltip("Минимальное и максимальное время, которое животное тратит на поедание")]
    public Vector2 eatingDurationRange = new Vector2(5f, 10f);
    [Tooltip("Сколько здоровья восстанавливается за один 'сеанс' еды")]
    public float healthRestoredFromEating = 10f;
    [Tooltip("Вероятность того, что животное пойдет есть при обнаружении травы (0-1)")]
    [Range(0, 1)]
    public float chanceToEat = 0.7f;

    [Header("Fleeing State")]
    [Tooltip("Как долго животное будет бежать после потери угрозы из виду")]
    public float fleeDurationAfterLosingThreat = 3f;
    [Tooltip("Насколько далеко животное пытается убежать от угрозы")]
    public float fleeDistance = 20f;

    [Header("Hurt State")]
    [Tooltip("Как долго животное находится в состоянии боли после получения урона")]
    public float hurtDuration = 1f;

    [Header("General Behavior")]
    [Tooltip("Если здоровье ниже этого процента (0-1), животное будет активнее искать еду")]
    [Range(0, 1)]
    public float lowHealthThreshold = 0.4f;
    [Tooltip("Если животное не видело угрозы дольше этого времени (в секундах), оно успокаивается")]
    public float calmDownTime = 60f;
}
