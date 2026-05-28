using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 활성화 이벤트.
    /// hitPhaseIndex로 현재 히트가 몇 번째 구간인지 Combat에 알린다.
    /// </summary>
    [Serializable]
    public class BeginCollisionEvent : MotionEventBase
    {
        [Tooltip("AttackInfoBase.hitPhases의 인덱스. 멀티 히트 시 구간마다 다른 값을 설정한다.")]
        public int hitPhaseIndex = 0;

        public override string GetDisplayName() => "Collision";

        public override string GetShortLabel() => $"Collision [{hitPhaseIndex}]";

        public override void Execute(GameObject target)
        {
            if (HandleMotionEventCombatTarget(target, true))
                return;

            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null) return;

            if (actor.HasActorType(ActorType.Player))
                HandlePlayerCombat(actor as PlayerActor, true);
            else if (actor.HasActorType(ActorType.Monster))
                HandleMonsterCombat(actor as MonsterActor, true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (HandleMotionEventCombatTarget(target, false))
                return;

            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null) return;

            if (actor.HasActorType(ActorType.Player))
                HandlePlayerCombat(actor as PlayerActor, false);
            else if (actor.HasActorType(ActorType.Monster))
                HandleMonsterCombat(actor as MonsterActor, false);
        }

        private bool HandleMotionEventCombatTarget(GameObject target, bool isCollisionEnable)
        {
            var combatTarget = target.GetComponent<IMotionEventCombatTarget>()
                               ?? target.GetComponentInParent<IMotionEventCombatTarget>()
                               ?? target.GetComponentInChildren<IMotionEventCombatTarget>();
            if (combatTarget == null) return false;

            Debug.Log($"[ResidualAttack] MotionEvent Collision route. target={target.name}, enable={isCollisionEnable}, phase={hitPhaseIndex}, handler={combatTarget.GetType().Name}");
            combatTarget.ClearHitTargets();
            if (isCollisionEnable)
                combatTarget.SetHitPhaseIndex(hitPhaseIndex);
            combatTarget.SetEnableCollision(isCollisionEnable);
            return true;
        }

        private void HandlePlayerCombat(PlayerActor playerActor, bool isCollisionEnable)
        {
            if (playerActor == null) return;
            PlayerCombat combat = playerActor.GetCombat();
            if (combat == null) return;

            combat.ClearHitTargets();
            if (isCollisionEnable)
            {
                combat.SetTargetLayerMask(playerActor.GetAttackTargetLayerMask());
                combat.SetHitPhaseIndex(hitPhaseIndex);
            }
            combat.SetEnableCollision(isCollisionEnable);
        }

        private void HandleMonsterCombat(MonsterActor monsterActor, bool isCollisionEnable)
        {
            if (monsterActor == null) return;
            EnemyCombat combat = monsterActor.Combat;
            if (combat == null) return;

            combat.ClearHitTargets();
            if (isCollisionEnable)
            {
                combat.SetTargetLayer(monsterActor.GetAttackTargetLayerMask());
                combat.SetHitPhaseIndex(hitPhaseIndex);
            }
            combat.SetEnableCollision(isCollisionEnable);
        }
    }
}
