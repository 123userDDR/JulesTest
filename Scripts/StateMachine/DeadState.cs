/// <summary>
/// Состояние смерти. Животное мертво.
/// </summary>
public class DeadState : AnimalStateBase
{
    public DeadState(AnimalController animalController, AnimalStateMachine machine) : base(animalController, machine)
    {
    }

    public override void Enter()
    {
        // Логика смерти
        // Остановить все движение, проиграть анимацию смерти
        controller.Movement.StopMovement();
    }

    public override void Update()
    {
        // В состоянии смерти ничего не происходит
    }
}
