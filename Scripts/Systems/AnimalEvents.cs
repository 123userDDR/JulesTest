using System;
using UnityEngine;

/// <summary>
/// Статический класс для хранения глобальных событий, связанных с животными.
/// </summary>
public static class AnimalEvents
{
    /// <summary>
    /// Событие, вызываемое при смерти любого животного.
    /// </summary>
    public static Action<AnimalController> OnAnimalDied;

    /// <summary>
    /// Событие, вызываемое при нанесении урона животному.
    /// </summary>
    public static Action<AnimalController, float> OnAnimalTookDamage;

    /// <summary>
    /// Событие, вызываемое при смене состояния животного.
    /// </summary>
    public static Action<AnimalController, AnimalStateBase> OnAnimalStateChanged;
}
