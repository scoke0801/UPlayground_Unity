using UnityEngine;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "Condition_IsTurnNeeded", menuName = "UP/FSM/Conditions/Is Turn Needed")]
    public class IsTurnNeededConditionSO : TransitionConditionSO
    {
        [Header("Turn Detection")]
        [SerializeField] private float turnAngleThreshold = 45f;
        [SerializeField] private float oppositeInputThreshold = 150f; // 반대 방향 감지 각도
        
        public override bool CheckCondition(CharacterBrain brain)
        {
            float currentSpeed = brain.GetData<float>("LastSpeed");
            Vector3 currentInput = brain.InputDirection;
            Vector3 previousInput = brain.PreviousInputDirection;
            
            float inputMag = currentInput.sqrMagnitude;
            float prevInputMag = previousInput.sqrMagnitude;
            
            // Case 1: 현재 입력이 있고 각도 차이가 큰 경우
            if (inputMag > 0.01f)
            {
                float angle = Vector3.SignedAngle(brain.transform.forward, currentInput, Vector3.up);
                float absAngle = Mathf.Abs(angle);
                
                if (absAngle > turnAngleThreshold)
                {
                    brain.SetData("TurnAngle", angle);
                    return true;
                }
            }
            
            // Case 2: 반대 방향 키 입력 감지 (A→D 또는 W→S 등)
            // 현재 입력이 0이지만 이전 입력이 있었고, 반대 방향으로 전환하려는 경우
            if (inputMag < 0.01f && prevInputMag > 0.01f && currentSpeed > 0.5f)
            {
                // Raw Input을 직접 체크하여 반대 방향 키가 눌렸는지 확인
                Vector3 rawInput = GetRawInputDirection(brain);
                
                if (rawInput.sqrMagnitude > 0.01f)
                {
                    float angleBetweenInputs = Vector3.Angle(previousInput, rawInput);
                    
                    // 반대 방향 입력이 감지되면 180도 회전
                    if (angleBetweenInputs > oppositeInputThreshold)
                    {
                        float angle = Vector3.SignedAngle(brain.transform.forward, rawInput, Vector3.up);
                        brain.SetData("TurnAngle", angle);
                        Debug.Log($"Opposite input detected! Angle: {angleBetweenInputs:F1}°");
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        // Raw Input 가져오기 (카메라 회전 적용 전)
        private Vector3 GetRawInputDirection(CharacterBrain brain)
        {
            if (brain is PlayerBrain playerBrain)
            {
                // PlayerBrain에서 raw input 접근
                return playerBrain.GetRawInput();
            }
            return Vector3.zero;
        }
    }
}