using UnityEngine;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "Condition_IsStopNeeded", menuName = "UP/FSM/Conditions/Is Stop Needed")]
    public class IsStopNeededConditionSO : TransitionConditionSO
    {
        [Header("Stop Detection")]
        [Tooltip("이 속도 이상일 때만 Stop 애니메이션 재생")]
        [SerializeField] private float stopSpeedThreshold = 1.5f;
        
        [Tooltip("입력이 이 값 이하일 때 정지로 간주")]
        [SerializeField] private float inputDeadzone = 0.01f;

        public override bool CheckCondition(CharacterBrain brain)
        {
            // 1. 현재 입력 확인
            float inputMag = brain.InputDirection.sqrMagnitude;
            
            // 2. 현재 이동 속도 확인 (수평 속도만)
            Vector3 horizontalVelocity = new Vector3(
                brain.Motor.Velocity.x, 
                0, 
                brain.Motor.Velocity.z
            );
            float currentSpeed = horizontalVelocity.magnitude;
            
            // 3. Stop 조건 체크
            // - 입력이 없고 (키를 떼었고)
            // - 현재 속도가 임계값 이상일 때 (멈춰야 할 정도로 빠를 때)
            bool shouldStop = inputMag < inputDeadzone && currentSpeed > stopSpeedThreshold;
            
            if (shouldStop)
            {
                Debug.Log($"[Stop Condition] InputMag: {inputMag:F3}, Speed: {currentSpeed:F2}");
            }
            
            return shouldStop;
        }
    }
}