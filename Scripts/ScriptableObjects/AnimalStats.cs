using UnityEngine;

/// <summary>
/// ScriptableObject для хранения базовых характеристик животного.
/// </summary>
[CreateAssetMenu(fileName = "NewAnimalStats", menuName = "Animal/Animal Stats")]
public class AnimalStats : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Максимальное здоровье животного")]
    public float maxHealth = 100f;

    [Header("Movement")]
    [Tooltip("Скорость ходьбы")]
    public float walkSpeed = 2f;

    [Tooltip("Скорость бега")]
    public float runSpeed = 5f;

    [Tooltip("Скорость спринта")]
    public float sprintSpeed = 8f;

    [Tooltip("Скорость поворота (градусов в секунду)")]
    public float rotationSpeed = 180f;

    [Header("AI Behavior")]
    [Tooltip("Максимальное расстояние от 'домашней' точки, на которое животное может отойти")]
    public float maxDistanceFromHome = 50f;

    [Header("Detection")]
    [Tooltip("Радиус обнаружения еды")]
    public float grassDetectionRadius = 10f;

    [Tooltip("Радиус обнаружения угроз")]
    public float threatDetectionRadius = 15f;
}
