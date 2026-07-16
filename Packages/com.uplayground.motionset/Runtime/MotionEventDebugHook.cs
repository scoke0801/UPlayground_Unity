using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Animation
{
    /// <summary>
    /// MotionEventExecutor의 재생 디버그 정보를 받아가는 싱크.
    /// 게임 측 오버레이(예: MotionSetEventDebugOverlay)가 구현해 <see cref="MotionEventDebugHook.Sink"/>에 꽂는다.
    /// </summary>
    public interface IMotionEventDebugSink
    {
        void Publish(GameObject target, float currentTime, IEnumerable<Data.Event.MotionEventBase> activeEvents, string sourceName);
        void RecordEvent(string message);
        void Clear();
    }

    /// <summary>
    /// 재생 디버그 연결점. 미등록(null)이면 디버그 출력 없이 조용히 동작한다.
    /// </summary>
    public static class MotionEventDebugHook
    {
        public static IMotionEventDebugSink Sink;
    }
}
