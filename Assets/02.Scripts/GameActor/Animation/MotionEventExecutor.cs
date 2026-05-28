using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

using UPlayGround.Debugging;
using UPlayGround.MovementController;

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
        private float _lastTime; // 이전 프레임 시간 저장
        private HashSet<MotionEventBase> _activeEvents = new HashSet<MotionEventBase>();
        private HashSet<MotionEventBase> _executedEvents = new HashSet<MotionEventBase>();

        // 인스펙터에서 _targetObject를 지정하지 않은 경우, 부모의 GameActor를 자동 탐색해 캐싱한다.
        // 모션 이벤트들은 target.GetComponent<GameActor>() 로 액터를 찾으므로,
        // Executor가 모델(GameActor의 자식)에 붙은 경우 반드시 부모의 GameActor.gameObject로 해석되어야 한다.
        private GameObject _resolvedTarget;

        public GameObject TargetObject
        {
            get
            {
                if (_targetObject != null) return _targetObject;
                if (_resolvedTarget != null) return _resolvedTarget;

                var actor = GetComponentInParent<GameActor>();
                _resolvedTarget = actor != null ? actor.gameObject : gameObject;
                return _resolvedTarget;
            }
        }

        public void SetTargetObject(GameObject target)
        {
            _targetObject = target;
            _resolvedTarget = target;
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
            List<MotionEventBase> toRemove = new List<MotionEventBase>();
            foreach (var evt in _activeEvents)
            {
                if (evt.IsActiveAtGlobal(_currentTime) == false)
                {
                    evt.OnCompleteEvent(TargetObject);
                    toRemove.Add(evt); // 삭제 예약
                }
            }
            
            // 예약된 요소들 삭제
            foreach (var evt in toRemove)
            {
                _activeEvents.Remove(evt);
                MotionSetEventDebugOverlay.RecordEvent(
                    $"Complete {evt.GetShortLabel()} @{_currentTime:F2}s");
            }

            // 2. 새로운 이벤트들 탐색 및 실행
            // [lastTime, currentTime] 구간에 시작점이 포함된 모든 이벤트 탐색
            var eventsToTrigger = _currentMotionSet.GetEventsInRange(_lastTime, _currentTime);

            foreach (var evt in eventsToTrigger)
            {
                if (!_executedEvents.Contains(evt))
                {
                    ExecuteEvent(evt);
                    _executedEvents.Add(evt);
                    _activeEvents.Add(evt);
                }
            }

            MotionSetEventDebugOverlay.Publish(
                TargetObject,
                _currentTime,
                _activeEvents,
                _currentMotionSet.motionSetName);

            _lastTime = _currentTime;
        }

        /// <summary>
        /// 이벤트 실행
        /// </summary>
        void ExecuteEvent(MotionEventBase evt)
        {
            if (evt == null) return;

            evt.Execute(TargetObject);
            MotionSetEventDebugOverlay.RecordEvent(
                $"Start {evt.GetShortLabel()} @{_currentTime:F2}s");
        }

        /// <summary>
        /// 타임라인 정지
        /// </summary>
        public void Stop()
        {
            _currentMotionSet = null;
            foreach (var evt in _activeEvents)
            {
                evt.OnCompleteEvent(TargetObject);
            }
            _activeEvents.Clear();
            _executedEvents.Clear();
            MotionSetEventDebugOverlay.Clear();
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

namespace UPlayGround.Debugging
{
    /// <summary>
    /// MotionSet 테스트 재생 중 이벤트 실행 상태를 Game/Scene 뷰에 표시한다.
    /// </summary>
    public class MotionSetEventDebugOverlay : MonoBehaviour
    {
        private const int MaxRecentEvents = 8;

        private static readonly List<string> ActiveEventNames = new List<string>();
        private static readonly List<string> RecentEventNames = new List<string>();

        [SerializeField] private bool _showGameViewOverlay = true;
        [SerializeField] private bool _showSceneLabel = true;
        [SerializeField] private Vector2 _screenOffset = new Vector2(16f, 16f);

        private static GameObject _currentTarget;
        private static float _currentTime;
        private static string _sourceName = "MotionSet";
        private static string _warpStatus = string.Empty;

        public static void Publish(
            GameObject target,
            float currentTime,
            IEnumerable<MotionEventBase> activeEvents,
            string sourceName = "MotionSet")
        {
            _currentTarget = target;
            _currentTime = currentTime;
            _sourceName = string.IsNullOrEmpty(sourceName) ? "MotionSet" : sourceName;
            _warpStatus = BuildWarpStatus(target);

            ActiveEventNames.Clear();
            if (activeEvents == null) return;

            foreach (var evt in activeEvents)
            {
                if (evt == null) continue;
                ActiveEventNames.Add(evt.GetShortLabel());
            }
        }

        public static void RecordEvent(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            RecentEventNames.Insert(0, message);
            while (RecentEventNames.Count > MaxRecentEvents)
                RecentEventNames.RemoveAt(RecentEventNames.Count - 1);
        }

        public static void Clear()
        {
            _currentTarget = null;
            _currentTime = 0f;
            ActiveEventNames.Clear();
            RecentEventNames.Clear();
            _warpStatus = string.Empty;
        }

        private void OnGUI()
        {
            if (!_showGameViewOverlay) return;
            if (_currentTarget == null || _currentTarget != gameObject) return;

            const float width = 300f;
            float height = 78f + (ActiveEventNames.Count + RecentEventNames.Count) * 18f;
            Rect rect = new Rect(_screenOffset.x, _screenOffset.y, width, height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label($"{_sourceName} Event Debug  {_currentTime:F2}s");
            if (!string.IsNullOrEmpty(_warpStatus))
                GUILayout.Label(_warpStatus);
            DrawList("Active", ActiveEventNames);
            DrawList("Recent", RecentEventNames);
            GUILayout.EndArea();
        }

        private static void DrawList(string label, IReadOnlyList<string> values)
        {
            GUILayout.Label($"{label}: {(values.Count == 0 ? "-" : string.Empty)}");
            for (int i = 0; i < values.Count; i++)
                GUILayout.Label($"  {values[i]}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showSceneLabel) return;
            if (_currentTarget == null || _currentTarget != gameObject) return;

            string active = ActiveEventNames.Count > 0
                ? string.Join(", ", ActiveEventNames)
                : "-";

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"{_sourceName} {_currentTime:F2}s\n{_warpStatus}\nActive: {active}");
        }
#endif

        private static string BuildWarpStatus(GameObject target)
        {
            if (target == null) return string.Empty;

            var controller = target.GetComponent<ActorMovementController>()
                          ?? target.GetComponentInParent<ActorMovementController>()
                          ?? target.GetComponentInChildren<ActorMovementController>();
            if (controller == null || controller.MotionWarp == null)
                return string.Empty;

            var warp = controller.MotionWarp;
            if (warp.IsApplicable)
                return $"Warp: 적용 / 오차 {warp.LastArrivalError:F2}m";

            if (!string.IsNullOrEmpty(warp.LastFailureReason))
                return $"Warp: {warp.LastFailureReason}";

            return "Warp: 대기";
        }
    }
}
