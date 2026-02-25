using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class CameraZoomEffectEvent : MotionEventBase
    {
        public float distanceOffset = -1f;
        public float blendIn = 0.08f;
        public float blendOut = 0.12f;

        [NonSerialized] private string _runtimeEffectId;

        public override string GetDisplayName() => "Camera Zoom";
        public override string GetShortLabel() => $"Zoom: {distanceOffset:F2}";

        public override void Execute(GameObject target)
        {
            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                return;
            }

            float holdDuration = Mathf.Max(0.01f, endTime - startTime);
            _runtimeEffectId = $"motion_zoom_{GetHashCode()}_{(target != null ? target.GetInstanceID() : 0)}";
            cameraManager.PlayZoomEffect(_runtimeEffectId, distanceOffset, holdDuration, blendIn, blendOut);
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
