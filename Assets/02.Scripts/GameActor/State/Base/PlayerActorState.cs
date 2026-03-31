using KinematicCharacterController;
using UnityEngine;
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

            // Foot IK 기본 비활성 (Idle, GroundMove, Crouch에서만 활성화)
            playerActor?.FootIK?.SetIKActive(false);
        }

        protected PlayerActorState(ActorMovementController controller) : base(controller)
        {
            playerController = controller as PlayerMovementController;
        }
    }
}