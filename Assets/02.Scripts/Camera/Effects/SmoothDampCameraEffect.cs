using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public sealed class SmoothDampCameraEffect : TimedCameraEffectBase
    {
        private readonly Vector3 _targetLocalOffset;
        private readonly float _smoothTime;
        private Vector3 _currentOffset;
        private Vector3 _velocity;

        public SmoothDampCameraEffect(string effectId, Vector3 targetLocalOffset, float holdDuration,
            float smoothTime = 0.12f, float blendInDuration = 0.1f, float blendOutDuration = 0.12f)
            : base(effectId, holdDuration, blendInDuration, blendOutDuration)
        {
            _targetLocalOffset = targetLocalOffset;
            _smoothTime = Mathf.Max(0.0001f, smoothTime);
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
            Vector3 target = _targetLocalOffset * weight;
            _currentOffset = Vector3.SmoothDamp(_currentOffset, target, ref _velocity, _smoothTime, Mathf.Infinity,
                deltaTime);
            output.LocalPositionOffset += _currentOffset;
        }
    }
}
