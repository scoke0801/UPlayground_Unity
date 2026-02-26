using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;

namespace UPlayGround
{
    /// <summary>
    /// TimeScale 제어 이펙트
    /// useHitStopManager=true: 기존 GameHitStopManager.Execute()에 위임 (기존 동작 보존)
    /// useHitStopManager=false: 직접 Time.timeScale을 블렌딩하여 부드러운 전환 제공
    ///
    /// 주의: 이 이펙트는 항상 unscaledDeltaTime을 사용한다.
    /// </summary>
    public class TimeScaleCameraEffect : BaseCameraEffect
    {
        private readonly float _targetTimeScale;
        private readonly bool _useHitStopManager;
        private float _originalTimeScale;
        private bool _delegatedToHitStop;

        public TimeScaleCameraEffect(TimeScaleCameraEffectData data) : base(data)
        {
            _targetTimeScale = data.targetTimeScale;
            _useHitStopManager = data.useHitStopManager;
        }

        public override CameraEffectChannel AffectedChannels => CameraEffectChannel.TimeScale;

        protected override void OnPlay()
        {
            _originalTimeScale = Time.timeScale;

            if (_useHitStopManager)
            {
                var hitStopMgr = GameHitStopManager.Instance;
                if (hitStopMgr != null)
                {
                    float hitStopDuration = Mathf.Max(0.01f, _duration);
                    hitStopMgr.Execute(hitStopDuration, _targetTimeScale);
                    _delegatedToHitStop = true;
                }
            }
        }

        protected override void OnUpdateEffect(float dt)
        {
            // HitStopManager에 위임하지 않은 경우 직접 블렌딩
            if (!_useHitStopManager && !_delegatedToHitStop)
            {
                Time.timeScale = Mathf.Lerp(_originalTimeScale, _targetTimeScale, Weight);
            }
        }

        protected override void OnStop()
        {
            if (!_useHitStopManager && !_delegatedToHitStop)
            {
                // BlendOut에서 Weight가 0으로 감소하면서 자동 복원
                // Stop 시점에서는 아직 BlendOut 중이므로 즉시 복원하지 않음
            }
        }

        public override void Apply(ref CameraEffectState state)
        {
            // TimeScale은 카메라 기하학을 수정하지 않는 사이드 이펙트
            // OnUpdateEffect에서 직접 Time.timeScale을 제어한다
        }

        public override void ForceDispose()
        {
            if (!_useHitStopManager && !_delegatedToHitStop)
            {
                Time.timeScale = _originalTimeScale;
            }
            else if (_delegatedToHitStop)
            {
                GameHitStopManager.Instance?.Stop();
            }
            base.ForceDispose();
        }
    }
}
