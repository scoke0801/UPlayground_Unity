using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 정지 모션 상태 — GroundMove에서 입력이 끊겼을 때 정지 애니메이션을 재생하고 Idle로 전환.
    /// 정지 직전 이동 방향(캐릭터 전방 기준 ±45°)에 따라 F / F_L45 / F_R45 클립을 선택한다.
    /// 해당 클립이 없으면 즉시 Idle로 이동한다.
    /// </summary>
    public class PlayerStopState : PlayerActorState
    {
        public override string StateName => "Stop";
        protected override ActorStateTag StateTagsCore => ActorStateTag.Locomotion;

        private readonly BaseMoveAnimType _moveAnimType;
        /// <summary> 정지 직전 이동 방향의 캐릭터 전방 기준 부호 있는 각도 (도) </summary>
        private readonly float _stopDirectionAngle;
        private MotionSet _playedMotionSet;
        private float _elapsed;
        private float _hardTimeout;
        private bool _motionCompleted;
        private bool _playFailed;

        /// <summary>
        /// <param name="stopDirection">정지 시점의 이동 방향 벡터 (월드)</param>
        /// </summary>
        public PlayerStopState(ActorMovementController controller, BaseMoveAnimType moveAnimType, Vector3 stopDirection)
            : base(controller)
        {
            _moveAnimType = moveAnimType;
            // 캐릭터 전방 기준 좌우 각도 계산 (진입 시 motor가 아직 유효)
            _stopDirectionAngle = Vector3.SignedAngle(
                controller.Motor.CharacterForward, stopDirection, controller.Motor.CharacterUp);
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 방향별 클립 시도 → 없으면 전방 클립 시도
            // (진입 자체는 PlayerGroundMoveState에서 HasMotion으로 보장되지만, L45/R45 클립이 없는 경우 fallback)
            var animKey = GetStopAnimKey(_moveAnimType, _stopDirectionAngle);
            var animState = gameActor.Animator.PlayMotion(animKey, 0.1f);

            if (animState == null)
            {
                var forwardKey = GetStopAnimKeyForward(_moveAnimType);
                animState = gameActor.Animator.PlayMotion(forwardKey, 0.1f);
            }

            if (animState != null)
            {
                _playedMotionSet = gameActor.Animator.CurrentMotionSet;
                float duration = _playedMotionSet?.TotalDuration ?? 0f;
                _hardTimeout = duration * 1.5f + 0.1f;
                gameActor.Animator.OnMotionSetEndedWithReason += OnMotionSetEnded;
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
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_playFailed || _motionCompleted || _elapsed >= _hardTimeout)
            {
                controller.TryTransitionToState(new PlayerIdleState(controller));
                return;
            }

            if (ShouldTransitionToAirborne(deltaTime))
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
                return;
            }

            // 이동 입력 복귀 시 즉시 GroundMove
            if (playerController.HasMoveInput())
            {
                playerController.TransitionToState(new PlayerGroundMoveState(controller));
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
                playerController.TransitionToState(new PlayerGuardState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Attack))
            {
                if (PlayerAttackState.TryEnter(playerController))
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Attack);
                    return;
                }
            }

            if (playerController.IsChargeAttackHeld())
            {
                playerController.TransitionToState(new PlayerChargeState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.HeavyAttack))
            {
                // TryEnter 가 OnEnter 안에서 HeavyAttack 입력을 소비한다.
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation * gameActor.Animator.RootMotionStepDeltaRotation;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = ActorVelocityUtility.ReplacePlanarPreserveVertical(
                gameActor.Animator.GetRootMotionStepVelocity(deltaTime),
                currentVelocity,
                motor.CharacterUp);
        }

        private void OnMotionSetEnded(MotionSet motionSet, MotionSetEndReason reason)
        {
            if (ReferenceEquals(motionSet, _playedMotionSet)
                && reason == MotionSetEndReason.Completed)
                _motionCompleted = true;
        }

        /// <summary>
        /// 이동 방향 각도(±45° 이내 → F, 45°~135° → L45/R45)에 따라 Stop UPlayGround.Gameplay.Tag.GameplayTag 반환.
        /// Sprint는 전방(F)과 ±45° 클립만 존재.
        /// PlayerGroundMoveState에서 HasMotion 체크에도 사용한다.
        /// </summary>
        internal static UPlayGround.Gameplay.Tag.GameplayTag GetStopAnimKey(BaseMoveAnimType moveAnimType, float stopDirectionAngle)
        {
            bool  isLeft = stopDirectionAngle < 0f;
            float abs    = Mathf.Abs(stopDirectionAngle);

            if (abs <= 45f)
                return GetStopAnimKeyForward(moveAnimType);

            return moveAnimType switch
            {
                BaseMoveAnimType.Walk   => isLeft ? UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Walking_L45   : UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Walking_R45,
                BaseMoveAnimType.Sprint => isLeft ? UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Sprinting_L45 : UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Sprinting_R45,
                _                       => isLeft ? UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Running_L45   : UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Running_R45,
            };
        }

        internal static UPlayGround.Gameplay.Tag.GameplayTag GetStopAnimKeyForward(BaseMoveAnimType moveAnimType)
        {
            return moveAnimType switch
            {
                BaseMoveAnimType.Walk   => UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Walking,
                BaseMoveAnimType.Sprint => UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Sprinting,
                _                       => UPlayGround.Data.Actor.Animation.MotionTags.Move_Stop_Running,
            };
        }
    }
}
