using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 적 지상 상태의 공통 베이스.
    /// KCC 접지 판정이 짧게 흔들리는 프레임을 자연 낙하로 오인하지 않도록 보정한다.
    /// </summary>
    public abstract class EnemyActorState : GameActorState
    {
        /// <summary> 지면 이탈 후 Airborne 전환까지 유예 시간 (초). </summary>
        protected virtual float AirborneGracePeriod => 0.15f;

        private float _unstableTimer;

        private const float GroundCheckOriginOffset = 0.1f;
        private const float GroundCheckDistance = 0.6f;

        protected EnemyActorState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _unstableTimer = 0f;

            // 진입한 상태를 몬스터의 현재 리액션 상태로 반영한다(통합 취약 배율 산출용).
            // 모든 지상 적 상태가 이 베이스를 거치므로, 일반 상태로 복귀하면 자동으로 None으로 초기화된다.
            if (gameActor is MonsterActor monster)
                monster.SetCurrentReactionState(MapReactionState(StateName));
        }

        // StateName → 행동불능 리액션 상태. 비-리액션 상태(Idle/Chase/Attack 등)는 모두 None으로 떨어진다.
        private static CombatReactionState MapReactionState(string stateName)
            => stateName switch
            {
                "Hit" => CombatReactionState.Hit,
                "Stun" => CombatReactionState.Stun,
                "Knockdown" => CombatReactionState.Knockdown,
                "Airborne" => CombatReactionState.Airborne,
                "Grabbed" => CombatReactionState.Grabbed,
                _ => CombatReactionState.None,
            };

        /// <summary>
        /// 지형 이탈로 인한 자연 Airborne 전환 판정.
        /// 피격 런치처럼 의도적으로 띄우는 전환에는 사용하지 않는다.
        /// </summary>
        protected bool ShouldTransitionToAirborne(float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                _unstableTimer = 0f;
                return false;
            }

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
    }
}
