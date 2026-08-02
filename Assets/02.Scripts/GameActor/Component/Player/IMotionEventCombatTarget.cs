using System.Collections.Generic;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.Components
{
    /// <summary>
    /// GameActor가 아닌 실행체가 MotionEvent 기반 전투 이벤트를 받을 때 사용하는 최소 인터페이스.
    /// </summary>
    public interface IMotionEventCombatTarget
    {
        /// <summary>
        /// 판정 소스·그룹·명시적 Shape를 한 번에 전달해 Collision 윈도우를 시작한다.
        /// 잔류 실행체는 자신의 targetLayerMask를 이미 보유하므로 요청의 LayerMask는 사용하지 않는다.
        /// </summary>
        void BeginCollision(in UPlayGround.Combat.CollisionRequest request);

        void EndCollision();

        void SetTargetLayerMask(LayerMask targetLayerMask);
        void SetHitPhaseIndex(int hitPhaseIndex);
        void SetHitboxGroup(string hitboxGroupId);
        void SetHitboxGroups(IReadOnlyList<string> hitboxGroupIds);
        void SetEnableCollision(bool enabled);
        void ClearHitTargets();
    }

    public interface IFinishAttackMotionEventTarget
    {
        void ApplyFinishAttackFromMotionEvent();
    }

    public interface ISpecialBreakAttackMotionEventTarget
    {
        void ApplySpecialBreakAttackFromMotionEvent();
    }

    public interface IResidualMotionWarpTarget
    {
        WarpResolverContext BuildWarpResolverContext();
        void SetResidualMotionWarpTarget(string key, Transform target, bool useSnapshot);
        void BeginResidualMotionWarp(MotionWarpWindowSettings settings, string key);
        void EndResidualMotionWarp();
    }
}
