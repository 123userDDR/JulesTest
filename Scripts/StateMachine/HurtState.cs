using UnityEngine;

/// <summary>
/// Состояние получения урона. Животное проигрывает анимацию боли.
/// </summary>
public class HurtState : AnimalStateBase
{
    private float hurtDuration = 0.5f; // Короткая пауза при получении урона
    private float timer;

    public HurtState(AnimalController animalController, AnimalStateMachine machine) : base(animalController, machine)
    {
    }

    public override void Enter()
    {
        timer = 0f;
        controller.Movement.StopMovement();
        // Здесь можно включить триггер анимации боли
        // controller.AnimalAnimator.SetHurtTrigger();
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer >= hurtDuration)
        {
            stateMachine.TransitionToIdle();
        }
    }
}
