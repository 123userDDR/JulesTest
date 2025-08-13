using UnityEngine;

/// <summary>
/// Состояние получения урона (Hurt). Кратковременное состояние, прерывающее другие действия.
/// </summary>
public class HurtState : AnimalStateBase
{
    private float hurtTimer;
    private float hurtDuration;

    public HurtState(AnimalStateMachine stateMachine) : base(stateMachine)
    {
        var behaviorSettings = controller.GetComponent<AnimalBehaviorSettings>();
        hurtDuration = behaviorSettings != null ? behaviorSettings.hurtDuration : 1f;
    }

    public override void Enter()
    {
        // Debug.Log("Entering Hurt State");

        // Останавливаем движение
        controller.Movement.StopMovement();

        // Запускаем анимацию и звук получения урона
        // (Это также делается автоматически по событию в AnimalAnimator и AnimalAudio,
        // но можно и здесь для надежности)
        controller.AnimalAnimator.PlayHurtAnimation();
        controller.AudioSystem.PlayHurtSound();

        hurtTimer = 0f;
    }

    public override void Update()
    {
        hurtTimer += Time.deltaTime;

        // После окончания анимации боли, решаем, что делать дальше
        if (hurtTimer >= hurtDuration)
        {
            // Если есть угроза, убегаем
            if (controller.Detection.HasThreats)
            {
                stateMachine.ChangeState(new FleeingState(stateMachine, controller.Detection.GetNearestThreat()));
            }
            else
            {
                // Иначе, возвращаемся в состояние покоя
                stateMachine.ChangeState(new IdleState(stateMachine));
            }
        }
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Hurt State");
        // Очистка не требуется
    }
}
