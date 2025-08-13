using UnityEngine;

/// <summary>
/// ScriptableObject для хранения настроек поведения животного.
/// </summary>
[CreateAssetMenu(fileName = "NewAnimalBehavior", menuName = "Animal/Animal Behavior Settings")]
public class AnimalBehaviorSettings : ScriptableObject
{
    [Header("Wandering")]
    [Tooltip("Минимальное время паузы в состоянии Idle перед следующим блужданием")]
    public float idleTimeMin = 2f;

    [Tooltip("Максимальное время паузы в состоянии Idle перед следующим блужданием")]
    public float idleTimeMax = 5f;

    [Tooltip("Время паузы при достижении точки маршрута в состоянии Wandering")]
    public float wanderPauseTime = 1f;

    [Header("Eating")]
    [Tooltip("Продолжительность поедания травы")]
    public float eatTime = 4f;

    [Tooltip("Шанс (0-1), что животное пойдет есть, обнаружив траву")]
    [Range(0f, 1f)]
    public float chanceToEat = 0.5f;

    [Header("Fleeing")]
    [Tooltip("Время, в течение которого животное будет убегать после потери угрозы")]
    public float fleeDurationAfterLost = 3f;
}
