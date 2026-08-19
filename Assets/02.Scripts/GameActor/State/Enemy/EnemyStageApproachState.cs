using System;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public enum EnemyStageApproachResult
    {
        Arrived,
        TimedOut,
        TargetLost,
        Interrupted,
    }

    /// <summary>전투 판단과 분리된 연출 이동의 목표와 완료 처리를 전달한다.</summary>
    public readonly struct EnemyStageApproachContext
    {
        public EnemyStageApproachContext(
            Transform target,
            float stopDistance,
            float speedMultiplier,
            float timeoutSeconds,
            Action<EnemyStageApproachResult> onCompleted)
        {
            Target = target;
            StopDistance = Mathf.Max(0.1f, stopDistance);
            SpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            TimeoutSeconds = Mathf.Max(0.1f, timeoutSeconds);
            OnCompleted = onCompleted;
        }

        public Transform Target { get; }
        public float StopDistance { get; }
        public float SpeedMultiplier { get; }
        public float TimeoutSeconds { get; }
        public Action<EnemyStageApproachResult> OnCompleted { get; }
    }

    /// <summary>대화·컷신 전환에서 몬스터가 순간이동하지 않고 목표까지 직접 걷게 한다.</summary>
    public sealed class EnemyStageApproachState : EnemyActorState,
        IConfigurableState<EnemyStageApproachContext>
    {
        private Transform _target;
        private Action<EnemyStageApproachResult> _onCompleted;
        private float _stopDistance;
        private float _moveSpeed;
        private float _timeoutSeconds;
        private float _elapsedSeconds;
        private bool _isCompleted;

        public EnemyStageApproachState(ActorMovementController controller)
            : base(controller)
        {
        }

        public override ActorStateId StateId => ActorStateId.StageApproach;
        public override bool BlocksBehaviorTree => true;
        protected override ActorStateTag StateTagsCore => ActorStateTag.Locomotion;

        public void Configure(in EnemyStageApproachContext context)
        {
            _target = context.Target;
            _stopDistance = context.StopDistance;
            _moveSpeed = controller.MaxRunMoveSpeed * context.SpeedMultiplier;
            _timeoutSeconds = context.TimeoutSeconds;
            _onCompleted = context.OnCompleted;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _elapsedSeconds = 0f;
            _isCompleted = false;
            gameActor.Animator.PlayMotion(MotionTags.Run, 0.2f);
        }

        public override void UpdateState(float deltaTime)
        {
            _elapsedSeconds += deltaTime;
            if (_target == null)
            {
                Complete(EnemyStageApproachResult.TargetLost);
                return;
            }

            if (IsWithinStopDistance())
            {
                Complete(EnemyStageApproachResult.Arrived);
                return;
            }

            if (_elapsedSeconds >= _timeoutSeconds)
                Complete(EnemyStageApproachResult.TimedOut);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!TryGetPlanarDirection(out Vector3 direction))
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, motor.CharacterUp);
            currentRotation = Quaternion.Slerp(
                currentRotation,
                targetRotation,
                1f - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!TryGetPlanarDirection(out Vector3 direction) || IsWithinStopDistance())
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 targetVelocity = direction * _moveSpeed;
            if (motor.GroundingStatus.IsStableOnGround)
            {
                targetVelocity = motor.GetDirectionTangentToSurface(
                    targetVelocity,
                    motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;
            }

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public override void OnExit(GameActorState toState)
        {
            if (!_isCompleted)
                NotifyCompleted(EnemyStageApproachResult.Interrupted);
            base.OnExit(toState);
        }

        private bool TryGetPlanarDirection(out Vector3 direction)
        {
            direction = Vector3.zero;
            if (_target == null)
                return false;

            direction = Vector3.ProjectOnPlane(
                _target.position - motor.TransientPosition,
                motor.CharacterUp);
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            direction.Normalize();
            return true;
        }

        private bool IsWithinStopDistance()
        {
            if (_target == null)
                return false;

            Vector3 offset = Vector3.ProjectOnPlane(
                _target.position - motor.TransientPosition,
                motor.CharacterUp);
            return offset.sqrMagnitude <= _stopDistance * _stopDistance;
        }

        private void Complete(EnemyStageApproachResult result)
        {
            if (_isCompleted)
                return;

            _isCompleted = true;
            Action<EnemyStageApproachResult> callback = TakeCompletionCallback();
            controller.TryTransitionToState(ActorStateId.Idle);
            callback?.Invoke(result);
        }

        private void NotifyCompleted(EnemyStageApproachResult result)
        {
            _isCompleted = true;
            Action<EnemyStageApproachResult> callback = TakeCompletionCallback();
            callback?.Invoke(result);
        }

        private Action<EnemyStageApproachResult> TakeCompletionCallback()
        {
            Action<EnemyStageApproachResult> callback = _onCompleted;
            _onCompleted = null;
            return callback;
        }
    }
}
