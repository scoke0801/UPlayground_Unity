using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 제자리 회전 상태 — Idle 중 카메라 방향과 캐릭터 전방의 각도 차이가 임계값을 초과할 때 진입.
    /// Stand_Idle_Turn_* 애니메이션을 재생하며, 애니메이션 진행도에 맞춰 목표 방향으로 회전한다.
    /// </summary>
    public class PlayerTurnInPlaceState : PlayerActorState
    {
        public override string StateName => "TurnInPlace";

        private readonly Vector3 _targetDirection;

        private Quaternion _startRotation;
        private Quaternion _targetRotation;

        public PlayerTurnInPlaceState(ActorMovementController controller, Vector3 targetDirection) : base(controller)
        {
            _targetDirection = targetDirection;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _startRotation  = motor.TransientRotation;
            _targetRotation = Quaternion.LookRotation(_targetDirection, motor.CharacterUp);

            float signedAngle = Vector3.SignedAngle(
                motor.CharacterForward, _targetDirection, motor.CharacterUp);

            var animKey   = GetTurnAnimKey(signedAngle);
            var animState = gameActor.Animator.PlayMotion(animKey, 0.1f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += TransitionToIdle;
            }
            else
            {
                // 클립 미등록 시 회전만 즉시 적용하고 Idle로
                playerController.TransitionToState(new PlayerIdleState(controller));
            }
        }

        public override void OnExit(GameActorState toState)
        {
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

            // 이동 입력이 들어오면 즉시 GroundMove
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
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 애니메이션 진행도(0→1)에 맞춰 Slerp — 클립 길이와 회전 속도가 자동으로 동기화됨
            float t = gameActor.Animator.GetNormalizedTime();
            currentRotation = Quaternion.Slerp(_startRotation, _targetRotation, t);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }

        private void TransitionToIdle()
        {
            playerController.TransitionToState(new PlayerIdleState(controller));
        }

        /// <summary>
        /// 부호 있는 각도(좌: 음수, 우: 양수)로 Stand_Idle_Turn AnimKey를 반환.
        /// 클립 종류: L45 / R45 / L90 / R90 / 180
        /// PlayerIdleState에서 HasMotion 체크에도 사용한다.
        /// </summary>
        internal static AnimKey GetTurnAnimKey(float signedAngle)
        {
            float abs    = Mathf.Abs(signedAngle);
            bool  isRight = signedAngle > 0f;

            // 67.5° 이하 → 45 클립
            if (abs < 67.5f)
                return isRight ? AnimKey.Stand_Idle_Turn_R45 : AnimKey.Stand_Idle_Turn_L45;

            // 135° 이하 → 90 클립
            if (abs < 135f)
                return isRight ? AnimKey.Stand_Idle_Turn_R90 : AnimKey.Stand_Idle_Turn_L90;

            // 135° 초과 → 180 클립
            return AnimKey.Stand_Idle_Turn_180;
        }
    }
}
