using System;
using UnityEngine;
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

            switch (actor)
            {
                case PlayerActor player:
                {
                    var combat = player.GetCombat();
                    if (combat == null) return;
                    if (enable) combat.ClearHitTargets();
                    combat.SetEnableCollision(enable);
                    break;
                }
                case MonsterActor monster:
                {
                    var combat = monster.Combat;
                    if (combat == null) return;
                    if (enable) combat.ClearHitTargets();
                    combat.SetEnableCollision(enable);
                    break;
                }
            }
        }
    }
}
