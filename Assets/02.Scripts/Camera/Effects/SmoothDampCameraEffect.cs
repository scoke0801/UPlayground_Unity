using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 카메라 Position/Rotation SmoothDamp 시간을 오버라이드하는 이펙트
    /// 블렌드 가중치에 따라 기본값과 오버라이드 값 사이를 보간한다.
    /// </summary>
    public class SmoothDampCameraEffect : BaseCameraEffect
    {
        private readonly float _positionSmoothTime;
        private readonly float _rotationSmoothTime;

        public SmoothDampCameraEffect(SmoothDampCameraEffectData data) : base(data)
        {
            _positionSmoothTime = data.positionSmoothTime;
            _rotationSmoothTime = data.rotationSmoothTime;
        }

        public override CameraEffectChannel AffectedChannels => CameraEffectChannel.SmoothDamp;

        public override void Apply(ref CameraEffectState state)
        {
            // 오버라이드 값 설정 (CameraManager가 Weight에 따라 기본값과 Lerp)
            state.positionSmoothTimeOverride = _positionSmoothTime;
            state.rotationSmoothTimeOverride = _rotationSmoothTime;
        }
    }
}
