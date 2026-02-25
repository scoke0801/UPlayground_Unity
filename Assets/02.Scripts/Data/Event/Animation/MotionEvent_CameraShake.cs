using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 카메라 쉐이크 이벤트
    /// </summary>
    [Serializable]
    public class BeginCameraShakeEvent : MotionEventBase
    {
        public float intensity = 1f;
        public float frequency = 10f;
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        public override string GetDisplayName() => "Camera Shake";

        public override string GetShortLabel() => $"Shake: {intensity:F1}";

        public override void Execute(GameObject target)
        {
            // 실제 구현은 카메라 매니저 연동 필요
            Debug.Log($"Camera Shake: Intensity={intensity}, Frequency={frequency}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

}