using UnityEngine;

/// <summary>
/// Состояние блуждания (Wandering). Животное перемещается между точками маршрута.
/// </summary>
public class WanderingState : AnimalStateBase
{
    private Transform targetWaypoint;
    private AnimalBehaviorSettings behaviorSettings;

    public WanderingState(AnimalStateMachine stateMachine) : base(stateMachine)
    {
        // Кэшируем настройки для производительности
        behaviorSettings = controller.GetComponent<AnimalBehaviorSettings>();
    }

    public override void Enter()
    {
        // Debug.Log("Entering Wandering State");

        // Получаем следующую точку от системы waypoints
        targetWaypoint = controller.WaypointSystem.GetNextWaypoint();

        if (targetWaypoint != null)
        {
            // Начинаем движение к точке
            controller.Movement.MoveToPosition(targetWaypoint.position);
            controller.Movement.SetWalkSpeed();
        }
        else
        {
            // Если нет waypoints, просто переходим в Idle
            stateMachine.ChangeState(new IdleState(stateMachine));
        }
    }

    public override void Update()
    {
        // Проверяем на наличие угроз
        if (controller.Detection.HasThreats)
        {
            stateMachine.ChangeState(new FleeingState(stateMachine, controller.Detection.GetNearestThreat()));
            return;
        }

        // Проверяем, голодно ли животное и видит ли оно траву
        float chanceToEat = behaviorSettings != null ? behaviorSettings.chanceToEat : 0.5f;
        if (controller.Health.HealthPercentage < (behaviorSettings?.lowHealthThreshold ?? 0.8f) &&
            controller.Detection.HasGrass && Random.value < chanceToEat)
        {
            stateMachine.ChangeState(new EatingState(stateMachine, controller.Detection.GetNearestGrass()));
            return;
        }

        // Проверяем, достигли ли мы цели
        float reachRadius = behaviorSettings != null ? behaviorSettings.waypointReachedRadius : 1.5f;
        if (controller.Movement.HasReachedTarget() || controller.WaypointSystem.HasReachedCurrentWaypoint(reachRadius))
        {
            // Достигли точки, переходим в состояние покоя на некоторое время
            stateMachine.ChangeState(new IdleState(stateMachine));
        }
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Wandering State");
        // Останавливаем движение при выходе из состояния, на всякий случай
        controller.Movement.StopMovement();
    }
}
