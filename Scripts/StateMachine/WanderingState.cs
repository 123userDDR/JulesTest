using UnityEngine;

/// <summary>
/// Состояние блуждания. Животное перемещается между точками маршрута.
/// </summary>
public class WanderingState : AnimalStateBase
{
    private Vector3 targetPosition;
    private float pauseTimer;
    private bool hasReachedDestination;

    public WanderingState(AnimalController animalController, AnimalStateMachine machine) : base(animalController, machine)
    {
    }

    public override void Enter()
    {
        hasReachedDestination = false;
        pauseTimer = 0f;

        targetPosition = controller.WaypointSystem.GetRandomWaypoint();
        controller.Movement.MoveToPosition(targetPosition);
        controller.Movement.SetWalkSpeed();

        controller.Movement.OnReachedTarget += HandleTargetReached;
    }

    public override void Update()
    {
        if (hasReachedDestination)
        {
            pauseTimer += Time.deltaTime;
            var behaviorSettings = controller.GetComponent<AnimalBehaviorSettings>();
            float wanderPauseTime = behaviorSettings != null ? behaviorSettings.wanderPauseTime : 1f;

            if (pauseTimer >= wanderPauseTime)
            {
                stateMachine.TransitionToIdle();
            }
        }
    }

    public override void Exit()
    {
        controller.Movement.OnReachedTarget -= HandleTargetReached;
    }

    private void HandleTargetReached()
    {
        hasReachedDestination = true;
        controller.Movement.StopMovement();
    }
}
