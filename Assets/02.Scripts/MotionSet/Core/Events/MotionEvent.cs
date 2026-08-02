using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    public enum MotionEventLinkMode
    {
        [InspectorName("절대 시간")] Absolute,
        [InspectorName("상대 시간")] Relative,
        [InspectorName("비율")] Proportional,
        [InspectorName("마커")] Marker,
    }

    public enum MotionEventReentryPolicy
    {
        [InspectorName("재생당 1회")] OncePerPlayback,
        [InspectorName("구간 진입당 1회")] OncePerSectionEntry,
        [InspectorName("통과할 때마다")] EveryCrossing,
    }

    public enum MotionEventDispatchMode
    {
        [InspectorName("큐 처리")] Queued,
        [InspectorName("정확한 시점")] Exact,
    }

    public enum MotionEventEvaluationPhase
    {
        [InspectorName("Update (기본)")] Update,
        [InspectorName("애니메이션 평가 후")] PostAnimationEvaluation,
    }

    [Serializable]
    public struct MotionEventTimeLink
    {
        [MotionEventLabel("사용")] public bool enabled;
        [MotionEventLabel("연결 방식")] public MotionEventLinkMode mode;
        [MotionEventLabel("연결 모션 ID")] public string linkedMotionId;
        [MotionEventLabel("마커 ID")] public string markerId;
        [MotionEventLabel("시작 값")] public float startValue;
        [MotionEventLabel("끝 값")] public float endValue;
    }

    public interface IMotionEventTick
    {
        void Tick(GameObject target, float normalizedTime, float deltaTime);
    }

    public interface IMotionEventSignal
    {
        string SignalId { get; }
    }

    public interface IMotionEventPreviewAdapter
    {
        void Enter(MotionEventBase motionEvent, float globalTime);
        void Tick(MotionEventBase motionEvent, float normalizedTime, float deltaTime);
        void Exit(MotionEventBase motionEvent, float globalTime);
    }

    /// <summary>
    /// 모션 이벤트 기본 추상 클래스
    /// 모든 모션 이벤트는 이 클래스를 상속받아야 함
    /// </summary>
    [Serializable]
    public abstract class MotionEventBase
    {
        public float startTime;
        public float endTime;
        [MotionEventLabel("시간 링크")] public MotionEventTimeLink timeLink;
        [MotionEventLabel("재진입 정책")] public MotionEventReentryPolicy reentryPolicy;
        [MotionEventLabel("실행 순서")] public int executionOrder;
        [MotionEventLabel("디스패치 방식")] public MotionEventDispatchMode dispatchMode;
        [MotionEventLabel("평가 시점")] public MotionEventEvaluationPhase evaluationPhase;
            
        // 이전 모션들의 누적 시간 (글로벌 타임라인에서의 오프셋)
        [HideInInspector] public float globalStartTimeOffset = 0f;
        
        /// <summary>
        /// 이벤트가 특정 시간(글로벌 시간)에 활성화되는지 확인
        /// 앞선 모션에서 흐른 시간을 더해 절대적인 글로벌 시간으로 비교
        /// </summary>
        public bool IsActiveAtGlobal(float globalTime)
        {
            float absoluteStartTime = startTime + globalStartTimeOffset;
            float absoluteEndTime = endTime + globalStartTimeOffset;
            
            return globalTime >= absoluteStartTime && globalTime <= absoluteEndTime;
        }
        
        /// <summary>
        /// 이벤트가 특정 시간(글로벌 시간)에 활성화되는지 확인
        /// 앞선 모션에서 흐른 시간을 더해 절대적인 글로벌 시간으로 비교
        /// </summary>
        public bool IsActiveAt(float localTime)
        {
            return localTime >= startTime && localTime <= endTime;
        }
        
        /// <summary>
        /// 이벤트의 표시 이름 (에디터용)
        /// </summary>
        public abstract string GetDisplayName();

        /// <summary>
        /// 이벤트의 짧은 설명 (타임라인 바에 표시)
        /// </summary>
        public virtual string GetShortLabel() => GetDisplayName();

        /// <summary>
        /// true면 이벤트 발화 결정(Update)과 실제 Execute를 분리해, 본(스켈레톤) 평가가 끝난 뒤
        /// LateUpdate에서 실행한다. 블레이드 본 등 라이브 트랜스폼의 월드 포즈를 즉석 샘플링하는
        /// 공간 이벤트(SlashVFX 등)는 Update 시점에 실행하면 직전 프레임 포즈를 읽어 위치가
        /// 프레임 타이밍에 따라 흔들리므로 반드시 본 평가 후에 샘플링해야 한다.
        /// 콜리전/워프/Freeze 등 타이밍이 민감한 이벤트는 false를 유지해 기존 Update 타이밍을 보존한다.
        /// </summary>
        public virtual bool RequiresPostEvaluation =>
            evaluationPhase == MotionEventEvaluationPhase.PostAnimationEvaluation;

        /// <summary>
        /// 이벤트 실행 (런타임)
        /// </summary>
        public abstract void Execute(GameObject target);

        /// <summary>
        /// 서브프레임 보정용 Execute. <paramref name="subFrameFraction"/>은 이벤트의 글로벌 시작 시각이
        /// 직전 프레임(lastTime)과 현재 프레임(currentTime) 사이 어디에 위치하는지를 [0,1]로 나타낸다.
        /// RequiresPostEvaluation 공간 이벤트(SlashVFX 등)는 이 값으로 직전/현재 프레임 포즈를 보간해,
        /// 발화 프레임의 오버슈트(프레임 타이밍 변동)와 무관하게 항상 동일한 모션 시점 위치에서 샘플링한다.
        /// 분율이 필요 없는 이벤트는 기존 Execute로 위임한다.
        /// </summary>
        public virtual void Execute(GameObject target, float subFrameFraction) => Execute(target);

        public abstract void OnCompleteEvent(GameObject target);
    }

}
