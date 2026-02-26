using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 카메라 거리(Distance)와 오프셋을 변화시키는 줌 이펙트
    /// zoomCurve로 줌 진행률을 제어한다.
    /// BlendOut 시 가중치가 0으로 감소하면서 자연스럽게 원래 거리로 복귀한다.
    /// </summary>
    public class ZoomCameraEffect : BaseCameraEffect
    {
        private readonly float _distanceDelta;
        private readonly Vector3 _offsetDelta;
        private readonly AnimationCurve _zoomCurve;

        public ZoomCameraEffect(ZoomCameraEffectData data) : base(data)
        {
            _distanceDelta = data.distanceDelta;
            _offsetDelta = data.offsetDelta;
            _zoomCurve = data.zoomCurve;
        }

        public override CameraEffectChannel AffectedChannels =>
            CameraEffectChannel.Distance | CameraEffectChannel.Offset;

        public override void Apply(ref CameraEffectState state)
        {
            // duration 기반 정규화 진행률 (0~1)
            float t = _duration > 0f ? Mathf.Clamp01(_elapsedTime / _duration) : 1f;
            float curveValue = _zoomCurve.Evaluate(t);

            state.distanceDelta += _distanceDelta * curveValue * Weight;
            state.offsetDelta += _offsetDelta * curveValue * Weight;
        }
    }
}
