using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Data.Event;

namespace UPlayGround.Debugging
{
    /// <summary>
    /// MotionSet 이벤트의 범용 실행 상태를 Game/Scene 뷰에 표시한다.
    /// 프로젝트별 진단은 파생 클래스의 BuildAdditionalStatus에서 확장한다.
    /// </summary>
    [MovedFrom(true, sourceAssembly: "UPlayGround.Actor")]
    public class MotionSetEventDebugOverlay : MonoBehaviour
    {
        private const int MaxRecentEvents = 8;

        protected static readonly List<string> ActiveEventNames = new();
        protected static readonly List<string> RecentEventNames = new();

        [SerializeField] private bool _showGameViewOverlay = true;
        [SerializeField] private bool _showSceneLabel = true;
        [SerializeField] private Vector2 _screenOffset = new(16f, 16f);

        protected static GameObject CurrentTarget { get; private set; }
        protected static float CurrentTime { get; private set; }
        protected static string SourceName { get; private set; } = "MotionSet";

        public static void Publish(
            GameObject target,
            float currentTime,
            IEnumerable<MotionEventBase> activeEvents,
            string sourceName = "MotionSet")
        {
            CurrentTarget = target;
            CurrentTime = currentTime;
            SourceName = string.IsNullOrEmpty(sourceName) ? "MotionSet" : sourceName;

            ActiveEventNames.Clear();
            if (activeEvents == null)
                return;

            foreach (MotionEventBase motionEvent in activeEvents)
            {
                if (motionEvent != null)
                    ActiveEventNames.Add(motionEvent.GetShortLabel());
            }
        }

        public static void RecordEvent(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            RecentEventNames.Insert(0, message);
            while (RecentEventNames.Count > MaxRecentEvents)
                RecentEventNames.RemoveAt(RecentEventNames.Count - 1);
        }

        public static void Clear()
        {
            CurrentTarget = null;
            CurrentTime = 0f;
            ActiveEventNames.Clear();
            RecentEventNames.Clear();
        }

        protected virtual string BuildAdditionalStatus(GameObject target) => string.Empty;

        private void OnGUI()
        {
            if (!_showGameViewOverlay || CurrentTarget == null || CurrentTarget != gameObject)
                return;

            string additionalStatus = BuildAdditionalStatus(CurrentTarget);
            const float width = 300f;
            float statusHeight = string.IsNullOrEmpty(additionalStatus) ? 0f : 18f;
            float height = 78f + statusHeight + (ActiveEventNames.Count + RecentEventNames.Count) * 18f;
            Rect rect = new(_screenOffset.x, _screenOffset.y, width, height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label($"{SourceName} Event Debug  {CurrentTime:F2}s");
            if (!string.IsNullOrEmpty(additionalStatus))
                GUILayout.Label(additionalStatus);
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
            if (!_showSceneLabel || CurrentTarget == null || CurrentTarget != gameObject)
                return;

            string active = ActiveEventNames.Count > 0
                ? string.Join(", ", ActiveEventNames)
                : "-";
            string additionalStatus = BuildAdditionalStatus(CurrentTarget);

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"{SourceName} {CurrentTime:F2}s\n{additionalStatus}\nActive: {active}");
        }
#endif
    }
}
