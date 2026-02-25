using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class CameraRotationEffectEvent : MotionEventBase
    {
        public Vector3 eulerOffset = new Vector3(0f, 4f, 0f);
        public float blendIn = 0.08f;
        public float blendOut = 0.12f;

        [NonSerialized] private string _runtimeEffectId;

        public override string GetDisplayName() => "Camera Rotation";
        public override string GetShortLabel() => $"Rot: {eulerOffset}";

        public override void Execute(GameObject target)
        {
            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                return;
            }

            float holdDuration = Mathf.Max(0.01f, endTime - startTime);
            _runtimeEffectId = $"motion_rot_{GetHashCode()}_{(target != null ? target.GetInstanceID() : 0)}";
            cameraManager.PlayRotationEffect(_runtimeEffectId, eulerOffset, holdDuration, blendIn, blendOut);
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
