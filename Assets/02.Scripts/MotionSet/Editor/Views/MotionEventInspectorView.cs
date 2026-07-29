using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor.UIToolkit
{
    public sealed class MotionEventInspectorView : VisualElement
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

            var header = new VisualElement();
            header.AddToClassList("up-panel-header");
            var kicker = new Label("SELECTION");
            kicker.AddToClassList("up-panel-kicker");
            header.Add(kicker);
            var title = new Label("이벤트 인스펙터");
            title.AddToClassList("up-panel-title");
            header.Add(title);
            Add(header);

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
                string titleText = drawer.selectedLayerIndex >= 0
                    ? $"L{drawer.selectedLayerIndex + 1} 모션 #{drawer.selectedMotionIndex}"
                    : $"모션 #{drawer.selectedMotionIndex}";
                var label = new Label(titleText);
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
            if (selectedEvent != null)
            {
                MotionEventStyle.EventVisual visual = MotionEventStyle.Get(selectedEvent);
                label.text = $"{visual.icon}  {eventName}";
                label.style.borderLeftColor = visual.color;
            }
            _content.Add(label);

            VisualElement timing = AddSection("TIMING", "이벤트 구간");

            SerializedProperty start = eventProperty.FindPropertyRelative("startTime");
            SerializedProperty end = eventProperty.FindPropertyRelative("endTime");
            if (start != null)
                timing.Add(new PropertyField(start, "Start"));
            if (end != null)
                timing.Add(new PropertyField(end, "End"));

            VisualElement properties = AddSection("PROPERTIES", "이벤트 속성");
            AddConcreteEventProperties(eventProperty, properties);

            if (selectedEvent != null && _drawer.onDrawEventToolPanel != null)
            {
                var toolPanel = new IMGUIContainer(
                    () => _drawer.onDrawEventToolPanel?.Invoke(selectedEvent));
                toolPanel.AddToClassList("up-motion-event-tool-panel");
                _content.Add(toolPanel);
            }
        }

        VisualElement AddSection(string kickerText, string titleText)
        {
            var section = new VisualElement();
            section.AddToClassList("up-inspector-section");

            var heading = new VisualElement();
            heading.AddToClassList("up-inspector-section-heading");
            var kicker = new Label(kickerText);
            kicker.AddToClassList("up-inspector-section-kicker");
            heading.Add(kicker);
            var title = new Label(titleText);
            title.AddToClassList("up-inspector-section-title");
            heading.Add(title);
            section.Add(heading);

            var body = new VisualElement();
            body.AddToClassList("up-inspector-section-body");
            section.Add(body);
            _content.Add(section);
            return body;
        }

        void AddConcreteEventProperties(SerializedProperty eventProperty, VisualElement container)
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

                container.Add(new PropertyField(iterator.Copy()));
                added = true;
            }

            if (!added)
            {
                var hint = new Label("추가 속성이 없는 이벤트입니다.");
                hint.AddToClassList("up-empty-hint");
                container.Add(hint);
            }
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
            string path = _drawer.selectedLayerIndex >= 0
                ? $"motionSet.layers.Array.data[{_drawer.selectedLayerIndex}].motions.Array.data[{_drawer.selectedMotionIndex}]"
                : $"motionSet.motions.Array.data[{_drawer.selectedMotionIndex}]";
            return _serializedObject.FindProperty(path);
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
