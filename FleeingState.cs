using UnityEngine;

/// <summary>
/// Состояние бегства (Fleeing). Животное убегает от угрозы.
/// </summary>
public class FleeingState : AnimalStateBase
{
    private GameObject threat;
    private float timeSinceThreatLost = 0f;
    private AnimalBehaviorSettings behaviorSettings;

    public FleeingState(AnimalStateMachine stateMachine, GameObject threat) : base(stateMachine)
    {
        this.threat = threat;
        this.behaviorSettings = controller.GetComponent<AnimalBehaviorSettings>();
    }

    public override void Enter()
    {
        // Debug.Log("Entering Fleeing State");

        // Воспроизводим звук тревоги
        controller.AudioSystem.PlayAlertSound();

        // Устанавливаем максимальную скорость
        controller.Movement.SetSprintSpeed();
    }

    public override void Update()
    {
        // Проверяем, жива ли еще угроза
        if (threat == null || !controller.Detection.DetectedThreats.Contains(threat))
        {
            // Угроза потеряна, начинаем таймер "успокоения"
            timeSinceThreatLost += Time.deltaTime;
            float fleeAfterLostDuration = behaviorSettings != null ? behaviorSettings.fleeDurationAfterLosingThreat : 3f;

            if (timeSinceThreatLost >= fleeAfterLostDuration)
            {
                // Успокоились, возвращаемся в Idle
                stateMachine.ChangeState(new IdleState(stateMachine));
                return;
            }
        }
        else
        {
            // Угроза все еще здесь, сбрасываем таймер и продолжаем бежать
            timeSinceThreatLost = 0f;
        }

        // Вычисляем направление бегства (от угрозы)
        Vector3 fleeDirection = (controller.transform.position - threat.transform.position).normalized;

        // Добавляем немного случайности, чтобы бегство не было прямолинейным
        fleeDirection += new Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f));

        // Пытаемся найти безопасную точку для бегства
        Vector3 fleePosition = controller.transform.position + fleeDirection.normalized * (behaviorSettings?.fleeDistance ?? 20f);

        // Движемся в направлении от угрозы
        controller.Movement.MoveToPosition(fleePosition);
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Fleeing State");
        // При выходе из состояния бегства, сбрасываем скорость до ходьбы,
        // чтобы следующее состояние (Wandering) началось с нормальной скоростью.
        controller.Movement.SetWalkSpeed();
    }
}
