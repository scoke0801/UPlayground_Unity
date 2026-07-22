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

        // ── 자연 낙하 유예 시스템 ──
        // KCC의 SnappingPrevented(MaxVelocityForLedgeSnap=0) 시 프로브 거리가 0.005m로
        // 급감하여 FoundAnyGround가 false가 되는 문제를 우회하기 위해,
        // KCC 프로브와 독립적인 자체 Raycast로 근접 지면을 탐지한다.

        /// <summary> 지면 이탈 후 Airborne 전환까지 유예 시간 (초). </summary>
        protected virtual float AirborneGracePeriod => 0.2f;

        private float _unstableTimer;

        // 자체 지면 탐지 파라미터
        private const float GroundCheckOriginOffset = 0.1f;  // 발 위치에서 위로 오프셋
        private const float GroundCheckDistance = 0.6f;       // 발 아래 탐지 거리

        /// <summary>
        /// NATURAL(지형 이탈) 전환 시 사용.
        /// 자체 Raycast 지면 탐지 + 유예 시간으로 판정.
        /// 점프/넉백 등 의도적/강제 전환에는 사용하지 않는다.
        /// </summary>
        protected bool ShouldTransitionToAirborne(float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                _unstableTimer = 0f;
                return false;
            }

            // 자체 Raycast로 발 아래 지면 확인 (KCC 프로브 거리 축소 문제 우회)
            if (CheckGroundNearby())
            {
                _unstableTimer = 0f;
                return false;
            }

            _unstableTimer += deltaTime;
            return _unstableTimer >= AirborneGracePeriod;
        }

        private bool CheckGroundNearby()
        {
            Vector3 origin = motor.TransientPosition + motor.CharacterUp * GroundCheckOriginOffset;
            return Physics.Raycast(
                origin,
                -motor.CharacterUp,
                GroundCheckDistance + GroundCheckOriginOffset,
                motor.CollidableLayers & motor.StableGroundLayers,
                QueryTriggerInteraction.Ignore);
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            playerActor = gameActor as PlayerActor;
            _unstableTimer = 0f;
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