using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class CameraSmoothDampEffectEvent : MotionEventBase
    {
        public Vector3 localOffset = new Vector3(0f, 0.2f, 0f);
        public float smoothTime = 0.12f;
        public float blendIn = 0.08f;
        public float blendOut = 0.12f;

        [NonSerialized] private string _runtimeEffectId;

        public override string GetDisplayName() => "Camera SmoothDamp";
        public override string GetShortLabel() => $"Smooth: {localOffset}";

        public override void Execute(GameObject target)
        {
            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                return;
            }

            float holdDuration = Mathf.Max(0.01f, endTime - startTime);
            _runtimeEffectId = $"motion_smooth_{GetHashCode()}_{(target != null ? target.GetInstanceID() : 0)}";
            cameraManager.PlaySmoothDampEffect(_runtimeEffectId, localOffset, holdDuration, smoothTime, blendIn, blendOut);
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
