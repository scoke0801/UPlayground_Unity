using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Animation
{
    /// <summary>
    /// 모션 이벤트 실행 매니저
    /// 애니메이션 재생 중 이벤트를 감지하고 실행
    /// </summary>
    public class MotionEventExecutor : MonoBehaviour
    {
        [SerializeField] GameObject _targetObject;
        
        private MotionSet _currentMotionSet;
        private float _currentTime;
        private HashSet<MotionEventBase> _activeEvents = new HashSet<MotionEventBase>();
        private HashSet<MotionEventBase> _executedEvents = new HashSet<MotionEventBase>();

        public GameObject TargetObject => _targetObject != null ? _targetObject : gameObject;

        /// <summary>
        /// 모션 셋 재생 시작
        /// </summary>
        public void PlayMotionSet(MotionSet motionSet)
        {
            _currentMotionSet = motionSet;
            _currentTime = 0f;
            _activeEvents.Clear();
            _executedEvents.Clear();
        }

        /// <summary>
        /// 타임라인 시간 업데이트 (매 프레임 호출)
        /// </summary>
        public void UpdateTime(float time)
        {
            if (_currentMotionSet == null) return;

            _currentTime = time;
            ProcessEvents();
        }

        /// <summary>
        /// 현재 시간의 이벤트 처리
        /// </summary>
        void ProcessEvents()
        {
            var currentActiveEvents = _currentMotionSet.GetActiveEventsAt(_currentTime);

            // 새로 활성화된 이벤트 실행
            foreach (var evt in currentActiveEvents)
            {
                if (!_executedEvents.Contains(evt))
                {
                    ExecuteEvent(evt);
                    _executedEvents.Add(evt);
                }
            }

            // 비활성화된 이벤트 정리
            _activeEvents.Clear();
            _activeEvents.UnionWith(currentActiveEvents);
        }

        /// <summary>
        /// 이벤트 실행
        /// </summary>
        void ExecuteEvent(MotionEventBase evt)
        {
            if (evt == null) return;

            try
            {
                evt.Execute(TargetObject);
                Debug.Log($"[MotionEvent] Executed: {evt.GetDisplayName()} at {_currentTime:F2}s");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MotionEvent] Error executing {evt.GetDisplayName()}: {e.Message}");
            }
        }

        /// <summary>
        /// 타임라인 정지
        /// </summary>
        public void Stop()
        {
            _currentMotionSet = null;
            _activeEvents.Clear();
            _executedEvents.Clear();
        }

        /// <summary>
        /// 특정 시간으로 점프 (씬 재생 시)
        /// </summary>
        public void SeekTo(float time)
        {
            if (_currentMotionSet == null) return;

            _executedEvents.Clear();
            _currentTime = time;

            // 현재 시간까지의 모든 이벤트를 실행된 것으로 표시
            // (이미 지나간 이벤트를 다시 실행하지 않기 위함)
            if (_currentMotionSet.globalEvents != null)
            {
                foreach (var evt in _currentMotionSet.globalEvents)
                {
                    if (evt != null && evt.startTime <= time)
                        _executedEvents.Add(evt);
                }
            }

            if (_currentMotionSet.motions != null)
            {
                float tOff = 0f;
                foreach (var motion in _currentMotionSet.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (var evt in motion.events)
                        {
                            if (evt != null && (tOff + evt.startTime) <= time)
                                _executedEvents.Add(evt);
                        }
                    }
                    tOff += motion.Duration;
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