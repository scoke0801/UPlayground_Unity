using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 카메라 FOV(Field of View)를 변화시키는 이펙트
    /// fovCurve로 변화 진행률을 제어한다.
    /// BlendOut 시 가중치가 0으로 감소하면서 자연스럽게 기본 FOV로 복귀한다.
    /// </summary>
    public class FOVCameraEffect : BaseCameraEffect
    {
        private readonly float _fovDelta;
        private readonly AnimationCurve _fovCurve;

        public FOVCameraEffect(FOVCameraEffectData data) : base(data)
        {
            _fovDelta = data.fovDelta;
            _fovCurve = data.fovCurve;
        }

        public override CameraEffectChannel AffectedChannels => CameraEffectChannel.FOV;

        public override void Apply(ref CameraEffectState state)
        {
            // duration 기반 정규화 진행률 (0~1)
            float t = _duration > 0f ? Mathf.Clamp01(_elapsedTime / _duration) : 1f;
            float curveValue = _fovCurve.Evaluate(t);

            state.fovDelta += _fovDelta * curveValue * Weight;
        }
    }
}
