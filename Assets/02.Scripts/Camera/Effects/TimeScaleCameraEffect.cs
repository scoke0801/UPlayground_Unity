using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public sealed class TimeScaleCameraEffect : TimedCameraEffectBase
    {
        private readonly float _targetTimeScale;

        public TimeScaleCameraEffect(string effectId, float targetTimeScale, float holdDuration,
            float blendInDuration = 0.02f, float blendOutDuration = 0.08f)
            : base(effectId, holdDuration, blendInDuration, blendOutDuration)
        {
            _targetTimeScale = Mathf.Clamp(targetTimeScale, 0.01f, 2f);
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
            float scale = Mathf.Lerp(1f, _targetTimeScale, weight);
            output.PushTimeScale(scale);
        }
    }
}
