using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Locomotion_Advanced", menuName = "UP/FSM/States/Locomotion Advanced")]
    public class LocomotionStateSO : StateSO
    {
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float sprintSpeed = 12f;
        public float rotationSpeed = 15f;
        public float acceleration = 10f;
        
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.1f;
        
        [Header("Transitions")]
        public StateSO stopState;
        public StateSO turnInPlaceState;

        [Header("Thresholds")]
        public float stopSpeedThreshold = 1.5f;
        public float turnAngleThreshold = 45f;
        
        private Vector3 _currentVelocity = Vector3.zero;
        
        public override void OnEnter(CharacterBrain brain)
        {
            ITransition mixer = brain.AnimData.GetAnimation(AnimKey.Mixer_Locomotion);
            AnimancerState state = brain.Animancer.Play(mixer, fadeDuration);
            
            brain.SetData("LocomotionState", state);
            
            // 현재 속도 유지
            Vector3 horizontalVelocity = new Vector3(brain.Rb.linearVelocity.x, 0, brain.Rb.linearVelocity.z);
            _currentVelocity = horizontalVelocity;
            Debug.Log($"OnEnter: {_currentVelocity.magnitude}, mixer: {mixer}");
            // 초기 애니메이션 파라미터 설정
            if (state is LinearMixerState mixerState)
            {
                //float initialParameter = GetInitialMixerParameter(brain);
                mixerState.Parameter = 0;
            }
        }
        
        private float GetInitialMixerParameter(CharacterBrain brain)
        {
            Vector3 horizontalVelocity = new Vector3(brain.Rb.linearVelocity.x, 0, brain.Rb.linearVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;
            float inputMag = brain.InputDirection.sqrMagnitude;
            
            if (inputMag > 0.1f)
            {
                if (currentSpeed >= runSpeed) return 3f;
                else if (currentSpeed >= walkSpeed) return 2f;
                else return 1f;
            }
            
            return 0f;
        }
        
        public override void OnUpdate(CharacterBrain brain)
        {
            float inputMag = brain.InputDirection.sqrMagnitude;
            Vector3 horizontalVelocity = new Vector3(brain.Rb.linearVelocity.x, 0, brain.Rb.linearVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            brain.SetData("LastSpeed", currentSpeed);
    
            // 현재 보는 방향과 입력 방향 사이의 각도 계산
            float angle = Vector3.SignedAngle(brain.transform.forward, brain.InputDirection, Vector3.up);
            brain.SetData("TurnAngle", angle);
            float absAngle = Mathf.Abs(angle);
            
            //Debug.Log($"OnUpdate Speed: {currentSpeed}, InputDirection: {brain.InputDirection}, angle: {angle}, absAngle: {absAngle}");

            // [개선] 1. 방향 전환 검사 (최우선)
            // 180도 턴은 입력이 반전되는 순간(inputMag가 잠시 낮아져도) 바로 실행되어야 함
            if (inputMag > 0.01f && absAngle > turnAngleThreshold)
            {                
                brain.ChangeState(turnInPlaceState);
                return;
            }

            // 2. 정지 상태 검사 (방향 전환 조건이 아닐 때만 실행)
            if (inputMag < 0.01f && currentSpeed > stopSpeedThreshold)
            {
                brain.ChangeState(stopState);
                return;
            }

            // 3. 애니메이션 믹서 파라미터 업데이트 (가속/감속 표현)
            var state = brain.GetData<LinearMixerState>("LocomotionState");
            if (state != null)
            {
                float targetSpeedValue = 0f;
                if (inputMag > 0.1f)
                {
                    if (currentSpeed >= runSpeed) targetSpeedValue = 3f;
                    else if (currentSpeed >= walkSpeed) targetSpeedValue = 2f;
                    else targetSpeedValue = 1f;
                }
                state.Parameter = currentSpeed;
            }
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            MoveCharacter(brain);
            RotateCharacter(brain);
        }

        private void MoveCharacter(CharacterBrain brain)
        {
            float targetSpeedValue = 0f;
            if (brain.IsSprintPressed) targetSpeedValue = sprintSpeed;
            else if (brain.InputDirection.sqrMagnitude > 0.8f) targetSpeedValue = runSpeed;
            else targetSpeedValue = walkSpeed;
            
            Vector3 targetVelocity = (brain.InputDirection.sqrMagnitude > 0.01f) 
                ? brain.InputDirection * targetSpeedValue 
                : Vector3.zero;

            _currentVelocity = Vector3.Lerp(
                _currentVelocity, 
                targetVelocity, 
                acceleration * Time.deltaTime
            );
            
            _currentVelocity.y = brain.Rb.linearVelocity.y;
            brain.Rb.linearVelocity = _currentVelocity;
        }

        private void RotateCharacter(CharacterBrain brain)
        {
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(brain.InputDirection);
                brain.Rb.MoveRotation(Quaternion.Slerp(brain.transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
            }
        }
        
    }
}