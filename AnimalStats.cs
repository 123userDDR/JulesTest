using UnityEngine;

/// <summary>
/// ScriptableObject для хранения основных характеристик животного.
/// Позволяет создавать разные типы животных с уникальными параметрами.
/// </summary>
[CreateAssetMenu(fileName = "NewAnimalStats", menuName = "Animal/Animal Stats", order = 1)]
public class AnimalStats : ScriptableObject
{
    [Header("General")]
    public string animalName = "Animal";

    [Header("Health & Vitals")]
    [Tooltip("Максимальное здоровье")]
    public float maxHealth = 100f;
    [Tooltip("Может ли здоровье регенерировать")]
    public bool canRegenerate = true;
    [Tooltip("Скорость регенерации (HP в секунду)")]
    public float regenerationRate = 1f;

    [Header("Movement")]
    [Tooltip("Скорость ходьбы")]
    public float walkSpeed = 2f;
    [Tooltip("Скорость бега")]
    public float runSpeed = 5f;
    [Tooltip("Скорость спринта (при бегстве)")]
    public float sprintSpeed = 8f;
    [Tooltip("Скорость поворота (градусов в секунду)")]
    public float rotationSpeed = 180f;
    [Tooltip("Максимальное расстояние от 'домашней' точки")]
    public float maxDistanceFromHome = 50f;

    [Header("Detection")]
    [Tooltip("Радиус обнаружения травы")]
    public float grassDetectionRange = 5f;
    [Tooltip("Радиус обнаружения угроз")]
    public float threatDetectionRange = 15f;
    [Tooltip("Радиус обнаружения игрока")]
    public float playerDetectionRange = 10f;
    [Tooltip("Угол обзора для обнаружения угроз")]
    [Range(0, 360)]
    public float threatDetectionAngle = 270f;

    [Header("Audio")]
    [Tooltip("Общая громкость звуков животного")]
    [Range(0, 1)]
    public float audioVolume = 1f;
    [Tooltip("Максимальная дистанция слышимости 3D звуков")]
    public float maxHearingDistance = 20f;
}
