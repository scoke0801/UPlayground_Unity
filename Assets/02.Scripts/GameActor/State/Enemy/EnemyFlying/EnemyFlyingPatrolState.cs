using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 순찰. 기존 EnemyPatrolState 로직을 EnemyFlyingAIController 참조로 재구현.
    /// </summary>
    public class EnemyFlyingPatrolState : GameActorState
    {
        public override string StateName => "Flying_Patrol";

        private readonly EnemyFlyingAIContext _brain;
        private Vector3 _targetPos;
        private float _patrolSpeed;
        private float _waitTimer;
        private bool _isWaiting;
        private float _stuckTimer;
        private Vector3 _lastPos;

        private const float ArrivalDist = 0.5f;
        private const float StuckTimeout = 2.0f;

        public EnemyFlyingPatrolState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller) { _brain = brain; }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            motor.SetGroundSolvingActivation(true);
            _patrolSpeed = controller.MaxRunMoveSpeed * 0.5f;
            _isWaiting = false;
            _waitTimer = 0f;
            _stuckTimer = 0f;
            _lastPos = motor.TransientPosition;
            SetNewTarget();
            gameActor.Animator.PlayMotion(AnimKey.Walk, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            // 타겟 발견 이후의 전환은 BT가 처리
            if (_brain.Detection.HasTarget) return;

            if (_isWaiting)
            {
                _waitTimer += deltaTime;
                if (_waitTimer >= _brain.PatrolWaitTime)
                {
                    _isWaiting = false;
                    SetNewTarget();
                    gameActor.Animator.PlayMotion(AnimKey.Walk, 0.25f);
                }
            }
            else
            {
                float dist = Vector3.Distance(
                    new Vector3(motor.TransientPosition.x, 0, motor.TransientPosition.z),
                    new Vector3(_targetPos.x, 0, _targetPos.z));

                if (dist <= ArrivalDist)
                {
                    StartWait();
                    return;
                }

                _stuckTimer += deltaTime;
                if (_stuckTimer >= StuckTimeout)
                {
                    float moved = Vector3.Distance(
                        new Vector3(motor.TransientPosition.x, 0, motor.TransientPosition.z),
                        new Vector3(_lastPos.x, 0, _lastPos.z));
                    if (moved < 0.2f) StartWait();
                    else { _lastPos = motor.TransientPosition; _stuckTimer = 0f; }
                }
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_isWaiting) return;
            Vector3 dir = (_targetPos - motor.TransientPosition); dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                currentRotation = Quaternion.Slerp(currentRotation,
                    Quaternion.LookRotation(dir.normalized),
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_isWaiting || !motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }
            Vector3 dir = (_targetPos - motor.TransientPosition); dir.y = 0;
            Vector3 vel = dir.normalized * _patrolSpeed;
            vel = motor.GetDirectionTangentToSurface(vel, motor.GroundingStatus.GroundNormal) * vel.magnitude;
            currentVelocity = Vector3.Lerp(currentVelocity, vel,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        private void StartWait()
        {
            _isWaiting = true;
            _waitTimer = 0f;
            gameActor.Animator.PlayMotion(AnimKey.Idle, 0.25f);
        }

        private void SetNewTarget()
        {
            _targetPos = _brain.GetRandomPatrolPoint();
            _targetPos.y = motor.TransientPosition.y;
            _lastPos = motor.TransientPosition;
            _stuckTimer = 0f;
        }
    }
}
