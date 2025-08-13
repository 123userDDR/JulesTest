using UnityEngine;

/// <summary>
/// Состояние покоя (Idle). Животное стоит на месте и осматривается.
/// </summary>
public class IdleState : AnimalStateBase
{
    private float idleTimer;
    private float idleDuration;

    public IdleState(AnimalStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Debug.Log("Entering Idle State");

        // Останавливаем движение
        controller.Movement.StopMovement();

        // Устанавливаем анимацию покоя
        controller.AnimalAnimator.PlayIdleAnimation();

        // Устанавливаем случайное время нахождения в этом состоянии
        if (controller.GetComponent<AnimalBehaviorSettings>() != null) // Предполагаем, что настройки на том же объекте
        {
            var settings = controller.GetComponent<AnimalBehaviorSettings>();
            idleDuration = Random.Range(settings.idleDurationRange.x, settings.idleDurationRange.y);
        }
        else
        {
            idleDuration = 5f; // Значение по умолчанию
        }

        idleTimer = 0f;
    }

    public override void Update()
    {
        idleTimer += Time.deltaTime;

        // Проверяем на наличие угроз
        if (controller.Detection.HasThreats)
        {
            stateMachine.ChangeState(new FleeingState(stateMachine, controller.Detection.GetNearestThreat()));
            return;
        }

        // Проверяем, голодно ли животное и видит ли оно траву
        if (controller.Health.HealthPercentage < 0.8f && controller.Detection.HasGrass)
        {
            // Можно добавить шанс из AnimalBehaviorSettings
            stateMachine.ChangeState(new EatingState(stateMachine, controller.Detection.GetNearestGrass()));
            return;
        }

        // Если время вышло, переходим в состояние блуждания
        if (idleTimer >= idleDuration)
        {
            stateMachine.ChangeState(new WanderingState(stateMachine));
        }
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Idle State");
        // Очистка не требуется
    }
}
