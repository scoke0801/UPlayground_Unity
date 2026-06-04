using System;
using UnityEngine;
using UPlayGround.Combat;
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
            HandleActorCombat(actor, true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (HandleMotionEventCombatTarget(target, false))
                return;

            GameActor actor = target.GetComponent<GameActor>();
            HandleActorCombat(actor, false);
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

        // P3 2차: PlayerCombat/EnemyCombat을 직접 호출하지 않고 CombatActionRunner에 위임한다.
        // runner가 등록된 ICombatCollisionExecutor(=각 Combat)에 동일한 순서로 전달한다.
        private void HandleActorCombat(GameActor actor, bool isCollisionEnable)
        {
            if (actor == null) return;
            if (!actor.HasActorType(ActorType.Player) && !actor.HasActorType(ActorType.Monster))
                return;

            CombatActionRunner runner = actor.ActionRunner;
            if (runner == null || !runner.HasCollisionExecutor)
            {
                // P3 3차 이후 충돌 윈도우는 runner instance가 단일 소유한다. runner/executor가 없으면
                // 그 자체가 설정 오류이며, 우회 경로(legacy SetEnableCollision)도 같은 runner로 forward되어
                // 결국 판정이 동작하지 않는다. 따라서 가짜 안전망을 두지 않고 설정 오류로 보고한다.
                Debug.LogError($"[Collision] {actor.name}의 CombatActionRunner/ICombatCollisionExecutor가 준비되지 않아 충돌 이벤트가 무시됩니다.");
                return;
            }

            runner.HandleCollisionEvent(isCollisionEnable, hitPhaseIndex, actor.GetAttackTargetLayerMask());
        }
    }
}
