using System;
using UnityEngine;
using UPlayGround.Manager;

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

        [NonSerialized] private string _runtimeEffectId;

        public override string GetDisplayName() => "Camera Shake";

        public override string GetShortLabel() => $"Shake: {intensity:F1}";

        public override void Execute(GameObject target)
        {
            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                return;
            }

            float holdDuration = Mathf.Max(0.01f, endTime - startTime);
            _runtimeEffectId = $"motion_shake_{GetHashCode()}_{(target != null ? target.GetInstanceID() : 0)}";

            Vector3 amp = Vector3.one * (0.08f * Mathf.Max(0f, intensity));
            cameraManager.PlayProceduralShakeEffect(_runtimeEffectId, amp, frequency, holdDuration, 0.02f, 0.15f);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (string.IsNullOrEmpty(_runtimeEffectId))
            {
                return;
            }

            CameraManager.Instance?.StopEffect(_runtimeEffectId);
            _runtimeEffectId = null;
        }
    }
}
