using UnityEngine;

/// <summary>
/// Состояние поедания (Eating). Животное ест траву для восстановления здоровья.
/// </summary>
public class EatingState : AnimalStateBase
{
    private GameObject foodSource;
    private float eatingTimer;
    private float eatingDuration;
    private bool isAtFoodSource = false;
    private AnimalBehaviorSettings behaviorSettings;

    public EatingState(AnimalStateMachine stateMachine, GameObject foodSource) : base(stateMachine)
    {
        this.foodSource = foodSource;
        this.behaviorSettings = controller.GetComponent<AnimalBehaviorSettings>();
    }

    public override void Enter()
    {
        // Debug.Log("Entering Eating State");

        if (foodSource == null)
        {
            // Если еда исчезла, возвращаемся в Idle
            stateMachine.ChangeState(new IdleState(stateMachine));
            return;
        }

        // Движемся к еде
        controller.Movement.MoveToPosition(foodSource.transform.position);
        controller.Movement.SetWalkSpeed();
        isAtFoodSource = false;
    }

    public override void Update()
    {
        // Проверяем на наличие угроз в первую очередь
        if (controller.Detection.HasThreats)
        {
            stateMachine.ChangeState(new FleeingState(stateMachine, controller.Detection.GetNearestThreat()));
            return;
        }

        if (foodSource == null)
        {
            // Если еда исчезла (например, ее съел кто-то другой)
            stateMachine.ChangeState(new IdleState(stateMachine));
            return;
        }

        if (!isAtFoodSource)
        {
            // Если еще не у источника еды, проверяем, дошли ли
            if (controller.Movement.HasReachedTarget())
            {
                BeginEating();
            }
        }
        else
        {
            // Если уже едим, обновляем таймер
            eatingTimer += Time.deltaTime;
            if (eatingTimer >= eatingDuration)
            {
                FinishEating();
            }
        }
    }

    private void BeginEating()
    {
        isAtFoodSource = true;

        // Останавливаемся и начинаем анимацию еды
        controller.Movement.StopMovement();
        controller.AnimalAnimator.StartEating();
        controller.AudioSystem.PlayGrassEatingSound(); // Можно зациклить звук

        // Устанавливаем таймер
        eatingDuration = Random.Range(behaviorSettings.eatingDurationRange.x, behaviorSettings.eatingDurationRange.y);
        eatingTimer = 0f;
    }

    private void FinishEating()
    {
        // Восстанавливаем здоровье
        float healthToRestore = behaviorSettings != null ? behaviorSettings.healthRestoredFromEating : 10f;
        controller.Health.Heal(healthToRestore);

        // Уничтожаем траву (опционально, зависит от геймдизайна)
        // Object.Destroy(foodSource);

        // Переходим в состояние покоя
        stateMachine.ChangeState(new IdleState(stateMachine));
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Eating State");

        // Гарантированно останавливаем анимацию еды при выходе
        controller.AnimalAnimator.StopEating();
    }
}
