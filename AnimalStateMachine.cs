using UnityEngine;

/// <summary>
/// Главный класс машины состояний (State Machine).
/// Управляет жизненным циклом состояний: смена, обновление.
/// </summary>
public class AnimalStateMachine
{
    public AnimalStateBase CurrentState { get; private set; }
    public AnimalController Controller { get; }

    public AnimalStateMachine(AnimalController controller)
    {
        this.Controller = controller;
    }

    /// <summary>
    /// Запускает машину состояний, устанавливая начальное состояние.
    /// </summary>
    public void Start()
    {
        // Начальное состояние - Idle
        ChangeState(new IdleState(this));
    }

    /// <summary>
    /// Меняет текущее состояние на новое.
    /// </summary>
    /// <param name="newState">Новое состояние для активации.</param>
    public void ChangeState(AnimalStateBase newState)
    {
        // Вызываем Exit у текущего состояния, если оно есть
        CurrentState?.Exit();

        // Устанавливаем новое состояние
        CurrentState = newState;

        // Вызываем Enter у нового состояния
        CurrentState.Enter();

        // Debug.Log($"[StateMachine] Changed state to: {newState.GetType().Name}");
    }

    /// <summary>
    /// Обновляет логику текущего активного состояния.
    /// Вызывается из AnimalController.Update().
    /// </summary>
    public void UpdateStateMachine()
    {
        CurrentState?.Update();
    }

    // Дополнительные проверки, которые могут быть полезны для состояний

    /// <summary>
    /// Может ли животное сейчас начать есть?
    /// (Например, не в состоянии бегства или боли)
    /// </summary>
    public bool CanEat()
    {
        return !(CurrentState is FleeingState || CurrentState is HurtState || CurrentState is DeadState);
    }
}
