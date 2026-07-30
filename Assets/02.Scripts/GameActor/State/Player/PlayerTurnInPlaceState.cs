using UnityEngine;
using KinematicCharacterController;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 정지 또는 이동 중 큰 방향 전환 상태.
    /// Idle에서는 카메라 정면과의 각도 차로 Stand_Idle_Turn_*을 재생하고,
    /// GroundMove에서는 입력 방향이 임계값을 초과할 때 Run/Walk/Sprint_F_Turn_*을 재생한다.
    /// 완료 후 이동 입력이 있으면 GroundMove, 없으면 Idle로 복귀.
    /// </summary>
    public class PlayerTurnInPlaceState : PlayerActorState
    {
        public override string StateName => "TurnInPlace";
        protected override ActorStateTag StateTagsCore => ActorStateTag.Locomotion;

        private readonly BaseMoveAnimType _moveAnimType;
        private readonly Vector3          _targetDirection;
        private readonly Vector3          _turnSourceDirection;
        private readonly bool             _useIdleTurn;
        private MotionSet _playedMotionSet;
        private float _elapsed;
        private float _duration;
        private float _hardTimeout;
        private float _rotationScale = 1f;
        private bool _motionCompleted;
        private bool _playFailed;
        private bool _wallBlocked;
        private bool _alignRotationOnExit;
        public float RequiredYaw { get; private set; }
        public float ClipTotalYaw { get; private set; }
        public float RotationScale => _rotationScale;

        public PlayerTurnInPlaceState(
            ActorMovementController controller,
            BaseMoveAnimType moveAnimType,
            Vector3 targetDirection,
            bool useIdleTurn = false,
            Vector3 turnSourceDirection = default) : base(controller)
        {
            _moveAnimType    = moveAnimType;
            _targetDirection = targetDirection;
            _useIdleTurn     = useIdleTurn;
            _turnSourceDirection = turnSourceDirection;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            Vector3 sourceDirection = Vector3.ProjectOnPlane(
                _turnSourceDirection,
                motor.CharacterUp);
            if (sourceDirection.sqrMagnitude <= 0.0001f)
                sourceDirection = motor.CharacterForward;
            else
                sourceDirection.Normalize();

            float signedAngle = Vector3.SignedAngle(
                sourceDirection, _targetDirection, motor.CharacterUp);

            var animKey = _useIdleTurn
                ? GetIdleTurnAnimKey(signedAngle)
                : GetTurnAnimKey(_moveAnimType, signedAngle);
            var animState = gameActor.Animator.PlayMotion(animKey, 0.1f);
            if (animState != null)
            {
                _playedMotionSet = gameActor.Animator.CurrentMotionSet;
                _duration = _playedMotionSet?.TotalDuration ?? 0f;
                _hardTimeout = _duration * 1.5f + 0.1f;
                gameActor.Animator.OnMotionSetEndedWithReason += OnMotionSetEnded;

                RequiredYaw = Mathf.Abs(signedAngle);
                if (gameActor.Animator.MotionSet != null
                    && gameActor.Animator.MotionSet.TryGetMotionRootYaw(
                        animKey,
                        out float clipTotalYaw))
                {
                    ClipTotalYaw = Mathf.Abs(clipTotalYaw);
                    float min = Mathf.Min(
                        controller.TurnRotationScaleMin,
                        controller.TurnRotationScaleMax);
                    float max = Mathf.Max(
                        controller.TurnRotationScaleMin,
                        controller.TurnRotationScaleMax);
                    _rotationScale = Mathf.Clamp(
                        RequiredYaw / ClipTotalYaw,
                        min,
                        max);
                }
                else
                {
                    Debug.LogWarning(
                        $"[{gameActor.name}] Turn 루트 yaw가 베이크되지 않았습니다: {animKey}. 회전 배율 1.0을 사용합니다.",
                        gameActor);
                }
            }
            else
            {
                // OnEnter 중 재전환하지 않고 다음 UpdateState에서 안전하게 이탈한다.
                _playFailed = true;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;
            playerController.StartTurnReentryCooldown();
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_playFailed || _wallBlocked)
            {
                TransitionToLocomotionResult();
                return;
            }
            if (_motionCompleted || _elapsed >= _hardTimeout)
            {
                _alignRotationOnExit = true;
                TransitionToLocomotionResult();
                return;
            }

            if (ShouldTransitionToAirborne(deltaTime))
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
                return;
            }

            if (playerController.HasJumpInput())
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
                return;
            }

            if (playerController.HasDodgeInput())
            {
                playerController.TransitionToState(new PlayerDodgeState(playerController));
                return;
            }

            if (playerController.HasDashInput())
            {
                if (controller.TryTransitionToState(new PlayerDashState(controller)))
                    return;
            }

            if (playerController.HasGuardInput())
            {
                if (controller.TryTransitionToState(new PlayerGuardState(playerController)))
                    return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Attack)
                && PlayerAttackState.TryEnter(playerController))
                return;

            if (playerController.IsChargeAttackHeld())
            {
                if (controller.TryTransitionToState(new PlayerChargeState(playerController)))
                    return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.HeavyAttack)
                && PlayerAttackState.TryEnter(playerController))
                return;

            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (skillGauge == null)
                    break;
                if (!playerController.HasSkillInput(i) || !skillGauge.CanUseSkill(i))
                    continue;
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }

            // 최소 체류 뒤 입력이 목표 방향에서 크게 벗어나면 GroundMove에서 재평가한다.
            if (playerController.HasMoveInput())
            {
                float angleToTarget = Vector3.Angle(playerController.MoveInputVector, _targetDirection);
                if (_elapsed >= controller.TurnMinDuration
                    && angleToTarget > controller.TurnAbortAngle)
                {
                    if (controller.TryTransitionToState(new PlayerGroundMoveState(controller)))
                        return;
                }
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Quaternion scaledRootRotation = Quaternion.SlerpUnclamped(
                Quaternion.identity,
                gameActor.Animator.RootMotionStepDeltaRotation,
                _rotationScale);
            currentRotation *= scaledRootRotation;

            float normalizedTime = _duration > 0.001f
                ? Mathf.Clamp01(_elapsed / _duration)
                : 1f;
            if (normalizedTime > 0.85f)
            {
                Vector3 planarTarget = Vector3.ProjectOnPlane(
                    _targetDirection,
                    motor.CharacterUp);
                if (planarTarget.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(
                        planarTarget.normalized,
                        motor.CharacterUp);
                    float correction = Mathf.InverseLerp(0.85f, 1f, normalizedTime);
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        targetRotation,
                        correction);
                }
            }
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 루트모션 델타 위치로 속도 결정 (DodgeState와 동일한 패턴)
            currentVelocity = ActorVelocityUtility.ReplacePlanarPreserveVertical(
                gameActor.Animator.GetRootMotionStepVelocity(deltaTime),
                currentVelocity,
                motor.CharacterUp);
        }

        public override void OnMovementHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            Vector3 planarNormal = Vector3.ProjectOnPlane(hitNormal, motor.CharacterUp);
            if (planarNormal.sqrMagnitude > 0.0001f
                && Vector3.Dot(planarNormal.normalized, _targetDirection) < -0.25f)
                _wallBlocked = true;
        }

        private void OnMotionSetEnded(MotionSet motionSet, MotionSetEndReason reason)
        {
            if (ReferenceEquals(motionSet, _playedMotionSet)
                && reason == MotionSetEndReason.Completed)
                _motionCompleted = true;
        }

        private void TransitionToLocomotionResult()
        {
            GameActorState nextState = playerController.HasMoveInput()
                ? new PlayerGroundMoveState(controller)
                : new PlayerIdleState(controller);

            Vector3 completionDirection = _alignRotationOnExit
                ? ResolveCompletionDirection()
                : Vector3.zero;
            if (!controller.TryTransitionToState(nextState))
                return;

            if (completionDirection.sqrMagnitude > 0.0001f)
            {
                controller.SeedRotationNextUpdate(
                    Quaternion.LookRotation(completionDirection, motor.CharacterUp));
            }
        }

        private Vector3 ResolveCompletionDirection()
        {
            Vector3 direction = playerController.HasMoveInput()
                ? playerController.MoveInputVector
                : _targetDirection;
            direction = Vector3.ProjectOnPlane(direction, motor.CharacterUp);
            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.zero;
        }

        /// <summary>
        /// 이동 타입과 부호 있는 각도(좌:음수, 우:양수)로 적절한 Turn UPlayGround.Gameplay.Tag.GameplayTag를 반환.
        /// PlayerGroundMoveState의 HasMotion 체크에서도 사용한다.
        /// </summary>
        internal static UPlayGround.Gameplay.Tag.GameplayTag GetTurnAnimKey(BaseMoveAnimType moveAnimType, float signedAngle)
        {
            float abs    = Mathf.Abs(signedAngle);
            bool  isRight = signedAngle > 0f;

            return moveAnimType switch
            {
                BaseMoveAnimType.Walk =>
                    abs < 67.5f ? (isRight ? UPlayGround.Data.Actor.Animation.MotionTags.Walk_Turn_R45 : UPlayGround.Data.Actor.Animation.MotionTags.Walk_Turn_L45) :
                    abs < 135f  ? (isRight ? UPlayGround.Data.Actor.Animation.MotionTags.Walk_Turn_R90 : UPlayGround.Data.Actor.Animation.MotionTags.Walk_Turn_L90) :
                                   UPlayGround.Data.Actor.Animation.MotionTags.Walk_Turn_180,

                BaseMoveAnimType.Sprint =>
                    abs < 67.5f ? (isRight ? UPlayGround.Data.Actor.Animation.MotionTags.Sprint_Turn_R45 : UPlayGround.Data.Actor.Animation.MotionTags.Sprint_Turn_L45) :
                    abs < 135f  ? (isRight ? UPlayGround.Data.Actor.Animation.MotionTags.Sprint_Turn_R90 : UPlayGround.Data.Actor.Animation.MotionTags.Sprint_Turn_L90) :
                                   UPlayGround.Data.Actor.Animation.MotionTags.Sprint_Turn_180,

                _ => // Run (기본)
                    abs < 67.5f ? (isRight ? UPlayGround.Data.Actor.Animation.MotionTags.Run_Turn_R45 : UPlayGround.Data.Actor.Animation.MotionTags.Run_Turn_L45) :
                    abs < 135f  ? (isRight ? UPlayGround.Data.Actor.Animation.MotionTags.Run_Turn_R90 : UPlayGround.Data.Actor.Animation.MotionTags.Run_Turn_L90) :
                                   UPlayGround.Data.Actor.Animation.MotionTags.Run_Turn_180,
            };
        }

        internal static UPlayGround.Gameplay.Tag.GameplayTag GetIdleTurnAnimKey(float signedAngle)
        {
            float abs = Mathf.Abs(signedAngle);
            bool isRight = signedAngle > 0f;

            if (abs < 67.5f)
            {
                return isRight
                    ? UPlayGround.Data.Actor.Animation.MotionTags.Stand_Idle_Turn_R45
                    : UPlayGround.Data.Actor.Animation.MotionTags.Stand_Idle_Turn_L45;
            }

            if (abs < 135f)
            {
                return isRight
                    ? UPlayGround.Data.Actor.Animation.MotionTags.Stand_Idle_Turn_R90
                    : UPlayGround.Data.Actor.Animation.MotionTags.Stand_Idle_Turn_L90;
            }

            return UPlayGround.Data.Actor.Animation.MotionTags.Stand_Idle_Turn_180;
        }
    }
}
