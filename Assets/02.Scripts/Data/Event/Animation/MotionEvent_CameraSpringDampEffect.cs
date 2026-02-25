using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class CameraSpringDampEffectEvent : MotionEventBase
    {
        public Vector3 localOffset = new Vector3(0f, 0.35f, -0.1f);
        public float stiffness = 90f;
        public float damping = 16f;
        public float blendIn = 0.05f;
        public float blendOut = 0.15f;

        [NonSerialized] private string _runtimeEffectId;

        public override string GetDisplayName() => "Camera SpringDamp";
        public override string GetShortLabel() => $"Spring: {localOffset}";

        public override void Execute(GameObject target)
        {
            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                return;
            }

            float holdDuration = Mathf.Max(0.01f, endTime - startTime);
            _runtimeEffectId = $"motion_spring_{GetHashCode()}_{(target != null ? target.GetInstanceID() : 0)}";
            cameraManager.PlaySpringDampEffect(_runtimeEffectId, localOffset, holdDuration, stiffness, damping, blendIn,
                blendOut);
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
