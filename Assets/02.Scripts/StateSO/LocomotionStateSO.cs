using UnityEngine;
using Animancer;
using UnityEngine.Serialization;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Locomotion_Advanced", menuName = "UP/FSM/States/Locomotion Advanced")]
    public class LocomotionStateSO : StateSO, IMovementState
    {
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float sprintSpeed = 12f;
        public float orientationSharpness = 10f;
        public float accelerationSharpness = 7.5f; // 낮을수록 천천히 가속
        public float decelerationSharpness = 15f; // 높을수록 빨리 멈춤
        
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.1f;
        
        [Header("Transitions")]
        public StateSO stopState;
        public StateSO turnInPlaceState;

        [Header("Thresholds")]
        public float stopSpeedThreshold = 1.5f;
        public float turnAngleThreshold = 45f;
        
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
            float absAngle = Mathf.Abs(angle);
            
            // [개선] 1. 방향 전환 검사 (최우선)
            if (inputMag > 0.01f && absAngle > turnAngleThreshold)
            {                
                //brain.ChangeState(turnInPlaceState);
                //return;
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
                state.Parameter = currentSpeed;
            }
        }
        
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime, CharacterBrain brain)
        {
            // 1. 현재 속도를 수평(Horizontal)과 수직(Vertical) 성분으로 분리
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, brain.Motor.CharacterUp);
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, brain.Motor.CharacterUp);

            if (brain.Motor.GroundingStatus.IsStableOnGround)
            {
                // 2. 입력 확인 및 목표 속도 결정
                float targetSpeed = 0f;
                // 아주 작은 입력은 무시하도록 데드존 설정 (0.01f)
                if (brain.InputDirection.sqrMagnitude > 0.01f)
                {
                    if (brain.IsSprintPressed) targetSpeed = sprintSpeed;
                    else if (brain.InputDirection.sqrMagnitude > 0.8f) targetSpeed = runSpeed;
                    else targetSpeed = walkSpeed;
                }

                // 3. 지면의 경사(Normal)를 고려한 이동 방향 계산
                // 단순히 InputDirection을 쓰는게 아니라 지면을 타고 흐르도록 합니다.
                Vector3 targetMovementDirection = brain.Motor.GetDirectionTangentToSurface(brain.InputDirection, brain.Motor.GroundingStatus.GroundNormal);
                Vector3 targetMovementVelocity = targetMovementDirection * targetSpeed;

                // 목표 속도에 따라 다른 Sharpness 적용
                float currentSharpness = (targetSpeed > 0.01f) ? accelerationSharpness : decelerationSharpness;

                // 4. 수평 속도만 목표치로 보간 (점진적 감소/증가)
                // 1f - Mathf.Exp(-sharpness * deltaTime)은 프레임 레이트에 독립적인 보간 방식입니다.
                //horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetMovementVelocity, 1f - Mathf.Exp(-stableMovementSharpness * deltaTime));
                horizontalVelocity = Vector3.Lerp(
                    horizontalVelocity, 
                    targetMovementVelocity, 
                    1f - Mathf.Exp(-currentSharpness * deltaTime)
                );
                
                // 지면에서는 중력으로 인해 쌓인 수직 속도를 초기화하여 바닥으로 파고드는 현상을 방지할 수 있습니다.
                // (KCC Motor가 알아서 처리하지만, 명시적으로 0에 가깝게 유지하는 것이 안정적입니다)
                verticalVelocity = Vector3.zero; 
            }
            else
            {
                // 공중 상태일 때의 추가 로직 (필요 시)
            }

            // 5. 최종 속도 재조합
            currentVelocity = horizontalVelocity + verticalVelocity;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime, CharacterBrain brain)
        {
            Vector3 lookDirection = brain.OverrideLookDirection ?? brain.InputDirection;
            if (lookDirection.sqrMagnitude > 0f && orientationSharpness > 0f)
            {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(brain.Motor.CharacterForward, lookDirection, 1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, brain.Motor.CharacterUp);
            }
            
            Vector3 currentUp = (currentRotation * Vector3.up);
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-orientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }
    }
}