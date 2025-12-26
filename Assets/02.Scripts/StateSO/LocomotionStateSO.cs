using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Locomotion_RB", menuName = "UP/FSM/States/Locomotion (Rigidbody)")]
    public class LocomotionStateSO : StateSO
    {
        [Header("Movement Settings")]
        public float MoveSpeed = 6f;
        public float RotationSpeed = 15f;
        
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.1f;
        
        [Header("Transitions (Branching)")]
        public StateSO FallState;
        
        
        public override void OnEnter(CharacterBrain brain)
        {
            ITransition mixer = brain.AnimData.GetAnimation(AnimKey.Mixer_Locomotion);
            
            AnimancerState state = brain.Animancer.Play(mixer, fadeDuration);
            
            brain.SetData("LocomotionState", state);
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            // 1. 애니메이션 블렌딩 처리
            var state = brain.GetData<LinearMixerState>("LocomotionState");
            if (state != null)
            {
                float inputMagnitude = brain.InputDirection.magnitude;
                
                // 입력이 0이면 state.Parameter에 0이 들어가 Idle 애니메이션이 재생됨
                state.Parameter = inputMagnitude;
            }
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            MoveCharacter(brain);
            RotateCharacter(brain);
        }

        private void MoveCharacter(CharacterBrain brain)
        {
            // 입력 방향이 없으면 멈추고, 있으면 이동
            Vector3 targetVelocity = Vector3.zero;

            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                targetVelocity = brain.InputDirection * MoveSpeed;
            }

            targetVelocity.y = brain.Rb.linearVelocity.y;

            // 속도 적용
            brain.Rb.linearVelocity = targetVelocity;
        }

        private void RotateCharacter(CharacterBrain brain)
        {
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(brain.InputDirection);
                
                Quaternion nextRotation = Quaternion.Slerp(
                    brain.transform.rotation, 
                    targetRotation, 
                    Time.fixedDeltaTime * RotationSpeed
                );
                
                brain.Rb.MoveRotation(nextRotation);
            }
        }
    }
}