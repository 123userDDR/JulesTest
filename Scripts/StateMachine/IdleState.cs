using UnityEngine;

/// <summary>
/// Состояние покоя. Животное стоит на месте и осматривается.
/// </summary>
public class IdleState : AnimalStateBase
{
    private float idleTimer;
    private float timeToWander;

    public IdleState(AnimalController animalController, AnimalStateMachine machine) : base(animalController, machine)
    {
    }

    public override void Enter()
    {
        controller.Movement.StopMovement();
        idleTimer = 0f;

        // Используем настройки из ScriptableObject, если они есть
        var behaviorSettings = controller.GetComponent<AnimalBehaviorSettings>();
        if (behaviorSettings != null)
        {
            timeToWander = Random.Range(behaviorSettings.idleTimeMin, behaviorSettings.idleTimeMax);
        }
        else
        {
            timeToWander = Random.Range(2f, 5f); // Значения по умолчанию
        }
    }

    public override void Update()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= timeToWander)
        {
            stateMachine.TransitionToWandering();
        }
    }
}
