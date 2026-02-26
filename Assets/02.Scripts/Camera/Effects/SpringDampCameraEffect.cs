using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 감쇠 조화 진동(Damped Harmonic Oscillator) 기반 카메라 스프링 이펙트
    /// 초기 변위에서 시작하여 스프링처럼 진동하며 감쇠된다.
    /// x'' = -omega^2 * x - 2 * zeta * omega * x'
    /// </summary>
    public class SpringDampCameraEffect : BaseCameraEffect
    {
        private readonly float _frequency;
        private readonly float _damping;
        private readonly Vector3 _initialDisplacement;
        private Vector3 _currentDisplacement;
        private Vector3 _velocity;

        public SpringDampCameraEffect(SpringDampCameraEffectData data) : base(data)
        {
            _frequency = data.springFrequency;
            _damping = data.springDamping;
            _initialDisplacement = data.initialDisplacement;
        }

        public override CameraEffectChannel AffectedChannels => CameraEffectChannel.Position;

        protected override void OnPlay()
        {
            _currentDisplacement = _initialDisplacement;
            _velocity = Vector3.zero;
        }

        protected override void OnUpdateEffect(float dt)
        {
            if (dt <= 0f) return;

            float omega = _frequency * 2f * Mathf.PI;
            Vector3 acceleration = -omega * omega * _currentDisplacement
                                   - 2f * _damping * omega * _velocity;
            _velocity += acceleration * dt;
            _currentDisplacement += _velocity * dt;
        }

        public override void Apply(ref CameraEffectState state)
        {
            state.positionDelta += _currentDisplacement * Weight;
        }
    }
}
