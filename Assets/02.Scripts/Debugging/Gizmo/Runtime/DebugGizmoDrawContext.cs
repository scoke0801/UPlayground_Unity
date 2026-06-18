using System.Text;
using UnityEngine;

namespace UPlayGround.Debugging
{
    public sealed class DebugGizmoDrawContext
    {
        private static readonly Vector3[] DiscPoints = new Vector3[73];
        private readonly StringBuilder _labelBuilder = new(256);

        public DebugGizmoCategory EnabledCategories { get; private set; }
        public GameObject FocusObject { get; private set; }
        public bool DrawLabels { get; private set; }
        public bool DrawOnlyFocus { get; private set; }
        public float MaxDrawDistance { get; private set; }
        public float Time { get; private set; }
        public Camera SceneCamera { get; private set; }

        public StringBuilder LabelBuilder => _labelBuilder;

        public void Reset(
            DebugGizmoCategory enabledCategories,
            GameObject focusObject,
            bool drawLabels,
            bool drawOnlyFocus,
            float maxDrawDistance,
            float time,
            Camera sceneCamera)
        {
            EnabledCategories = enabledCategories;
            FocusObject = focusObject;
            DrawLabels = drawLabels;
            DrawOnlyFocus = drawOnlyFocus;
            MaxDrawDistance = maxDrawDistance;
            Time = time;
            SceneCamera = sceneCamera;
            _labelBuilder.Clear();
        }

        public bool IsEnabled(DebugGizmoCategory category)
        {
            return (EnabledCategories & category) != 0;
        }

        public bool PassesDistance(Vector3 position)
        {
            if (SceneCamera == null || MaxDrawDistance <= 0f)
                return true;

            return Vector3.Distance(SceneCamera.transform.position, position) <= MaxDrawDistance;
        }

        public void DrawWireDisc(Vector3 center, float radius, Color color, int segments = 72)
        {
            if (radius <= 0f)
                return;

            segments = Mathf.Clamp(segments, 8, DiscPoints.Length - 1);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                DiscPoints[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            Gizmos.color = color;
            for (int i = 1; i <= segments; i++)
                Gizmos.DrawLine(DiscPoints[i - 1], DiscPoints[i]);
        }

        public void DrawLabel(Vector3 position, string text)
        {
            if (!DrawLabels || string.IsNullOrEmpty(text))
                return;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(position, text);
#endif
        }
    }
}
