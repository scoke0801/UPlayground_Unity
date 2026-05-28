using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// GameActor가 아닌 실행체가 MotionEvent 기반 전투 이벤트를 받을 때 사용하는 최소 인터페이스.
    /// </summary>
    public interface IMotionEventCombatTarget
    {
        void SetTargetLayerMask(LayerMask targetLayerMask);
        void SetHitPhaseIndex(int hitPhaseIndex);
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
}
