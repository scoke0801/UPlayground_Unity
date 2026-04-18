using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 추적 상태 - 타겟을 향해 이동
    /// </summary>
    public class EnemyChaseState : GameActorState
    {
        public override string StateName => "Chase";
        
        private EnemyBrain _brain;
        private EnemyDetection _detection;
        
        private float _chaseSpeed;
        private float _strafeSign; // +1 or -1, OnEnter마다 랜덤 결정
        private AnimKey _lastLocoKey = AnimKey.None;
        
        public EnemyChaseState(ActorMovementController controller, EnemyBrain brain, EnemyDetection detection) : base(controller)
        {
            _brain = brain;
            _detection = detection;
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _chaseSpeed = controller.MaxRunMoveSpeed * _brain.ChaseSpeedMultiplier;
            _strafeSign = Random.value > 0.5f ? 1f : -1f;
            _lastLocoKey = AnimKey.Run;
            gameActor.Animator.PlayMotion(AnimKey.Run, 0.25f);
            
            Debug.Log("[EnemyChaseState] 추적 시작");
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            Debug.Log("[EnemyChaseState] 추적 종료");
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
            if (!_detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            EnemyLocomotionHelper.UpdateAnim(gameActor, motor, ref _lastLocoKey,
                EnemyLocomotionHelper.LocoStyle.Run);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_detection.HasTarget)
            {
                // 타겟을 향해 회전
                Vector3 directionToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                directionToTarget.y = 0; // 수평 방향만
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        targetRotation,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_detection.HasTarget)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y       = 0;
            float dist       = toTarget.magnitude;

            // chaseStopDistance 이하면 제자리 정지 — Brain의 행동 결정 대기
            if (dist <= _brain.ChaseStopDistance)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            if (!motor.GroundingStatus.IsStableOnGround)
            {
                if (currentVelocity.sqrMagnitude > 0.01f)
                {
                    Vector3 airVelocity = currentVelocity;
                    airVelocity.y   = 0;
                    currentVelocity = airVelocity.normalized * Mathf.Min(airVelocity.magnitude, _chaseSpeed);
                }
                return;
            }

            Vector3 moveDir = toTarget.normalized;

            // chaseStopDistance의 1.5배 이내 진입 시 측면 이동 혼합 (직진 70% + 측면 30%)
            // 단조로운 직선 돌진을 막아 자연스러운 접근처럼 보이게 한다
            if (dist < _brain.ChaseStopDistance * 1.5f)
            {
                Vector3 strafeDir = Vector3.Cross(Vector3.up, moveDir) * _strafeSign;
                moveDir = (moveDir * 0.7f + strafeDir * 0.3f).normalized;
            }

            Vector3 targetVelocity = moveDir * _chaseSpeed;
            targetVelocity = motor.GetDirectionTangentToSurface(targetVelocity, motor.GroundingStatus.GroundNormal)
                             * targetVelocity.magnitude;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }
    }
}