using UnityEngine;
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

        private readonly BaseMoveAnimType _moveAnimType;
        /// <summary> 정지 직전 이동 방향의 캐릭터 전방 기준 부호 있는 각도 (도) </summary>
        private readonly float _stopDirectionAngle;

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
            gameActor.Animator.ApplyRootMotion(true);

            // 방향별 클립 시도 → 없으면 전방 클립 시도
            // (진입 자체는 PlayerGroundMoveState에서 HasMotion으로 보장되지만, L45/R45 클립이 없는 경우 fallback)
            var animKey   = GetStopAnimKey(_moveAnimType, _stopDirectionAngle);
            var animState = gameActor.Animator.PlayMotion(animKey, 0.1f);

            if (animState == null)
            {
                var forwardKey = GetStopAnimKeyForward(_moveAnimType);
                animState = gameActor.Animator.PlayMotion(forwardKey, 0.1f);
            }

            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += TransitionToIdle;
            }
            else
            {
                // 이론상 도달 불가 (PlayerGroundMoveState에서 사전 체크), 안전장치
                playerController.TransitionToState(new PlayerIdleState(controller));
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.ApplyRootMotion(false);
            gameActor.Animator.OnMotionSetCompleted -= TransitionToIdle;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
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

            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
            {
                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
            }

            if (playerController.IsChargeAttackHeld())
            {
                playerController.TransitionToState(new PlayerChargeState(playerController));
                return;
            }

            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
            {
                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation * gameActor.Animator.DeltaRotation;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
        }

        private void TransitionToIdle()
        {
            playerController.TransitionToState(new PlayerIdleState(controller));
        }

        /// <summary>
        /// 이동 방향 각도(±45° 이내 → F, 45°~135° → L45/R45)에 따라 Stop AnimKey 반환.
        /// Sprint는 전방(F)과 ±45° 클립만 존재.
        /// PlayerGroundMoveState에서 HasMotion 체크에도 사용한다.
        /// </summary>
        internal static AnimKey GetStopAnimKey(BaseMoveAnimType moveAnimType, float stopDirectionAngle)
        {
            bool  isLeft = stopDirectionAngle < 0f;
            float abs    = Mathf.Abs(stopDirectionAngle);

            if (abs <= 45f)
                return GetStopAnimKeyForward(moveAnimType);

            return moveAnimType switch
            {
                BaseMoveAnimType.Walk   => isLeft ? AnimKey.Move_Stop_Walking_L45   : AnimKey.Move_Stop_Walking_R45,
                BaseMoveAnimType.Sprint => isLeft ? AnimKey.Move_Stop_Sprinting_L45 : AnimKey.Move_Stop_Sprinting_R45,
                _                       => isLeft ? AnimKey.Move_Stop_Running_L45   : AnimKey.Move_Stop_Running_R45,
            };
        }

        internal static AnimKey GetStopAnimKeyForward(BaseMoveAnimType moveAnimType)
        {
            return moveAnimType switch
            {
                BaseMoveAnimType.Walk   => AnimKey.Move_Stop_Walking,
                BaseMoveAnimType.Sprint => AnimKey.Move_Stop_Sprinting,
                _                       => AnimKey.Move_Stop_Running,
            };
        }
    }
}
