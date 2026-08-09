using System.Collections.Generic;
using System;
using UnityEngine;
using UPlayGround.Data.Event;

using UPlayGround.Debugging;

namespace UPlayGround.Animation
{
    /// <summary>
    /// 모션 이벤트 실행 매니저
    /// 애니메이션 재생 중 이벤트를 감지하고 실행
    /// </summary>
    public class MotionEventExecutor : MonoBehaviour
    {
        public event Action<MotionEventBase> EventExecuted;
        public event Action<string, bool> SignalChanged;
        [SerializeField] GameObject _targetObject;

        private MotionSet _currentMotionSet;
        private float _currentTime;
        private float _lastTime; // 이전 프레임 시간 저장
        private HashSet<MotionEventBase> _activeEvents = new HashSet<MotionEventBase>();
        private HashSet<MotionEventBase> _executedEvents = new HashSet<MotionEventBase>();
        private HashSet<MotionEventBase> _sectionExecutedEvents = new HashSet<MotionEventBase>();
        private readonly List<MotionEventBase> _eventsToTrigger = new List<MotionEventBase>();
        private readonly List<MotionEventBase> _eventsToRemove = new List<MotionEventBase>();
        private readonly List<MotionEventBase> _seekEvents = new List<MotionEventBase>();
        private readonly List<MotionEventBase> _activeEventOrder = new List<MotionEventBase>();
        private readonly List<MotionEventBase> _queuedEvents = new List<MotionEventBase>();
        private readonly List<MotionEventBase> _crossedEvents = new List<MotionEventBase>();

        // RequiresPostEvaluation 이벤트는 발화 결정(UpdateTime)과 실제 Execute를 분리해,
        // 본 평가가 끝난 뒤 FlushDeferredEvents(LateUpdate)에서 실행한다.
        // 발화 프레임의 포즈는 eventStart를 0~한 프레임만큼 오버슈트하므로, eventStart가
        // [_lastTime, _currentTime] 구간 어디에 위치하는지(subFrameFraction)를 함께 보관해
        // 공간 이벤트가 직전/현재 프레임 포즈를 보간하도록 한다.
        private struct DeferredEvent
        {
            public MotionEventBase evt;
            public float subFrameFraction;
            public bool completeAfterExecute;
        }
        private List<DeferredEvent> _deferredExecute = new List<DeferredEvent>();

        // 명시적 대상이 없으면 부모의 범용 provider를 찾고, 없으면 Executor 자신을 사용한다.
        // Core는 GameActor를 알지 않으며 외부 호스트가 IMotionEventTargetProvider를 구현한다.
        private GameObject _resolvedTarget;

        public GameObject TargetObject
        {
            get
            {
                if (_targetObject != null) return _targetObject;
                if (_resolvedTarget != null) return _resolvedTarget;

                MonoBehaviour[] parents = GetComponentsInParent<MonoBehaviour>(true);
                for (int i = 0; i < parents.Length; i++)
                {
                    if (parents[i] is not IMotionEventTargetProvider provider)
                        continue;

                    GameObject providedTarget = provider.MotionEventTarget;
                    if (providedTarget == null)
                        continue;

                    _resolvedTarget = providedTarget;
                    return _resolvedTarget;
                }

                _resolvedTarget = gameObject;
                return _resolvedTarget;
            }
        }

        public void SetTargetObject(GameObject target)
        {
            _targetObject = target;
            _resolvedTarget = target;
        }

        private void OnTransformParentChanged()
        {
            if (_targetObject == null)
                _resolvedTarget = null;
        }

        /// <summary>
        /// 모션 셋 재생 시작
        /// </summary>
        public void PlayMotionSet(MotionSet motionSet)
        {
            _currentMotionSet = motionSet;
            _currentTime = 0f;
            _lastTime = -0.001f; // 0초에 걸린 이벤트도 실행되도록 약간 음수에서 시작
            _activeEvents.Clear();
            _executedEvents.Clear();
            _sectionExecutedEvents.Clear();
            _deferredExecute.Clear();

            // 재생 시작 시 모든 이벤트의 글로벌 시작 시간 오프셋을 미리 계산
            CalculateEventOffsets();
        }

        /// <summary>
        /// 타임라인 시간 업데이트 (매 프레임 호출)
        /// </summary>
        public void UpdateTime(float time)
        {
            if (_currentMotionSet == null) return;

            // 시간이 되감아진 경우 (Loop 발생 시) lastTime을 조정하여 중복 실행 방지
            if (time < _lastTime)
            {
                _lastTime = time - 0.001f;
            }

            _currentTime = time;
            ProcessEvents();
        }

