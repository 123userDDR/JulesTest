using UnityEngine;

/// <summary>
/// Состояние смерти (Dead). Финальное состояние, из которого нет выхода.
/// </summary>
public class DeadState : AnimalStateBase
{
    public DeadState(AnimalStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Debug.Log("Entering Dead State");

        // Убеждаемся, что животное мертво в системе здоровья
        if (!controller.Health.IsDead)
        {
            controller.Health.Kill(); // Убиваем, если еще не мертво
        }

        // Останавливаем любое движение
        controller.Movement.StopMovement();
        // Отключаем компонент движения, чтобы избежать любых дальнейших перемещений
        controller.Movement.enabled = false;

        // Запускаем анимацию смерти
        // (Это также делается по событию в AnimalAnimator, но дублируем для надежности)
        controller.AnimalAnimator.PlayDeathAnimation();

        // Звук смерти уже должен был проиграться через событие OnDeath,
        // но можно добавить и сюда для гарантии.
        // controller.AudioSystem.PlayDeathSound();

        // Вызываем систему дропа лута
        // (Это также делается в AnimalController по событию OnDeath)
        // var lootDropper = controller.GetComponent<LootDropper>();
        // if (lootDropper != null)
        // {
        //     lootDropper.DropLoot();
        // }

        // Отключаем компонент обнаружения
        if (controller.Detection != null)
        {
            controller.Detection.StopDetection();
            controller.Detection.enabled = false;
        }
    }

    public override void Update()
    {
        // В состоянии смерти ничего не происходит
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Dead State - this should not happen!");
        // Из этого состояния нет выхода
    }
}
