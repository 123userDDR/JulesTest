using UnityEngine;

/// <summary>
/// Состояние поедания. Животное ест траву.
/// </summary>
public class EatingState : AnimalStateBase
{
    private GameObject grassTarget;
    private bool isAtTarget;
    private float eatTimer;

    public EatingState(AnimalController animalController, AnimalStateMachine machine, GameObject grass) : base(animalController, machine)
    {
        this.grassTarget = grass;
    }

    public override void Enter()
    {
        isAtTarget = false;
        eatTimer = 0f;

        if (grassTarget == null)
        {
            stateMachine.TransitionToIdle();
            return;
        }

        controller.Movement.MoveToPosition(grassTarget.transform.position);
        controller.Movement.SetWalkSpeed();
        controller.Movement.OnReachedTarget += HandleTargetReached;
    }

    public override void Update()
    {
        if (grassTarget == null && !isAtTarget)
        {
            // Трава исчезла, пока мы к ней шли
            stateMachine.TransitionToIdle();
            return;
        }

        if (isAtTarget)
        {
            eatTimer += Time.deltaTime;
            var behaviorSettings = controller.GetComponent<AnimalBehaviorSettings>();
            float eatTime = behaviorSettings != null ? behaviorSettings.eatTime : 4f;

            if (eatTimer >= eatTime)
            {
                // Поели, можно и отдохнуть
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
        isAtTarget = true;
        controller.Movement.StopMovement();

        // Тут можно было бы включить анимацию поедания
        // controller.AnimalAnimator.SetEating(true);

        // Уничтожаем траву, которую съели
        if (grassTarget != null)
        {
            Object.Destroy(grassTarget);
        }
    }
}