        /// <summary>
        /// 현재 시간의 이벤트 처리
        /// </summary>
        void ProcessEvents()
        {
            // 1. 기존 활성 이벤트들 중 종료된 것 처리
            _eventsToRemove.Clear();
            _activeEventOrder.Clear();
            _activeEventOrder.AddRange(_activeEvents);
            _activeEventOrder.Sort(CompareExecutionOrder);
            foreach (var evt in _activeEventOrder)
            {
                if (!MotionTimelineResolver.TryGetEventGlobalRange(
                        _currentMotionSet,
                        evt,
                        out float start,
                        out float end) ||
                    _currentTime < start ||
                    _currentTime > end)
                {
                    CompleteEvent(evt);
                    _eventsToRemove.Add(evt);
                    continue;
                }

                if (evt is IMotionEventTick tick)
                {
                    float duration = Mathf.Max(0.0001f, end - start);
                    float normalizedTime = Mathf.Clamp01((_currentTime - start) / duration);
                    tick.Tick(TargetObject, normalizedTime, Mathf.Max(0f, _currentTime - _lastTime));
                }
            }
            
            // 예약된 요소들 삭제
            foreach (var evt in _eventsToRemove)
            {
                _activeEvents.Remove(evt);
                MotionSetEventDebugOverlay.RecordEvent(
                    $"Complete {evt.GetShortLabel()} @{_currentTime:F2}s");
            }

            // 2. 새로운 이벤트들 탐색 및 실행
            // [lastTime, currentTime] 구간에 시작점이 포함된 모든 이벤트 탐색
            MotionTimelineResolver.CollectEventsInRange(
                _currentMotionSet,
                _lastTime,
                _currentTime,
                _eventsToTrigger);

            _queuedEvents.Clear();
            _crossedEvents.Clear();
            foreach (var evt in _eventsToTrigger)
            {
                if (CanEnter(evt))
                {
                    bool crossedEnd = MotionTimelineResolver.TryGetEventGlobalRange(
                        _currentMotionSet,
                        evt,
                        out _,
                        out float eventEnd) &&
                        _currentTime > eventEnd;
                    // 공간 샘플링 이벤트는 본 평가 후 실행해야 하므로 Execute만 LateUpdate로 미룬다.
                    // active/complete 추적은 기존과 동일하게 이 시점에 등록한다.
                    if (evt.RequiresPostEvaluation)
                        _deferredExecute.Add(new DeferredEvent
                        {
                            evt = evt,
                            subFrameFraction = ComputeSubFrameFraction(evt),
                            completeAfterExecute = crossedEnd,
                        });
                    else if (evt.dispatchMode == MotionEventDispatchMode.Queued)
                        _queuedEvents.Add(evt);
                    else
                        ExecuteEvent(evt);

                    MarkEntered(evt);
                    _activeEvents.Add(evt);
                    if (crossedEnd && !evt.RequiresPostEvaluation)
                        _crossedEvents.Add(evt);
                }
            }
            foreach (MotionEventBase queuedEvent in _queuedEvents)
                ExecuteEvent(queuedEvent);
            foreach (MotionEventBase crossedEvent in _crossedEvents)
            {
                CompleteEvent(crossedEvent);
                _activeEvents.Remove(crossedEvent);
            }

            MotionSetEventDebugOverlay.Publish(
                TargetObject,
                _currentTime,
                _activeEvents,
                _currentMotionSet.motionSetName);

            _lastTime = _currentTime;
        }

        bool CanEnter(MotionEventBase motionEvent)
        {
            if (motionEvent == null || _activeEvents.Contains(motionEvent))
                return false;
            if (IsBlockedForEnemyTarget(motionEvent))
                return false;
            return motionEvent.reentryPolicy switch
            {
                MotionEventReentryPolicy.EveryCrossing => true,
                MotionEventReentryPolicy.OncePerSectionEntry =>
                    !_sectionExecutedEvents.Contains(motionEvent),
                _ => !_executedEvents.Contains(motionEvent),
            };
        }

        void MarkEntered(MotionEventBase motionEvent)
        {
            if (motionEvent.reentryPolicy == MotionEventReentryPolicy.OncePerSectionEntry)
                _sectionExecutedEvents.Add(motionEvent);
            else if (motionEvent.reentryPolicy == MotionEventReentryPolicy.OncePerPlayback)
                _executedEvents.Add(motionEvent);
        }

        public void EnterSection()
        {
            _sectionExecutedEvents.Clear();
        }

