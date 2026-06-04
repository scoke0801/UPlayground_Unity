using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// <see cref="CombatActionRunner"/>가 충돌 판정 윈도우를 구동할 때 위임하는 실행체 계약 (P3 2차).
    /// PlayerCombat / EnemyCombat이 구현한다.
    ///
    /// 잔류 공격용 <c>IMotionEventCombatTarget</c>과 의도적으로 분리한다 — MotionEvent의 잔류 경로가
    /// PlayerCombat/EnemyCombat을 먼저 가로채는 것을 막기 위함이다.
    /// </summary>
    public interface ICombatCollisionExecutor
    {
        void SetTargetLayerMask(LayerMask targetLayerMask);
        void SetHitPhaseIndex(int hitPhaseIndex);
        void SetEnableCollision(bool enabled);
        void ClearHitTargets();
    }
}
