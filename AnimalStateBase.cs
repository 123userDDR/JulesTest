using UnityEngine;

/// <summary>
/// Базовый абстрактный класс для всех состояний животного.
/// Определяет основной жизненный цикл состояния: Enter, Update, Exit.
/// </summary>
public abstract class AnimalStateBase
{
    protected AnimalStateMachine stateMachine;
    protected AnimalController controller;

    /// <summary>
    /// Конструктор базового состояния.
    /// </summary>
    /// <param name="stateMachine">Ссылка на главную машину состояний.</param>
    public AnimalStateBase(AnimalStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        this.controller = stateMachine.Controller;
    }

    /// <summary>
    /// Вызывается при входе в состояние.
    /// Используется для начальной настройки.
    /// </summary>
    public abstract void Enter();

    /// <summary>
    /// Вызывается каждый кадр, пока состояние активно.
    /// Здесь находится основная логика состояния.
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// Вызывается при выходе из состояния.
    /// Используется для очистки и сброса.
    /// </summary>
    public abstract void Exit();

    /// <summary>
    /// Виртуальный метод для обработки получения урона.
    /// Позволяет состояниям по-разному реагировать на урон.
    /// </summary>
    public virtual void OnTakeDamage()
    {
        // По умолчанию, при получении урона переходим в состояние HurtState,
        // если животное еще живо.
        if (controller.Health.IsAlive)
        {
            stateMachine.ChangeState(new HurtState(stateMachine));
        }
    }
}
