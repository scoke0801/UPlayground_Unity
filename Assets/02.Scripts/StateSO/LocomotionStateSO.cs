using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Locomotion_Advanced", menuName = "UP/FSM/States/Locomotion Advanced")]
    public class LocomotionStateSO : StateSO
    {
        [Header("Movement Settings")]
        public float MoveSpeed = 6f;
        public float RotationSpeed = 15f;
        
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.1f;
        
        [Header("Transitions")]
        public StateSO StopState;
        public StateSO TurnInPlaceState;

        [Header("Thresholds")]
        public float StopSpeedThreshold = 1.5f; // 이 속도 이상일 때 멈추면 Stop 애니메이션 재생
        public float TurnAngleThreshold = 45f;  // 이 각도 이상 차이날 때 제자리 회전 실행
        
        public override void OnEnter(CharacterBrain brain)
        {
            ITransition mixer = brain.AnimData.GetAnimation(AnimKey.Mixer_Locomotion);
            
            AnimancerState state = brain.Animancer.Play(mixer, fadeDuration);
            
            brain.SetData("LocomotionState", state);
        }
        
        public override void OnUpdate(CharacterBrain brain)
        {
            float inputMag = brain.InputDirection.sqrMagnitude;
            Vector3 horizontalVelocity = new Vector3(brain.Rb.linearVelocity.x, 0, brain.Rb.linearVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            // 1. 제자리 회전 체크: 거의 멈춰있는 상태에서 입력 각도가 클 때
            if (inputMag > 0.01f && currentSpeed < 0.5f)
            {
                float angle = Vector3.SignedAngle(brain.transform.forward, brain.InputDirection, Vector3.up);
                if (Mathf.Abs(angle) > TurnAngleThreshold)
                {
                    brain.SetData("TurnAngle", angle);
                    brain.ChangeState(TurnInPlaceState);
                    return;
                }
            }

            // 2. 정지 체크: 입력은 없는데 움직이던 속도가 빠를 때
            if (inputMag < 0.01f && currentSpeed > StopSpeedThreshold)
            {
                brain.SetData("LastSpeed", currentSpeed);
                brain.ChangeState(StopState);
                return;
            }

            // 3. 기존 애니메이션 믹서 업데이트
            var state = brain.GetData<LinearMixerState>("LocomotionState");
            if (state != null)
            {
                state.Parameter = brain.InputDirection.magnitude;
            }
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            MoveCharacter(brain);
            RotateCharacter(brain);
        }

        private void MoveCharacter(CharacterBrain brain)
        {
            Vector3 targetVelocity = (brain.InputDirection.sqrMagnitude > 0.01f) 
                ? brain.InputDirection * MoveSpeed 
                : Vector3.zero;

            targetVelocity.y = brain.Rb.linearVelocity.y;
            brain.Rb.linearVelocity = targetVelocity;
        }

        private void RotateCharacter(CharacterBrain brain)
        {
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(brain.InputDirection);
                brain.Rb.MoveRotation(Quaternion.Slerp(brain.transform.rotation, targetRotation, Time.fixedDeltaTime * RotationSpeed));
            }
        }
    }
}