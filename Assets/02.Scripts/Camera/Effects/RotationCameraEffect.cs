using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 카메라 Yaw/Pitch를 지정된 양만큼 회전시키는 이펙트
    /// rotationCurve로 회전 진행률을 제어하며, 프레임 간 차이(frameDelta)만큼만 적용하여
    /// 누적 없이 정확한 총 회전량을 보장한다.
    /// </summary>
    public class RotationCameraEffect : BaseCameraEffect
    {
        private readonly float _yawDelta;
        private readonly float _pitchDelta;
        private readonly AnimationCurve _rotationCurve;
        private float _lastCurveValue;

        public RotationCameraEffect(RotationCameraEffectData data) : base(data)
        {
            _yawDelta = data.yawDelta;
            _pitchDelta = data.pitchDelta;
            _rotationCurve = data.rotationCurve;
        }

        public override CameraEffectChannel AffectedChannels =>
            CameraEffectChannel.Yaw | CameraEffectChannel.Pitch;

        protected override void OnPlay()
        {
            _lastCurveValue = 0f;
        }

        public override void Apply(ref CameraEffectState state)
        {
            // duration 기반 정규화 진행률 (0~1)
            float t = _duration > 0f ? Mathf.Clamp01(_elapsedTime / _duration) : 1f;
            float curveValue = _rotationCurve.Evaluate(t);

            // 프레임 간 커브 변화분만 적용 → 총 회전량 = yawDelta * 1.0
            float frameDelta = curveValue - _lastCurveValue;
            _lastCurveValue = curveValue;

            state.yawDelta += _yawDelta * frameDelta * Weight;
            state.pitchDelta += _pitchDelta * frameDelta * Weight;
        }
    }
}
