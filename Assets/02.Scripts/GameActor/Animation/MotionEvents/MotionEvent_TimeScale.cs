using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 애니메이션 이벤트 구간 동안 TimeScale을 변경한다.
    /// GameTimeManager의 요청 큐에 등록/해제하므로
    /// 다른 HitStop 효과와 자동으로 강도 비교된다.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class TimeScaleEvent : MotionEventBase
    {
        [Tooltip("목표 타임스케일 (0.01 = 거의 정지, 1.0 = 정상)")]
        [Range(0.01f, 1f)]
        public float targetTimeScale = 0.3f;

        [Tooltip("전환 시간 (0 = 즉시)")]
        [Min(0f)]
        public float blendDuration = 0.05f;

        public override string GetDisplayName() => "TimeScale";
        public override string GetShortLabel()  => $"TimeScale: ×{targetTimeScale:F2}";

        public override void Execute(GameObject target)
        {
            float duration = endTime - startTime;
            if (duration <= 0f) return;

            // HitStopManager.Execute()를 통해 큐에 등록
            // → 내부에서 GameTimeManager.Request()가 id를 발급하고 코루틴이 Release 처리
            ActorSvc.Combat?.ExecuteHitStop(duration, targetTimeScale);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            // 스킬 캔슬 등으로 구간이 강제 종료될 때만 호출됨.
            // HitStopManager는 이미 등록된 요청을 duration 기반으로 스스로 Release하므로
            // 여기서는 추가 조치 없음 — 중복 Release 방지.
        }
    }
}
