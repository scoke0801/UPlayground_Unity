using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public sealed class RotationCameraEffect : TimedCameraEffectBase
    {
        private readonly Vector3 _rotationEuler;

        public RotationCameraEffect(string effectId, Vector3 rotationEuler, float holdDuration, float blendInDuration = 0.1f,
            float blendOutDuration = 0.1f)
            : base(effectId, holdDuration, blendInDuration, blendOutDuration)
        {
            _rotationEuler = rotationEuler;
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
            output.LocalEulerOffset += _rotationEuler * weight;
        }
    }
}
