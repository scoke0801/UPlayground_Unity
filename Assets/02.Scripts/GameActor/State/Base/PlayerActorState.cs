using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 모든 Actor 이동 상태의 베이스 클래스
    /// </summary>
    public abstract class PlayerActorState : GameActorState
    {
        protected PlayerMovementController playerController;
        protected PlayerActor playerActor;

        /// <summary>
        /// 이 상태가 진입 시 반드시 재생해야 하는 모션 키.
        /// null 이면 모션 보유 여부와 무관하게 진입 가능.
        /// DashAttack / JumpAttack 처럼 전용 모션이 없으면 의미가 없는 상태에서 오버라이드한다.
        /// </summary>
        protected virtual UPlayGround.Gameplay.Tag.GameplayTag? RequiredMotionKey => null;

        /// <summary>
        /// 액터가 RequiredMotionKey 에 해당하는 모션을 보유하고 있는지 확인.
        /// 상태 전이 가드(<see cref="CanTransitionState"/>) 에서 호출.
        /// </summary>
        protected bool HasRequiredMotion()
        {
            if (RequiredMotionKey.HasValue == false) return true;

            var animator = gameActor?.Animator;
            if (animator == null) return false;

            return animator.HasMotion(RequiredMotionKey.Value, true);
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            playerActor = gameActor as PlayerActor;
        }

        protected PlayerActorState(ActorMovementController controller) : base(controller)
        {
            playerController = controller as PlayerMovementController;
        }

        /// <summary>
        /// 이동 입력 방향을 캐릭터 기준 4방향(F/B/L/R)으로 분류해 해당 모션 키를 반환.
        /// 매칭되는 모션이 없거나 입력이 없으면 fallbackKey 반환.
        /// </summary>
        protected UPlayGround.Gameplay.Tag.GameplayTag ResolveDirectionalMotionKey(
            UPlayGround.Gameplay.Tag.GameplayTag forwardKey, UPlayGround.Gameplay.Tag.GameplayTag backKey, UPlayGround.Gameplay.Tag.GameplayTag leftKey, UPlayGround.Gameplay.Tag.GameplayTag rightKey, UPlayGround.Gameplay.Tag.GameplayTag fallbackKey)
        {
            var animator = gameActor?.Animator;
            if (animator == null)
                return fallbackKey;

            if (playerController == null || playerController.HasMoveInput() == false)
                return animator.HasMotion(forwardKey, true) ? forwardKey : fallbackKey;

            Vector3 input = playerController.MoveInputVector.normalized;
            float forwardDot = Vector3.Dot(input, motor.CharacterForward);
            float rightDot   = Vector3.Dot(input, motor.CharacterRight);

            UPlayGround.Gameplay.Tag.GameplayTag candidate = Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot)
                ? (forwardDot >= 0f ? forwardKey : backKey)
                : (rightDot   >= 0f ? rightKey   : leftKey);

            return animator.HasMotion(candidate, true) ? candidate : fallbackKey;
        }
    }
}
