using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;

namespace UPlayGround.Debugging
{
    /// <summary>
    /// MotionSet 테스트 재생 중 이벤트 실행 상태를 Game/Scene 뷰에 표시한다.
    /// 패키지의 MotionEventExecutor와는 MotionEventDebugHook(싱크)으로 연결된다.
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

        // 패키지 Runtime(MotionEventExecutor)이 게임 어셈블리를 참조할 수 없으므로,
        // 정적 훅에 어댑터를 꽂아 재생 디버그 정보를 넘겨받는다.
        private sealed class SinkAdapter : IMotionEventDebugSink
        {
            public void Publish(GameObject target, float currentTime, IEnumerable<MotionEventBase> activeEvents, string sourceName)
                => MotionSetEventDebugOverlay.Publish(target, currentTime, activeEvents, sourceName);

            public void RecordEvent(string message) => MotionSetEventDebugOverlay.RecordEvent(message);

            public void Clear() => MotionSetEventDebugOverlay.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterSinkRuntime() => MotionEventDebugHook.Sink = new SinkAdapter();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterSinkEditor() => MotionEventDebugHook.Sink = new SinkAdapter();
#endif

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
