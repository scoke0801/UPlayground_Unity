using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 발자국 이벤트 (지형별 사운드)
    /// </summary>
    [Serializable]
    public class FinishAttackEvent : MotionEventBase
    {
        public override string GetDisplayName() => "FinishAttack";

        public override string GetShortLabel() => $"FinishAttack";

        public override void Execute(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if(actor == null)
            {
                return;
            }

            if (actor.HasActorType(ActorType.Player))
            {
                HandlePlayerActor(actor as PlayerActor);
            }
            else if (actor.HasActorType(ActorType.Monster))
            {
                HandleMonsterActor(actor as MonsterActor);
            }
        }

        private void HandleMonsterActor(MonsterActor actor)
        {
            if (actor == null)
            {
                return;
            }
        }

        private void HandlePlayerActor(PlayerActor actor)
        {
            if (actor == null)
            {
                return;
            }
            
            var movCtrl = actor.GetComponent<ActorMovementController>();
            if (movCtrl?.CurrentState is not PlayerFinishAttackState finishState)
            {
                return;
            }
            
            var targetActor = finishState.FinishTarget.GetComponent<MonsterActor>();
            if (targetActor == null)
            {
                return;
            }
            
            targetActor.OnTakeFinishAttack(actor.transform.forward);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}