using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 이동 중 급격한 방향 전환 상태.
    /// PlayerGroundMoveState에서 CharacterForward와 MoveInputVector의 각도 차가
    /// 임계값을 초과할 때 진입하며, Run/Walk/Sprint_F_Turn_* 애니메이션을 재생한다.
    /// 완료 후 이동 입력이 있으면 GroundMove, 없으면 Idle로 복귀.
    /// </summary>
    public class PlayerTurnInPlaceState : PlayerActorState
    {
        public override string StateName => "TurnInPlace";

        private readonly BaseMoveAnimType _moveAnimType;
        private readonly Vector3          _targetDirection;

        public PlayerTurnInPlaceState(
            ActorMovementController controller,
            BaseMoveAnimType moveAnimType,
            Vector3 targetDirection) : base(controller)
        {
            _moveAnimType    = moveAnimType;
            _targetDirection = targetDirection;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Animator.ApplyRootMotion(true);

            float signedAngle = Vector3.SignedAngle(
                motor.CharacterForward, _targetDirection, motor.CharacterUp);

            var animKey   = GetTurnAnimKey(_moveAnimType, signedAngle);
            var animState = gameActor.Animator.PlayMotion(animKey, 0.1f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnTurnComplete;
            }
            else
            {
                // 클립 미등록 → 즉시 GroundMove 복귀
                playerController.TransitionToState(new PlayerGroundMoveState(controller));
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.ApplyRootMotion(false);
            gameActor.Animator.OnMotionSetCompleted -= OnTurnComplete;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
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
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 루트모션 델타 회전을 누적 적용
            currentRotation = currentRotation * gameActor.Animator.DeltaRotation;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 루트모션 델타 위치로 속도 결정 (DodgeState와 동일한 패턴)
            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
        }

        private void OnTurnComplete()
        {
            if (playerController.HasMoveInput())
                playerController.TransitionToState(new PlayerGroundMoveState(controller));
            else
                playerController.TransitionToState(new PlayerIdleState(controller));
        }

        /// <summary>
        /// 이동 타입과 부호 있는 각도(좌:음수, 우:양수)로 적절한 Turn AnimKey를 반환.
        /// PlayerGroundMoveState의 HasMotion 체크에서도 사용한다.
        /// </summary>
        internal static AnimKey GetTurnAnimKey(BaseMoveAnimType moveAnimType, float signedAngle)
        {
            float abs    = Mathf.Abs(signedAngle);
            bool  isRight = signedAngle > 0f;

            return moveAnimType switch
            {
                BaseMoveAnimType.Walk =>
                    abs < 67.5f ? (isRight ? AnimKey.Walk_Turn_R45 : AnimKey.Walk_Turn_L45) :
                    abs < 135f  ? (isRight ? AnimKey.Walk_Turn_R90 : AnimKey.Walk_Turn_L90) :
                                   AnimKey.Walk_Turn_180,

                BaseMoveAnimType.Sprint =>
                    abs < 67.5f ? (isRight ? AnimKey.Sprint_Turn_R45 : AnimKey.Sprint_Turn_L45) :
                    abs < 135f  ? (isRight ? AnimKey.Sprint_Turn_R90 : AnimKey.Sprint_Turn_L90) :
                                   AnimKey.Sprint_Turn_180,

                _ => // Run (기본)
                    abs < 67.5f ? (isRight ? AnimKey.Run_Turn_R45 : AnimKey.Run_Turn_L45) :
                    abs < 135f  ? (isRight ? AnimKey.Run_Turn_R90 : AnimKey.Run_Turn_L90) :
                                   AnimKey.Run_Turn_180,
            };
        }
    }
}
