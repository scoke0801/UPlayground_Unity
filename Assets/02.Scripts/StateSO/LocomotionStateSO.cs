using UnityEngine;
using Animancer;
using UnityEngine.Serialization;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Locomotion_Advanced", menuName = "UP/FSM/States/Locomotion Advanced")]
    public class LocomotionStateSO : StateSO, IMovementState
    {
        [Header("Movement Settings")] public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float sprintSpeed = 12f;
        
        [Header("Animation")] [SerializeField] private float fadeDuration = 0.1f;

        [Header("Animation Sync")] public float minMoveSpeed = 0.5f; // 이 속도 이하에서는 이동하지 않음
        public float animationSyncSharpness = 10f; // 애니메이션 파라미터 보간 속도

        [Header("Transitions")] public StateSO stopState;
        public StateSO turnInPlaceState;

        [Header("Thresholds")] public float stopSpeedThreshold = 1.5f;
        public float turnAngleThreshold = 45f;

        // 애니메이션 파라미터를 부드럽게 업데이트하기 위한 변수
        private float currentAnimationSpeed = 0f;

        public override void OnEnter(CharacterBrain brain)
        {
            Debug.Log("LocomotionStateSO.OnEnter");
            ITransition mixer = brain.AnimData.GetAnimation(AnimKey.Mixer_Locomotion);
            AnimancerState state = brain.Animancer.Play(mixer, fadeDuration);

            brain.SetData("LocomotionState", state);

            // 초기 애니메이션 파라미터 설정
            if (state is LinearMixerState mixerState)
            {
                mixerState.Parameter = 0;
                mixerState.ApplyFootIK = true;
            }

            currentAnimationSpeed = 0f;

        }

        public override void OnUpdate(CharacterBrain brain)
        {
            float inputMag = brain.InputDirection.sqrMagnitude;
            Vector3 horizontalVelocity = new Vector3(brain.Motor.Velocity.x, 0, brain.Motor.Velocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            brain.SetData("LastSpeed", currentSpeed);

            // 현재 보는 방향과 입력 방향 사이의 각도 계산
            float angle = Vector3.SignedAngle(brain.transform.forward, brain.InputDirection, Vector3.up);
            brain.SetData("TurnAngle", angle);

            // 3. 애니메이션 믹서 파라미터 업데이트 (가속/감속 표현)
            var state = brain.GetData<LinearMixerState>("LocomotionState");
            if (state != null)
            {
                // currentAnimationSpeed = Mathf.Lerp(
                //     currentAnimationSpeed,
                //     currentSpeed,
                //     1f - Mathf.Exp(-animationSyncSharpness * Time.deltaTime)
                // );

                state.Parameter = currentSpeed;
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime, CharacterBrain brain)
        {
            brain.Controller.DoDefaultUpdateVelocity(ref currentVelocity, deltaTime);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime, CharacterBrain brain)
        {
            Vector3 lookDirection = brain.OverrideLookDirection ?? brain.InputDirection;
            if (lookDirection.sqrMagnitude > 0f && brain.MovementData.OrientationSharpness > 0f)
            {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(brain.Motor.CharacterForward, lookDirection, 1 - Mathf.Exp(-brain.MovementData.OrientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, brain.Motor.CharacterUp);
            }
            
            Vector3 currentUp = (currentRotation * Vector3.up);
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-brain.MovementData.OrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }
    }
}