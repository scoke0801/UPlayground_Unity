using UPlayGround.CameraSystem;
using UnityEngine;

namespace UPlayGround.Manager
{
    public partial class CameraManager
    {
        private readonly SpeedRadialBlur _speedRadialBlur = new SpeedRadialBlur();

        private void UpdateRadialBlur(bool canShow, CameraMotionContext motion)
        {
            _speedRadialBlur.Advance(settings, motion, canShow, Time.deltaTime);
            if (settings == null)
            {
                SpeedRadialBlurState.Clear();
                return;
            }
            Vector3 flow = _mainCamera != null
                ? SpeedRadialBlur.CalculateFlow(_mainCamera.transform.rotation, _mainCamera.fieldOfView,
                    _mainCamera.aspect, motion.Velocity) : Vector3.forward;
            SpeedRadialBlurState.Set(_mainCamera, _speedRadialBlur.Strength,
                settings.speedRadialBlurInnerRadius, settings.speedRadialBlurOuterRadius,
                flow, settings.speedRadialBlurMaxRadius);
        }
    }
}
