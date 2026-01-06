using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_TurnInPlace_Advanced", menuName = "UP/FSM/States/Turn In Place Advanced")]
    public class TurnInPlaceStateSO : StateSO, IMovementState
    {
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private LocomotionStateSO locomotionState;

        [Header("Root Motion Settings")]
        [SerializeField] private RootMotionMode rootMotionMode = RootMotionMode.RotationAndPosition;
        [SerializeField] private float positionWeight = 1f; // Root Motion Position 영향력
        [SerializeField] private float rotationWeight = 0.7f; // Root Motion Rotation 영향력
        
        [Header("Code Fallback")]
        [SerializeField] private float codeRotationSpeed = 540f;
        [SerializeField] private float decelerationSpeed = 15f; // Root Motion이 없을 때만 사용
        
        [Header("Completion")]
        [SerializeField] private float rotationAccuracy = 5f;
        [SerializeField] private bool snapToTargetOnComplete = true;
        
        [Header("Early Exit")]
        [SerializeField] private bool allowEarlyExit = true;
        [SerializeField] private float minAnimationTime = 0.3f;
        [SerializeField] private float earlyExitAngleThreshold = 30f;

        // 내부 상태
        private CharacterBrain _cachedBrain;
        private AnimancerState _animState;
        private Vector3 _targetDirection;
        private Quaternion _targetRotation;
        private float _targetAngle;
        private float _startTime;
        private bool _rotationCompleted;
        
        // Root Motion 데이터
        private Vector3 _rootMotionPositionDelta;
        private Quaternion _rootMotionRotationDelta;
        private bool _hasRootMotion;

        public enum RootMotionMode
        {
            RotationOnly,           // 회전만 Root Motion
            PositionOnly,           // 이동만 Root Motion
            RotationAndPosition,    // 둘 다 Root Motion
            Disabled                // Root Motion 사용 안 함
        }

        public override void OnEnter(CharacterBrain brain)
        {
            _cachedBrain = brain;
            _startTime = Time.time;
            _rotationCompleted = false;
            _hasRootMotion = false;
            
            // Root Motion 데이터 초기화
            _rootMotionPositionDelta = Vector3.zero;
            _rootMotionRotationDelta = Quaternion.identity;

            // 목표 방향 결정
            _targetDirection = DetermineTargetDirection(brain);
            _targetRotation = Quaternion.LookRotation(_targetDirection, Vector3.up);
            _targetAngle = brain.GetData<float>("TurnAngle");
            
            Debug.Log($"[TurnInPlace] Angle: {_targetAngle:F1}°, Mode: {rootMotionMode}");

            // Root Motion 활성화
            if (rootMotionMode != RootMotionMode.Disabled)
            {
                brain.Animancer.Animator.applyRootMotion = true;
            }

            // 애니메이션 재생
            AnimKey turnKey = SelectTurnAnimation(brain);
            var anim = brain.AnimData.GetAnimation(turnKey);
            _animState = brain.Animancer.Play(anim, fadeDuration);

            // 애니메이션 이벤트
            if (_animState.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.OnEnd = OnAnimationComplete;
            }
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            if (_rotationCompleted) return;

            float elapsed = Time.time - _startTime;

            // 조기 종료 체크
            if (allowEarlyExit && elapsed > minAnimationTime)
            {
                if (ShouldExitEarly(brain))
                {
                    ExitToLocomotion();
                    return;
                }
            }

            // 회전 완료 체크
            if (IsRotationComplete(brain))
            {
                _rotationCompleted = true;
                if (snapToTargetOnComplete)
                {
                    brain.transform.rotation = _targetRotation;
                }
            }
        }

        public override void OnExit(CharacterBrain brain)
        {
            brain.Animancer.Animator.applyRootMotion = false;
            _rootMotionPositionDelta = Vector3.zero;
            _rootMotionRotationDelta = Quaternion.identity;
        }

        #region IMovementState Implementation

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime, CharacterBrain brain)
        {
            // 수평/수직 속도 분리
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, brain.Motor.CharacterUp);
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, brain.Motor.CharacterUp);

            if (brain.Motor.GroundingStatus.IsStableOnGround)
            {
                // Root Motion Position 처리
                if (rootMotionMode == RootMotionMode.PositionOnly || 
                    rootMotionMode == RootMotionMode.RotationAndPosition)
                {
                    if (_hasRootMotion && deltaTime > 0f)
                    {
                        // Root Motion의 Position Delta를 Velocity로 변환
                        Vector3 rootMotionVelocity = _rootMotionPositionDelta / deltaTime;
                        
                        // 수평 속도만 추출 (Y축 제외)
                        Vector3 rootMotionHorizontal = Vector3.ProjectOnPlane(rootMotionVelocity, Vector3.up);
                        
                        // Weight 적용
                        horizontalVelocity = rootMotionHorizontal * positionWeight;
                        
                        Debug.DrawRay(brain.transform.position, rootMotionHorizontal, Color.cyan, deltaTime);
                    }
                    else
                    {
                        // Root Motion이 없으면 감속
                        horizontalVelocity = Vector3.Lerp(
                            horizontalVelocity,
                            Vector3.zero,
                            1f - Mathf.Exp(-decelerationSpeed * deltaTime)
                        );
                    }
                }
                else
                {
                    // Root Motion을 사용하지 않으면 감속
                    horizontalVelocity = Vector3.Lerp(
                        horizontalVelocity,
                        Vector3.zero,
                        1f - Mathf.Exp(-decelerationSpeed * deltaTime)
                    );
                }

                // 지면에 안착
                verticalVelocity = Vector3.zero;
            }
            else
            {
                // 공중에서는 Root Motion 무시하고 기존 속도 유지
            }

            // Root Motion 데이터 초기화 (다음 프레임을 위해)
            _rootMotionPositionDelta = Vector3.zero;
            _hasRootMotion = false;

            // 최종 속도
            currentVelocity = horizontalVelocity + verticalVelocity;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime, CharacterBrain brain)
        {
            if (_rotationCompleted) return;

            // Root Motion Rotation 처리
            if (rootMotionMode == RootMotionMode.RotationOnly || 
                rootMotionMode == RootMotionMode.RotationAndPosition)
            {
                if (_hasRootMotion)
                {
                    // Root Motion의 Rotation을 적용
                    Quaternion rootMotionRotation = Quaternion.Slerp(
                        Quaternion.identity,
                        _rootMotionRotationDelta,
                        rotationWeight
                    );
                    
                    currentRotation = rootMotionRotation * currentRotation;
                    
                    // 추가 코드 보정 (1 - weight)
                    if (rotationWeight < 1f)
                    {
                        float codeWeight = 1f - rotationWeight;
                        currentRotation = RotateTowardsTarget(currentRotation, deltaTime, codeWeight);
                    }
                }
                else
                {
                    // Root Motion이 없으면 코드로 회전
                    currentRotation = RotateTowardsTarget(currentRotation, deltaTime, 1f);
                }
            }
            else
            {
                // Root Motion 미사용 시 코드로만 회전
                currentRotation = RotateTowardsTarget(currentRotation, deltaTime, 1f);
            }

            // Root Motion Rotation 초기화
            _rootMotionRotationDelta = Quaternion.identity;

            // 상하 회전 보정 (수평 유지)
            Vector3 currentUp = currentRotation * Vector3.up;
            if (Vector3.Angle(currentUp, Vector3.up) > 1f)
            {
                Vector3 smoothedUp = Vector3.Slerp(currentUp, Vector3.up, deltaTime * 10f);
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedUp) * currentRotation;
            }
        }

        #endregion

        #region Root Motion Callbacks

        // PlayerCharacterController에서 호출됨
        public void OnAnimatorMoveCallback(CharacterBrain brain)
        {
            //if (!enabled) return;
            if (brain.CurrentState != this) return;

            Animator animator = brain.Animancer.Animator;
            
            // Root Motion Delta 저장
            _rootMotionPositionDelta = animator.deltaPosition;
            _rootMotionRotationDelta = animator.deltaRotation;
            _hasRootMotion = true;
            
            // 디버그
            if (_rootMotionPositionDelta.sqrMagnitude > 0.001f)
            {
                Debug.Log($"[RootMotion] Pos: {_rootMotionPositionDelta}, Rot: {_rootMotionRotationDelta.eulerAngles}");
            }
        }

        #endregion

        #region Helper Methods

        private Quaternion RotateTowardsTarget(Quaternion current, float deltaTime, float weight)
        {
            if (weight <= 0f) return current;

            float maxDegrees = codeRotationSpeed * deltaTime * weight;
            return Quaternion.RotateTowards(current, _targetRotation, maxDegrees);
        }

        private Vector3 DetermineTargetDirection(CharacterBrain brain)
        {
            Vector3 currentInput = brain.InputDirection;
            if (currentInput.sqrMagnitude > 0.01f)
                return currentInput.normalized;

            Vector3 previousInput = brain.PreviousInputDirection;
            if (previousInput.sqrMagnitude > 0.01f)
                return previousInput.normalized;

            return -brain.transform.forward;
        }

        private bool ShouldExitEarly(CharacterBrain brain)
        {
            Vector3 currentInput = brain.InputDirection;
            if (currentInput.sqrMagnitude < 0.01f)
                return false;

            float angleToInput = Vector3.Angle(_targetDirection, currentInput);
            if (angleToInput > 90f)
                return true;

            float currentAngle = Vector3.Angle(brain.transform.forward, _targetDirection);
            return currentAngle < earlyExitAngleThreshold;
        }

        private bool IsRotationComplete(CharacterBrain brain)
        {
            float currentAngle = Vector3.Angle(brain.transform.forward, _targetDirection);
            return currentAngle < rotationAccuracy;
        }

        private void OnAnimationComplete()
        {
            ExitToLocomotion();
        }

        private void ExitToLocomotion()
        {
            if (_cachedBrain == null) return;

            if (snapToTargetOnComplete && !_rotationCompleted)
            {
                _cachedBrain.transform.rotation = _targetRotation;
            }

            _cachedBrain.ChangeState(_cachedBrain.DefaultState);
        }

        private AnimKey SelectTurnAnimation(CharacterBrain brain)
        {
            float lastSpeed = brain.GetData<float>("LastSpeed");
            float angle = _targetAngle;
            float absAngle = Mathf.Abs(angle);
            bool isRight = angle > 0;

            int speedLevel = lastSpeed switch
            {
                >= 9f => 2,
                >= 5f => 1,
                _ => 0
            };

            return speedLevel switch
            {
                2 => GetSprintTurnAnimation(absAngle, isRight),
                1 => GetRunTurnAnimation(absAngle, isRight),
                _ => GetWalkTurnAnimation(absAngle, isRight)
            };
        }

        private AnimKey GetSprintTurnAnimation(float absAngle, bool isRight) => absAngle switch
        {
            >= 135f => AnimKey.Sprint_Turn_180,
            >= 75f => isRight ? AnimKey.Sprint_Turn_R90 : AnimKey.Sprint_Turn_L90,
            _ => isRight ? AnimKey.Sprint_Turn_R45 : AnimKey.Sprint_Turn_L45
        };

        private AnimKey GetRunTurnAnimation(float absAngle, bool isRight) => absAngle switch
        {
            >= 135f => AnimKey.Run_Turn_180,
            >= 75f => isRight ? AnimKey.Run_Turn_R90 : AnimKey.Run_Turn_L90,
            _ => isRight ? AnimKey.Run_Turn_R45 : AnimKey.Run_Turn_L45
        };

        private AnimKey GetWalkTurnAnimation(float absAngle, bool isRight) => absAngle switch
        {
            >= 135f => AnimKey.Walk_Turn_180,
            >= 75f => isRight ? AnimKey.Walk_Turn_R90 : AnimKey.Walk_Turn_L90,
            _ => isRight ? AnimKey.Walk_Turn_R45 : AnimKey.Walk_Turn_L45
        };

        #endregion
    }
}