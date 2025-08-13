using UnityEngine;

/// <summary>
/// Базовый абстрактный класс для всех состояний животного.
/// </summary>
public abstract class AnimalStateBase
{
    protected AnimalController controller;
    protected AnimalStateMachine stateMachine;

    public AnimalStateBase(AnimalController animalController, AnimalStateMachine machine)
    {
        this.controller = animalController;
        this.stateMachine = machine;
    }

    /// <summary>
    /// Вызывается при входе в состояние.
    /// </summary>
    public virtual void Enter()
    {
    }

    /// <summary>
    /// Вызывается каждый кадр, когда состояние активно.
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// Вызывается при выходе из состояния.
    /// </summary>
    public virtual void Exit()
    {
    }

    /// <summary>
    /// Вызывается для обработки физики.
    /// </summary>
    public virtual void FixedUpdate()
    {
    }
}
