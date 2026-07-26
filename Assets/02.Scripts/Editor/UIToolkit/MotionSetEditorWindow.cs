using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Animation.Editor.UIToolkit.Timeline;

namespace UPlayGround.Animation.Editor
{
    public partial class MotionSetEditorWindow
    {
        TimelineView _timelineView;
        IMGUIContainer _motionAuthoringContainer;

        VisualElement BuildMotionEditorBodyUIToolkit()
        {
            var root = new VisualElement();
            root.AddToClassList("up-motion-editor-body");

            var authoring = new Foldout
            {
                text = "MotionSet 구성 · 이벤트 목록",
                value = false,
            };
            authoring.AddToClassList("up-motion-authoring-foldout");
            _motionAuthoringContainer = new IMGUIContainer(DrawMotionAuthoringBody);
            _motionAuthoringContainer.AddToClassList("up-motion-authoring-imgui");
            var authoringScroll = new ScrollView(ScrollViewMode.Vertical)
            {
                verticalScrollerVisibility = ScrollerVisibility.Auto,
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            authoringScroll.AddToClassList("up-motion-authoring-scroll");
            authoringScroll.Add(_motionAuthoringContainer);
            authoring.Add(authoringScroll);
            root.Add(authoring);

            _timelineView = new TimelineView(
                GetCurrentMotionSet,
                () => _drawer,
                () => _asset,
                HandleTimelineDataChanged,
                HandleTimelineScrubbing);
            root.Add(_timelineView);
            return root;
        }

        void DrawMotionAuthoringBody()
        {
            MotionSet currentSet = GetCurrentMotionSet();
            if (currentSet == null)
            {
                DrawEmptyState();
                return;
            }

            EditorGUI.BeginChangeCheck();
            _drawer.DrawFullGUI(currentSet);
            EditorGUILayout.Space(4);
            _drawer.DrawEventsGUI(currentSet);
            if (EditorGUI.EndChangeCheck() || GUI.changed)
            {
                if (_asset != null)
                    EditorUtility.SetDirty(_asset);
                _timelineView?.RefreshData(true);
            }
        }

        void HandleTimelineDataChanged()
        {
            _motionAuthoringContainer?.MarkDirtyRepaint();
            QueueEventInspectorRefresh();
        }
    }
}
