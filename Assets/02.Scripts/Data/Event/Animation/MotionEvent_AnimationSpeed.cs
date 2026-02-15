using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 애니메이션 속도 변경 이벤트
    /// </summary>
    [Serializable]
    public class AnimationSpeedEvent : MotionEventBase
    {
        public float speedMultiplier = 1f;
        public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public override string GetDisplayName() => "Anim Speed";

        public override string GetShortLabel() => $"Speed: {speedMultiplier:F2}x";

        public override void Execute(GameObject target)
        {
            Debug.Log($"Animation Speed: {speedMultiplier}x");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}