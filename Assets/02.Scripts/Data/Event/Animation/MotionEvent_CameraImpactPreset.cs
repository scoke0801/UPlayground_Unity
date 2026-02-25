using System;
using UnityEngine;
using UPlayGround.CameraEffects;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class CameraImpactPresetEvent : MotionEventBase
    {
        public CameraImpactPreset preset = CameraImpactPreset.MediumHit;

        [NonSerialized] private string _runtimeGroupId;

        public override string GetDisplayName() => "Camera Preset";
        public override string GetShortLabel() => $"Preset: {preset}";

        public override void Execute(GameObject target)
        {
            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                return;
            }

            _runtimeGroupId = $"motion_preset_{preset}_{GetHashCode()}_{(target != null ? target.GetInstanceID() : 0)}";
            cameraManager.PlayImpactPreset(preset, _runtimeGroupId);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (string.IsNullOrEmpty(_runtimeGroupId))
            {
                return;
            }

            CameraManager.Instance?.StopImpactPreset(_runtimeGroupId);
            _runtimeGroupId = null;
        }
    }
}
