using UnityEngine;
using UPlayGround.CameraSystem;

namespace UPlayGround.Manager
{
    public partial class CameraManager
    {
        private GlidingSpeedLines _glidingSpeedLines;

        private void UpdateGlidingFeedback(bool canShow)
        {
            CameraMotionContext motion = CameraMotionContext.Unavailable;
            if (_targetMotion != null) _targetMotion.TryGetCameraMotionContext(out motion);
            UpdateRadialBlur(canShow && _mainCamera != null, motion);
            bool isVisible = canShow && settings != null && settings.enableGlidingSpeedFeedback
                && motion.IsGliding && _mainCamera != null;
            if (_glidingSpeedLines == null && isVisible && motion.SpeedFeedback > 0f
                && settings.glidingSpeedLineMaterial != null)
                _glidingSpeedLines = new GlidingSpeedLines(_mainCamera, settings);
            _glidingSpeedLines?.Advance(isVisible ? motion.SpeedFeedback : 0f,
                motion.PlanarVelocity, Time.deltaTime, isVisible);
        }

        private void DisposeGlidingFeedback()
        {
            _speedRadialBlur.Reset();
            SpeedRadialBlurState.Clear();
            _glidingSpeedLines?.Dispose();
            _glidingSpeedLines = null;
        }
    }
}
