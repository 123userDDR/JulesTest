using UnityEngine;

/// <summary>
/// Управляет состояниями животного, переключая их и обновляя текущее.
/// </summary>
public class AnimalStateMachine
{
    public AnimalStateBase CurrentState { get; private set; }

    private AnimalController controller;

    public AnimalStateMachine(AnimalController animalController)
    {
        this.controller = animalController;
    }

    public void Start()
    {
        TransitionToIdle();
    }

    public void Update()
    {
        CurrentState?.Update();
    }

    public void FixedUpdate()
    {
        CurrentState?.FixedUpdate();
    }

    private void ChangeState(AnimalStateBase newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    // --- State Transition Methods ---

    public void TransitionToIdle()
    {
        ChangeState(new IdleState(controller, this));
    }

    public void TransitionToWandering()
    {
        ChangeState(new WanderingState(controller, this));
    }

    public void TransitionToEating(GameObject grass)
    {
        if (CanEat())
        {
            ChangeState(new EatingState(controller, this, grass));
        }
    }

    public void TransitionToFleeing(GameObject threat)
    {
        if (controller.IsAlive)
        {
            ChangeState(new FleeingState(controller, this, threat));
        }
    }

    public void TransitionToHurt()
    {
        if (controller.IsAlive)
        {
            ChangeState(new HurtState(controller, this));
        }
    }

    public void TransitionToDead()
    {
        ChangeState(new DeadState(controller, this));
    }

    // --- State Condition Checks ---

    public bool CanEat()
    {
        return !(CurrentState is FleeingState || CurrentState is HurtState || CurrentState is EatingState);
    }
}
