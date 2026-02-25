using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class CameraFovEffectEvent : MotionEventBase
    {
        public float fovOffset = -6f;
        public float blendIn = 0.08f;
        public float blendOut = 0.15f;

        [NonSerialized] private string _runtimeEffectId;

        public override string GetDisplayName() => "Camera FOV";
        public override string GetShortLabel() => $"FOV: {fovOffset:F1}";

        public override void Execute(GameObject target)
        {
            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                return;
            }

            float holdDuration = Mathf.Max(0.01f, endTime - startTime);
            _runtimeEffectId = $"motion_fov_{GetHashCode()}_{(target != null ? target.GetInstanceID() : 0)}";
            cameraManager.PlayFovEffect(_runtimeEffectId, fovOffset, holdDuration, blendIn, blendOut);
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
