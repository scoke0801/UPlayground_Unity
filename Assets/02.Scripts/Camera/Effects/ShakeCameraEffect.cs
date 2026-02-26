using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 기존 CameraShaker를 래핑하는 Shake 이펙트
    /// 이펙트 시스템의 생명주기(BlendIn/Out, Priority)를 적용하면서
    /// 실제 셰이크 동작은 CameraShaker의 Pre/PostRender 콜백에 위임한다.
    /// Apply()는 비어있음 (CameraShaker가 직접 카메라 위치를 수정)
    /// </summary>
    public class ShakeCameraEffect : BaseCameraEffect
    {
        private readonly CameraShakeData _shakeData;
        private readonly string _shakeDataKey;

        public ShakeCameraEffect(ShakeCameraEffectData data) : base(data)
        {
            _shakeData = data.shakeData;
            _shakeDataKey = data.shakeDataKey;
        }

        public override CameraEffectChannel AffectedChannels => CameraEffectChannel.Position;

        protected override void OnPlay()
        {
            var cameraManager = CameraManager.Instance;
            if (cameraManager == null) return;

            if (_shakeData != null)
                cameraManager.StartShake(_shakeData);
            else if (!string.IsNullOrEmpty(_shakeDataKey))
                cameraManager.StartShake(_shakeDataKey);
        }

        protected override void OnStop()
        {
            // 조기 중단 시에만 셰이크 강제 정지
            // (정상 종료 시 CameraShaker가 duration 만큼 자동 정지)
        }

        public override void Apply(ref CameraEffectState state)
        {
            // CameraShaker가 Pre/PostRender 콜백으로 직접 처리하므로
            // 이펙트 시스템의 델타 누적에는 기여하지 않음
        }

        public override void ForceDispose()
        {
            CameraManager.Instance?.StopShake();
            base.ForceDispose();
        }
    }
}
