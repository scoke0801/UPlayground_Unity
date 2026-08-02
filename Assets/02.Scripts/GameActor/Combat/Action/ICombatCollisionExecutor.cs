using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// <see cref="CombatActionRunner"/>가 충돌 판정 윈도우를 구동할 때 위임하는 실행체 계약 (P3 2차).
    /// PlayerCombat / EnemyCombat이 구현한다.
    ///
    /// 잔류 공격용 <c>IMotionEventCombatTarget</c>과 의도적으로 분리한다 — MotionEvent의 잔류 경로가
    /// PlayerCombat/EnemyCombat을 먼저 가로채는 것을 막기 위함이다.
    ///
    /// 신규 호출부는 원자적 <see cref="BeginCollision"/>/<see cref="EndCollision"/>을 사용한다.
    /// 아래 setter들은 직접 호출자(PlayerChargeState, 궁극기 등) 호환을 위한 전환기 API이며,
    /// 명시적 Shape 요청을 표현할 수 없으므로 신규 코드에서 사용하지 않는다.
    /// </summary>
    public interface ICombatCollisionExecutor
    {
        /// <summary>판정 소스·그룹·Shape·LayerMask를 한 번에 전달해 Collision 윈도우를 시작한다.</summary>
        void BeginCollision(in CollisionRequest request);

        /// <summary>Collision 윈도우를 종료하고 부착형 그룹과 명시적 세션을 모두 정리한다.</summary>
        void EndCollision();

        void SetTargetLayerMask(LayerMask targetLayerMask);
        void SetHitPhaseIndex(int hitPhaseIndex);
        void SetHitboxGroup(string hitboxGroupId);
        void SetHitboxGroups(IReadOnlyList<string> hitboxGroupIds);
        void SetEnableCollision(bool enabled);
        void ClearHitTargets();
    }
}
