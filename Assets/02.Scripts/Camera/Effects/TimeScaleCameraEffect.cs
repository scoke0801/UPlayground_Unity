using UnityEngine;
using UPlayGround.Data;
using UPlayGround.CameraSystem;

namespace UPlayGround
{
    /// <summary>
    /// TimeScale 제어 이펙트.
    ///
    /// useHitStopManager=true:  GameCombatManager.HitStop.Execute()에 위임
    ///                          → 요청 큐에 올라가 다른 효과와 자동 강도 비교
    /// useHitStopManager=false: 직접 GameTimeManager.Request()/Release()로 관리
    ///                          → 블렌드 아웃 중 ForceDispose 시 즉시 해제
    /// </summary>
    public class TimeScaleCameraEffect : BaseCameraEffect
    {
        private readonly float _targetTimeScale;
        private readonly bool  _useHitStopManager;

        // useHitStopManager=false 경로에서 발급받은 요청 id
        private int _requestId = -1;

        public TimeScaleCameraEffect(TimeScaleCameraEffectData data) : base(data)
        {
            _targetTimeScale   = data.targetTimeScale;
            _useHitStopManager = data.useHitStopManager;
        }

        public override CameraEffectChannel AffectedChannels => CameraEffectChannel.TimeScale;

        protected override void OnPlay()
        {
            if (_useHitStopManager)
            {
                // HitStopManager 경로: duration 종료 후 자동 Release
                CameraRuntimeServices.Adapter.ExecuteHitStop(
                    Mathf.Max(0.01f, _duration),
                    _targetTimeScale);
            }
            else
            {
                // 직접 관리 경로: id 발급, ForceDispose/OnStop에서 Release
                _requestId = CameraRuntimeServices.Adapter.RequestTimeScale(_targetTimeScale);
            }
        }

        protected override void OnUpdateEffect(float dt) { }

        protected override void OnStop()
        {
            // 직접 관리 경로에서 정상 종료(블렌드 아웃 완료) 시 해제
            ReleaseDirect();
        }

        public override void Apply(ref CameraEffectState state) { }

        public override void ForceDispose()
        {
            if (_useHitStopManager)
                CameraRuntimeServices.Adapter.StopHitStop();
            else
                ReleaseDirect();

            base.ForceDispose();
        }

        private void ReleaseDirect()
        {
            if (_requestId < 0) return;
            CameraRuntimeServices.Adapter.ReleaseTimeScale(_requestId);
            _requestId = -1;
        }
    }
}
