using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public sealed class SpringDampCameraEffect : TimedCameraEffectBase
    {
        private readonly Vector3 _targetLocalOffset;
        private readonly float _stiffness;
        private readonly float _damping;

        private Vector3 _position;
        private Vector3 _velocity;

        public SpringDampCameraEffect(string effectId, Vector3 targetLocalOffset, float holdDuration,
            float stiffness = 90f, float damping = 16f, float blendInDuration = 0.05f, float blendOutDuration = 0.15f)
            : base(effectId, holdDuration, blendInDuration, blendOutDuration)
        {
            _targetLocalOffset = targetLocalOffset;
            _stiffness = Mathf.Max(0f, stiffness);
            _damping = Mathf.Max(0f, damping);
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
            Vector3 target = _targetLocalOffset * weight;

            Vector3 displacement = _position - target;
            Vector3 acceleration = (-_stiffness * displacement) - (_damping * _velocity);

            _velocity += acceleration * deltaTime;
            _position += _velocity * deltaTime;

            output.LocalPositionOffset += _position;
        }
    }
}