        public void ExitActiveEvents()
        {
            if (_activeEvents.Count == 0)
                return;

            _eventsToRemove.Clear();
            _eventsToRemove.AddRange(_activeEvents);
            _eventsToRemove.Sort((left, right) => right.executionOrder.CompareTo(left.executionOrder));
            foreach (MotionEventBase motionEvent in _eventsToRemove)
                CompleteEvent(motionEvent);
            _activeEvents.Clear();
            _deferredExecute.Clear();
        }

        /// <summary>
        /// 이벤트의 글로벌 시작 시각이 [_lastTime, _currentTime] 구간 어디에 위치하는지 [0,1]로 환산.
        /// 0 = 직전 프레임 포즈, 1 = 현재 프레임 포즈. 공간 이벤트의 프레임 간 보간에 사용한다.
        /// </summary>
        float ComputeSubFrameFraction(MotionEventBase evt)
        {
            float span = _currentTime - _lastTime;
            if (span <= 1e-6f) return 1f; // 시간이 흐르지 않은 프레임은 현재 포즈 사용

            // 캐시된 globalStartTimeOffset 대신 발화 검출과 동일한 누적으로 즉석 재계산(포즈시간과 기준 일치).
            float eventStartGlobal = _currentMotionSet != null &&
                                     MotionTimelineResolver.TryGetEventGlobalRange(
                                         _currentMotionSet,
                                         evt,
                                         out float gs,
                                         out _)
                ? gs
                : evt.startTime + evt.globalStartTimeOffset;

            return Mathf.Clamp01((eventStartGlobal - _lastTime) / span);
        }

        /// <summary>
        /// 이벤트 실행
        /// </summary>
        void ExecuteEvent(MotionEventBase evt, float subFrameFraction = 1f)
        {
            if (evt == null || IsBlockedForEnemyTarget(evt)) return;

            evt.Execute(TargetObject, subFrameFraction);
            if (evt is IMotionEventSignal signal && !string.IsNullOrEmpty(signal.SignalId))
                SignalChanged?.Invoke(signal.SignalId, true);
            EventExecuted?.Invoke(evt);
            MotionSetEventDebugOverlay.RecordEvent(
                $"Start {evt.GetShortLabel()} @{_currentTime:F2}s");
        }

        private bool IsBlockedForEnemyTarget(MotionEventBase motionEvent)
        {
            if (motionEvent == null
                || motionEvent.EnemyExecutionPolicy
                    == MotionEventEnemyExecutionPolicy.Allowed)
            {
                return false;
            }

            GameObject target = TargetObject;
            if (target == null)
                return false;

            // 명시적 event target이 액터의 자식 오브젝트여도 부모 호스트의
            // 실행 범위 정책을 찾아야 한다.
            MonoBehaviour[] components = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++)
                if (components[i] is IMotionEventExecutionScope scope)
                    return scope.IsEnemyMotionEventTarget;
            return false;
        }

        /// <summary>
        /// 본(스켈레톤) 평가가 끝난 뒤(LateUpdate) 호출한다.
        /// 이번 프레임 UpdateTime에서 발화가 결정된 RequiresPostEvaluation 이벤트들을 실행한다.
        /// 이로써 블레이드 본 등 라이브 트랜스폼을 항상 이번 프레임 최종 포즈로 샘플링한다.
        /// </summary>
        public void FlushDeferredEvents()
        {
            if (_deferredExecute.Count == 0) return;

            for (int i = 0; i < _deferredExecute.Count; i++)
            {
                DeferredEvent deferred = _deferredExecute[i];
                ExecuteEvent(deferred.evt, deferred.subFrameFraction);
                if (deferred.completeAfterExecute)
                {
                    CompleteEvent(deferred.evt);
                    _activeEvents.Remove(deferred.evt);
                }
            }

            _deferredExecute.Clear();
        }

        /// <summary>
        /// 타임라인 정지
        /// </summary>
        public void Stop()
        {
            ExitActiveEvents();
            _currentMotionSet = null;
            _executedEvents.Clear();
            _sectionExecutedEvents.Clear();
            // 중단 시점의 부정확한 포즈로 내보내지 않도록 미실행 지연 이벤트는 폐기한다.
            _deferredExecute.Clear();
            MotionSetEventDebugOverlay.Clear();
        }

        void CompleteEvent(MotionEventBase motionEvent)
        {
            motionEvent.OnCompleteEvent(TargetObject);
            if (motionEvent is IMotionEventSignal signal && !string.IsNullOrEmpty(signal.SignalId))
                SignalChanged?.Invoke(signal.SignalId, false);
        }

