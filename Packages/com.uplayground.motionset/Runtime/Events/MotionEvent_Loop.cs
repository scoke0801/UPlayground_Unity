using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UPlayGround.Data.Event
{
    public enum LoopEventMode
    {
        Loop,         // startTime~endTime 구간을 N회 반복
        Freeze,       // startTime 시점에서 duration만큼 정지
        InfiniteLoop, // startTime~endTime 구간을 외부에서 해제할 때까지 무한 반복
    }

    /// <summary>
    /// 모션 구간 반복 / 프리즈 이벤트.
    /// 일반 이벤트와 달리 ActorAnimator가 타임라인 흐름 자체를 제어한다.
    /// Execute/OnCompleteEvent는 사용하지 않음 — 타임라인 레벨에서 처리.
    /// </summary>
    [Serializable]
    [MotionEventMeta("Loop", Category = "Movement / Time", CategoryOrder = 30,
        Description = "모션 구간 반복, 정지, 무한 루프를 설정합니다.",
        Aliases = new[] { "repeat", "freeze", "loop", "반복", "정지" },
        Icon = "🔁", Color = new[] { 0.35f, 0.75f, 1.00f })]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class LoopEvent : MotionEventBase
    {
        public LoopEventMode mode = LoopEventMode.Loop;

        [Tooltip("Loop: 반복 횟수 (1이면 구간을 1회 추가 재생 = 총 2회)")]
        public int loopCount = 1;

        [Tooltip("Freeze: 정지 시간(초). startTime 시점에서 이 시간만큼 멈춘다.")]
        public float freezeDuration = 0.5f;

        public override string GetDisplayName() => mode switch
        {
            LoopEventMode.Loop => "Loop",
            LoopEventMode.Freeze => "Freeze",
            LoopEventMode.InfiniteLoop => "∞ Loop",
            _ => "Loop"
        };

        public override string GetShortLabel() => mode switch
        {
            LoopEventMode.Loop => $"Loop x{loopCount}",
            LoopEventMode.Freeze => $"Freeze {freezeDuration:F2}s",
            LoopEventMode.InfiniteLoop => "∞ Loop",
            _ => "Loop"
        };

        // 타임라인 제어 이벤트이므로 Execute는 no-op
        public override void Execute(GameObject target) { }
        public override void OnCompleteEvent(GameObject target) { }
    }
}