using UnityEngine;

/// <summary>
/// Состояние бегства. Животное убегает от угрозы.
/// </summary>
public class FleeingState : AnimalStateBase
{
    private GameObject threat;

    public FleeingState(AnimalController animalController, AnimalStateMachine machine, GameObject threat) : base(animalController, machine)
    {
        this.threat = threat;
    }

    public override void Enter()
    {
        controller.Movement.SetSprintSpeed();
    }

    public override void Update()
    {
        if (threat == null)
        {
            // Угроза исчезла, просто бежим вперед
            controller.Movement.MoveInDirection(controller.transform.forward);
            return;
        }

        // Бежим в направлении, противоположном угрозе
        Vector3 fleeDirection = (controller.transform.position - threat.transform.position).normalized;
        controller.Movement.MoveInDirection(fleeDirection);
    }

    public override void Exit()
    {
        // При выходе из состояния бегства, сбрасываем скорость до обычной
        controller.Movement.SetWalkSpeed();
    }
}
