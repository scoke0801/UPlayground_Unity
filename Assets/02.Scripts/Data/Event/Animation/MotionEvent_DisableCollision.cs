using System;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 비활성화 이벤트.
    /// startTime에 충돌 OFF, endTime에 충돌 ON 복구.
    /// BeginCollisionEvent와 반대 동작 — 특정 구간만 피격/공격 판정을 제거할 때 사용.
    /// </summary>
    [Serializable]
    public class DisableCollisionEvent : MotionEventBase
    {
        public override string GetDisplayName() => "Disable Collision";
        public override string GetShortLabel() => "No Collision";

        public override void Execute(GameObject target) => SetCollision(target, false);
        public override void OnCompleteEvent(GameObject target) => SetCollision(target, true);

        private void SetCollision(GameObject target, bool enable)
        {
            var combatTarget = target.GetComponent<IMotionEventCombatTarget>()
                               ?? target.GetComponentInParent<IMotionEventCombatTarget>()
                               ?? target.GetComponentInChildren<IMotionEventCombatTarget>();
            if (combatTarget != null)
            {
                Debug.Log($"[ResidualAttack] MotionEvent DisableCollision route. target={target.name}, enable={enable}, handler={combatTarget.GetType().Name}");
                if (enable) combatTarget.ClearHitTargets();
                combatTarget.SetEnableCollision(enable);
                return;
            }

            var actor = target.GetComponent<GameActor>();
            if (actor == null) return;
            if (!actor.HasActorType(ActorType.Player) && !actor.HasActorType(ActorType.Monster))
                return;

            // P3 2차: PlayerCombat/EnemyCombat을 직접 호출하지 않고 CombatActionRunner에 위임한다.
            // P3 3차 이후 runner/executor가 없으면 우회 경로도 같은 runner로 forward되어 동작하지 않으므로,
            // 가짜 안전망 대신 설정 오류로 보고한다.
            CombatActionRunner runner = actor.ActionRunner;
            if (runner == null || !runner.HasCollisionExecutor)
            {
                Debug.LogError($"[DisableCollision] {actor.name}의 CombatActionRunner/ICombatCollisionExecutor가 준비되지 않아 이벤트가 무시됩니다.");
                return;
            }

            runner.HandleCollisionToggle(enable);
        }
    }
}
