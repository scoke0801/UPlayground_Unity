using System;
using UnityEngine;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 애니메이션 이벤트 구간 동안 TimeScale을 변경한다.
    /// endTime에 자동으로 원래 TimeScale로 복원.
    /// </summary>
    [Serializable]
    public class TimeScaleEvent : MotionEventBase
    {
        [Tooltip("목표 타임스케일 (0.01 = 거의 정지, 1.0 = 정상)")]
        [Range(0.01f, 1f)]
        public float targetTimeScale = 0.3f;

        [Tooltip("전환 시간 (0 = 즉시)")]
        [Min(0f)]
        public float blendDuration = 0.05f;

        public override string GetDisplayName() => "TimeScale";
        public override string GetShortLabel() => $"TimeScale: ×{targetTimeScale:F2}";

        public override void Execute(GameObject target)
        {
            var mgr = GameHitStopManager.Instance;
            if (mgr == null) return;

            // duration = endTime - startTime 구간 동안 slowmo 적용
            float duration = endTime - startTime;
            if (duration <= 0f) return;

            mgr.Execute(duration, targetTimeScale);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            // HitStopManager의 코루틴이 duration 종료 후 자동 복원하므로
            // 이벤트가 중간에 끊길 경우(스킬 캔슬 등)에만 강제 Stop
            GameHitStopManager.Instance?.Stop();
        }
    }
}
