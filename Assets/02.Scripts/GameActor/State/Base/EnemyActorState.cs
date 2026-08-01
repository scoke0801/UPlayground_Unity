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
        protected override float AirborneGracePeriod => 0.15f;

        protected EnemyActorState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            // 진입한 상태를 몬스터의 현재 리액션 상태로 반영한다(통합 취약 배율 산출용).
            // 모든 지상 적 상태가 이 베이스를 거치므로, 일반 상태로 복귀하면 자동으로 None으로 초기화된다.
            if (gameActor is MonsterActor monster)
                monster.SetCurrentReactionState(MapReactionState(StateId));
        }

        // StateId → 행동불능 리액션 상태. 비-리액션 상태는 모두 None으로 떨어진다.
        private static CombatReactionState MapReactionState(ActorStateId stateId)
            => stateId switch
            {
                ActorStateId.Hit => CombatReactionState.Hit,
                ActorStateId.Stun => CombatReactionState.Stun,
                ActorStateId.Knockdown => CombatReactionState.Knockdown,
                ActorStateId.Airborne => CombatReactionState.Airborne,
                ActorStateId.Grabbed => CombatReactionState.Grabbed,
                _ => CombatReactionState.None,
            };
    }
}
