using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 타임스케일 조작 이벤트 (슬로우 모션 등)
    /// </summary>
    [Serializable]
    public class TimeScaleEvent : MotionEventBase
    {
        [Range(0.01f, 2f)] public float timeScale = 0.5f;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public override string GetDisplayName() => "Time Scale";

        public override string GetShortLabel() => $"Time: {timeScale:F2}x";

        public override void Execute(GameObject target)
        {
            Debug.Log($"Time Scale: {timeScale}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

}