        static int CompareExecutionOrder(MotionEventBase left, MotionEventBase right)
        {
            int order = left.executionOrder.CompareTo(right.executionOrder);
            if (order != 0)
                return order;
            return left.startTime.CompareTo(right.startTime);
        }

        /// <summary>
        /// 특정 시간으로 점프 (씬 재생 시)
        /// </summary>
        public void SeekTo(float time)
        {
            if (_currentMotionSet == null) return;

            _executedEvents.Clear();
            _sectionExecutedEvents.Clear();
            _deferredExecute.Clear();
            _currentTime = time;

            // 현재 시간까지의 모든 이벤트를 실행된 것으로 표시
            // (이미 지나간 이벤트를 다시 실행하지 않기 위함)
            MotionTimelineResolver.CollectEventsInRange(
                _currentMotionSet,
                float.MinValue,
                time,
                _seekEvents);
            foreach (MotionEventBase motionEvent in _seekEvents)
            {
                if (motionEvent.reentryPolicy == MotionEventReentryPolicy.OncePerSectionEntry)
                    _sectionExecutedEvents.Add(motionEvent);
                else if (motionEvent.reentryPolicy == MotionEventReentryPolicy.OncePerPlayback)
                    _executedEvents.Add(motionEvent);
            }
            _lastTime = time;
        }
        
        /// <summary>
        /// 각 모션의 이벤트를 순회하며 이전 모션들의 누적 재생 시간을 오프셋으로 주입
        /// </summary>
        private void CalculateEventOffsets()
        {
            if (_currentMotionSet == null) return;

            // 글로벌 이벤트는 모션셋 시작점 기준이므로 오프셋이 0입니다.
            if (_currentMotionSet.globalEvents != null)
            {
                foreach (var evt in _currentMotionSet.globalEvents)
                {
                    if (evt != null) evt.globalStartTimeOffset = 0f;
                }
            }

            // 모션별 이벤트는 이전 모션들의 길이를 누적하여 오프셋으로 설정합니다.
            if (_currentMotionSet.motions != null)
            {
                float accumulatedTime = 0f;
                foreach (Motion motion in _currentMotionSet.motions)
                {
                    if(motion == null) continue;
                    
                    if (motion.events != null)
                    {
                        foreach (var evt in motion.events)
                        {
                            if (evt != null) 
                                evt.globalStartTimeOffset = accumulatedTime;
                        }
                    }
                    // 다음 모션으로 넘어가기 전, 현재 모션의 길이를 누적합니다.
                    accumulatedTime += motion.Duration;
                }
            }

            if (_currentMotionSet.layers != null)
            {
                foreach (MotionLayer layer in _currentMotionSet.layers)
                {
                    if (layer == null || !layer.enabled)
                        continue;
                    if (layer.globalEvents != null)
                        foreach (MotionEventBase evt in layer.globalEvents)
                            if (evt != null)
                                evt.globalStartTimeOffset = 0f;

                    float layerOffset = 0f;
                    if (layer.motions == null)
                        continue;
                    foreach (Motion motion in layer.motions)
                    {
                        if (motion?.events != null)
                            foreach (MotionEventBase evt in motion.events)
                                if (evt != null)
                                    evt.globalStartTimeOffset = layerOffset;
                        layerOffset += motion?.Duration ?? 0f;
                    }
                }
            }
        }
        
        /// <summary>
        /// 특정 타입의 이벤트만 실행 (디버그/테스트용)
        /// </summary>
        public void ExecuteEventsByType<T>() where T : MotionEventBase
        {
            if (_currentMotionSet == null) return;

            var events = new List<MotionEventBase>();
            
            // 글로벌 이벤트
            if (_currentMotionSet.globalEvents != null)
            {
                foreach (var evt in _currentMotionSet.globalEvents)
                {
                    if (evt is T)
                        events.Add(evt);
                }
            }

            // 모션 이벤트
            if (_currentMotionSet.motions != null)
            {
                foreach (var motion in _currentMotionSet.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (var evt in motion.events)
                        {
                            if (evt is T)
                                events.Add(evt);
                        }
                    }
                }
            }

            Debug.Log($"Found {events.Count} events of type {typeof(T).Name}");
            foreach (var evt in events)
            {
                ExecuteEvent(evt);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (_currentMotionSet == null) return;

            // 현재 활성화된 이벤트 시각화
            Gizmos.color = Color.yellow;
            foreach (var evt in _activeEvents)
            {
                var label = evt?.GetShortLabel() ?? "Unknown";
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                    $"Active: {label}");
            }
        }
#endif
    }
}
