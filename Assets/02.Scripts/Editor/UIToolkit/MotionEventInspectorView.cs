using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor.UIToolkit
{
    internal sealed class MotionEventInspectorView : VisualElement
    {
        readonly ScrollView _content;
        readonly Action _requestRepaint;
        MotionSetAsset _asset;
        MotionSetDrawer _drawer;
        SerializedObject _serializedObject;

        public MotionEventInspectorView(Action requestRepaint)
        {
            _requestRepaint = requestRepaint;
            AddToClassList("up-motion-inspector");

            var title = new Label("인스펙터");
            title.AddToClassList("up-panel-title");
            Add(title);

            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("up-motion-inspector-content");
            _content.RegisterCallback<SerializedPropertyChangeEvent>(HandlePropertyChanged);
            Add(_content);

            RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += HandleUndoRedo);
            RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= HandleUndoRedo);
        }

        public void Refresh(MotionSetAsset asset, MotionSetDrawer drawer)
        {
            _content.Unbind();
            _content.Clear();
            _asset = asset;
            _drawer = drawer;
            _serializedObject = asset != null ? new SerializedObject(asset) : null;

            if (_serializedObject == null || drawer == null || asset.motionSet == null)
            {
                AddHint("MotionSet 에셋을 선택하세요.");
                return;
            }

            _serializedObject.Update();
            SerializedProperty selectedEvent = FindSelectedEventProperty();
            if (selectedEvent != null)
            {
                BuildEventInspector(selectedEvent, drawer.GetSelectedEvent(asset.motionSet));
                _content.Bind(_serializedObject);
                return;
            }

            SerializedProperty selectedMotion = FindSelectedMotionProperty();
            if (selectedMotion != null)
            {
                var label = new Label($"모션 #{drawer.selectedMotionIndex}");
                label.AddToClassList("up-inspector-selection-title");
                _content.Add(label);
                _content.Add(new PropertyField(selectedMotion));
                _content.Bind(_serializedObject);
                return;
            }

            AddHint("타임라인 또는 모션 목록에서 편집할 대상을 선택하세요.");
        }

        void BuildEventInspector(SerializedProperty eventProperty, MotionEventBase selectedEvent)
        {
            string eventName = selectedEvent?.GetDisplayName() ?? "이벤트";
            var label = new Label(eventName);
            label.AddToClassList("up-inspector-selection-title");
            _content.Add(label);

            var timingTitle = new Label("TIMING");
            timingTitle.AddToClassList("up-inspector-section-title");
            _content.Add(timingTitle);

            SerializedProperty start = eventProperty.FindPropertyRelative("startTime");
            SerializedProperty end = eventProperty.FindPropertyRelative("endTime");
            if (start != null)
                _content.Add(new PropertyField(start, "Start"));
            if (end != null)
                _content.Add(new PropertyField(end, "End"));

            var propertiesTitle = new Label("PROPERTIES");
            propertiesTitle.AddToClassList("up-inspector-section-title");
            _content.Add(propertiesTitle);
            AddConcreteEventProperties(eventProperty);

            if (selectedEvent != null && _drawer.onDrawEventToolPanel != null)
            {
                var toolPanel = new IMGUIContainer(
                    () => _drawer.onDrawEventToolPanel?.Invoke(selectedEvent));
                toolPanel.AddToClassList("up-motion-event-tool-panel");
                _content.Add(toolPanel);
            }
        }

        void AddConcreteEventProperties(SerializedProperty eventProperty)
        {
            SerializedProperty iterator = eventProperty.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            int directChildDepth = eventProperty.depth + 1;
            bool added = false;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                if (iterator.depth != directChildDepth)
                    continue;
                if (iterator.name is "startTime" or "endTime" or "globalStartTimeOffset")
                    continue;

                _content.Add(new PropertyField(iterator.Copy()));
                added = true;
            }

            if (!added)
                AddHint("추가 속성이 없는 이벤트입니다.");
        }

        SerializedProperty FindSelectedEventProperty()
        {
            if (_drawer.selectedEventIndex < 0)
                return null;

            string path = _drawer.selectedEventIsSetEvent
                ? $"motionSet.globalEvents.Array.data[{_drawer.selectedEventIndex}]"
                : $"motionSet.motions.Array.data[{_drawer.selectedEventMotionIndex}].events.Array.data[{_drawer.selectedEventIndex}]";
            return _serializedObject.FindProperty(path);
        }

        SerializedProperty FindSelectedMotionProperty()
        {
            if (_drawer.selectedMotionIndex < 0)
                return null;
            return _serializedObject.FindProperty(
                $"motionSet.motions.Array.data[{_drawer.selectedMotionIndex}]");
        }

        void HandlePropertyChanged(SerializedPropertyChangeEvent evt)
        {
            if (_asset != null)
                EditorUtility.SetDirty(_asset);
            _requestRepaint?.Invoke();
        }

        void HandleUndoRedo()
        {
            Refresh(_asset, _drawer);
            _requestRepaint?.Invoke();
        }

        void AddHint(string message)
        {
            var hint = new Label(message);
            hint.AddToClassList("up-empty-hint");
            _content.Add(hint);
        }
    }
}
