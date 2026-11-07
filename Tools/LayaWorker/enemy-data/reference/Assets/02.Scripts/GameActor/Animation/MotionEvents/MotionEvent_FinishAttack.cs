using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 발자국 이벤트 (지형별 사운드)
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("FinishAttack", "Combat", 0, "피니시 공격 처리 타이밍을 발생시킵니다.", "finish", "execution", "처형", "피니시")]
    public class FinishAttackEvent : MotionEventBase
    {
        public override string GetDisplayName() => "FinishAttack";

        public override string GetShortLabel() => $"FinishAttack";

        public override void Execute(GameObject target)
        {
            var residualTarget = target.GetComponent<IFinishAttackMotionEventTarget>()
                                 ?? target.GetComponentInParent<IFinishAttackMotionEventTarget>()
                                 ?? target.GetComponentInChildren<IFinishAttackMotionEventTarget>();
            if (residualTarget != null)
            {
                Debug.Log($"[ResidualAttack] MotionEvent FinishAttack route. target={target.name}, handler={residualTarget.GetType().Name}");
                residualTarget.ApplyFinishAttackFromMotionEvent();
                return;
            }

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

            Transform finishTarget = finishState.FinishTarget;
            if (finishTarget == null)
                return;

            var targetActor = finishTarget.GetComponent<MonsterActor>()
                              ?? finishTarget.GetComponentInParent<MonsterActor>();
            // 진입 때 확정한 대상은 모션 중 브레이크가 회복돼도 유효하다.
            if (targetActor == null || !targetActor.gameObject.activeInHierarchy || !targetActor.CanTakeDamage())
            {
                return;
            }
            
            targetActor.OnTakeFinishAttack(actor, actor.transform.forward);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}
