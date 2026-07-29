using System.Collections.Generic;
using UPlayGround.Animation.Editor.UIToolkit;
using UPlayGround.Animation.Editor.UIToolkit.Timeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Animation.Editor
{
    public sealed partial class MotionSetEditorWindow
    {
        private const string SelectedPanelPrefs =
            "MotionSetEditor.SelectedExtensionPanel";

        private MotionEditorShell _uiToolkitShell;
        private MotionListView _motionListView;
        private MotionEventInspectorView _eventInspectorView;
        private TimelineView _timelineView;
        private IMGUIContainer _motionAuthoringContainer;
        private VisualElement _extensionPanelBody;
        private readonly List<Button> _extensionPanelButtons = new();
        private int _selectedExtensionPanel;

        public void CreateGUI()
        {
            DisposeUIToolkit();

            _motionListView = new MotionListView(
                SelectMotionListItem,
                ShowAssignableSlotMenu);
            _eventInspectorView = new MotionEventInspectorView(Repaint);

            _uiToolkitShell = new MotionEditorShell(
                rootVisualElement,
                DrawToolbar,
                BuildExtensionPanelTabs(),
                DrawPreviewControlsUIToolkit,
                _motionListView,
                BuildMotionEditorBody(),
                _eventInspectorView,
                RunUIToolkitSideEffects);
            _uiToolkitShell.SetSidebarVisible(_catalog != null);

            rootVisualElement.RegisterCallback<KeyDownEvent>(
                HandleEditorShortcut,
                TrickleDown.TrickleDown);
            RefreshEditorViews();
        }

        private VisualElement BuildMotionEditorBody()
        {
            var root = new VisualElement();
            root.AddToClassList("up-motion-editor-body");

            var authoringFoldout = new Foldout
            {
                text = "MotionSet 구성 · 이벤트 목록",
                value = false,
            };
            authoringFoldout.AddToClassList("up-motion-authoring-foldout");
            _motionAuthoringContainer = new IMGUIContainer(DrawMotionAuthoring);
            authoringFoldout.Add(_motionAuthoringContainer);
            root.Add(authoringFoldout);

            _timelineView = new TimelineView(
                () => CurrentSet,
                () => _drawer,
                () => _asset,
                HandleTimelineChanged,
                HandleTimelineScrubbed);
            root.Add(_timelineView);
            return root;
        }

        private VisualElement BuildExtensionPanelTabs()
        {
            var root = new VisualElement();
            root.AddToClassList("up-control-panels");

            var tabs = new UnityEditor.UIElements.Toolbar();
            tabs.AddToClassList("up-control-panel-tabs");
            root.Add(tabs);

            IReadOnlyList<IMotionEditorPanel> panels =
                MotionEditorExtensionRegistry.Panels;
            _selectedExtensionPanel = Mathf.Clamp(
                EditorPrefs.GetInt(SelectedPanelPrefs, 0),
                0,
                Mathf.Max(0, panels.Count - 1));

            _extensionPanelButtons.Clear();
            for (int index = 0; index < panels.Count; index++)
            {
                int capturedIndex = index;
                var button = new Button(
                    () => SelectExtensionPanel(capturedIndex))
                {
                    text = panels[index].Title,
                };
                button.AddToClassList("up-control-panel-tab");
                tabs.Add(button);
                _extensionPanelButtons.Add(button);
            }

            _extensionPanelBody = new VisualElement();
            _extensionPanelBody.AddToClassList("up-control-panel-body");
            root.Add(_extensionPanelBody);
            RebuildSelectedExtensionPanel();
            return root;
        }

        private void SelectExtensionPanel(int index)
        {
            if (index == _selectedExtensionPanel)
                return;

            _selectedExtensionPanel = index;
            EditorPrefs.SetInt(SelectedPanelPrefs, index);
            // 이전 패널이 남긴 오버레이 트랙이 탭 전환 후에도 타임라인에 남지 않게 한다.
            // 새 패널이 자기 트랙을 갖고 있으면 첫 OnGUI에서 다시 밀어 넣는다.
            SetOverlayTracks(null, null);
            RebuildSelectedExtensionPanel();
        }

        private void RebuildSelectedExtensionPanel()
        {
            if (_extensionPanelBody == null)
                return;

            _extensionPanelBody.Clear();
            IReadOnlyList<IMotionEditorPanel> panels =
                MotionEditorExtensionRegistry.Panels;
            if (panels.Count == 0)
            {
                _extensionPanelBody.style.display = DisplayStyle.None;
                return;
            }

            _selectedExtensionPanel = Mathf.Clamp(
                _selectedExtensionPanel,
                0,
                panels.Count - 1);
            for (int index = 0; index < _extensionPanelButtons.Count; index++)
            {
                _extensionPanelButtons[index].EnableInClassList(
                    "up-tab-selected",
                    index == _selectedExtensionPanel);
            }

            IMotionEditorPanel panel = panels[_selectedExtensionPanel];
            _extensionPanelBody.style.display = panel.IsAvailable(this)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (panel.IsAvailable(this))
            {
                _extensionPanelBody.Add(
                    new IMGUIContainer(() => panel.OnGUI(this)));
            }
        }

        private void DrawPreviewControlsUIToolkit()
        {
            DrawCatalog();
            DrawPreviewSubject();
            DrawPlayback();
        }

        private void DrawMotionAuthoring()
        {
            MotionSet set = CurrentSet;
            if (set == null)
            {
                EditorGUILayout.HelpBox(
                    "MotionSetAsset 또는 카탈로그 슬롯을 선택하세요.",
                    MessageType.Info);
                return;
            }

            _drawer.DrawFullGUI(set);
            _drawer.DrawEventsGUI(set);
        }

        private void RefreshMotionList()
        {
            if (_motionListView == null)
                return;

            var items = new List<MotionListView.Item>();
            IReadOnlyList<MotionSetSlot> slots = _catalog?.Slots;
            if (slots != null)
            {
                foreach (MotionSetSlot slot in slots)
                {
                    MotionSetAsset slotAsset = _catalog.Resolve(slot.SlotId);
                    items.Add(new MotionListView.Item
                    {
                        Group = string.IsNullOrEmpty(slot.GroupLabel)
                            ? "기타"
                            : slot.GroupLabel,
                        Title = slot.DisplayName,
                        Subtitle = slotAsset != null
                            ? slotAsset.name
                            : "미할당",
                        UserData = slot,
                        IsSelected = slot.SlotId == _selectedSlotId,
                    });
                }
            }
            else if (_asset != null)
            {
                items.Add(new MotionListView.Item
                {
                    Group = "현재 MotionSet",
                    Title = _asset.name,
                    Subtitle = CurrentSet?.motionSetName,
                    UserData = _asset,
                    IsSelected = true,
                });
            }

            _motionListView.SetItems(items);
            _uiToolkitShell?.SetSidebarVisible(
                _catalog != null || _asset != null);
        }

        private void SelectMotionListItem(MotionListView.Item item)
        {
            if (item?.UserData is MotionSetSlot slot)
                SelectSlot(slot.SlotId);
            else if (item?.UserData is MotionSetAsset asset)
                SetAsset(asset);
        }

        private void QueueEditorViewRefresh()
        {
            if (rootVisualElement?.panel == null)
            {
                Repaint();
                return;
            }

            rootVisualElement.schedule.Execute(RefreshEditorViews);
        }

        private void RefreshEditorViews()
        {
            RefreshMotionList();
            _eventInspectorView?.Refresh(_asset, _drawer);
            _timelineView?.RefreshData(true);
            _motionAuthoringContainer?.MarkDirtyRepaint();
            RebuildSelectedExtensionPanel();
            Repaint();
        }

        private void HandleTimelineChanged()
        {
            if (_asset != null)
                EditorUtility.SetDirty(_asset);
            RefreshEditorViews();
        }

        private void HandleTimelineScrubbed()
        {
            SetPlaybackTime(_drawer?.cursorTime ?? 0f);
        }

        private void RunUIToolkitSideEffects()
        {
            _timelineView?.RefreshIfChanged();
        }

        private void HandleEditorShortcut(KeyDownEvent evt)
        {
            // Ctrl+S(저장) 같은 조합키를 재생 단축키로 오인해 삼키지 않는다.
            if (evt.ctrlKey || evt.commandKey || evt.altKey || evt.shiftKey)
                return;
            if (IsTextInputTarget(evt.target))
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Space:
                    if (_isPlaying)
                        TogglePause();
                    else
                        StartPlayback();
                    evt.StopImmediatePropagation();
                    break;
                case KeyCode.S:
                    StopPlayback();
                    evt.StopImmediatePropagation();
                    break;
            }
        }

        /// <summary>
        /// 문자 입력이나 Space 활성화를 소비하는 대상인지 판별한다.
        /// IMGUIContainer는 내부 텍스트 필드가 이벤트 target으로 노출되지 않으므로
        /// 컨테이너 단위로 제외한다.
        /// </summary>
        private static bool IsTextInputTarget(IEventHandler target)
        {
            if (target is not VisualElement element)
                return false;

            return element is IMGUIContainer ||
                   element is TextElement ||
                   element is TextField ||
                   element is Toggle ||
                   element.GetFirstAncestorOfType<TextField>() != null ||
                   element.GetFirstAncestorOfType<IMGUIContainer>() != null ||
                   element.GetFirstAncestorOfType<Toggle>() != null;
        }

        private void DisposeUIToolkit()
        {
            rootVisualElement?.UnregisterCallback<KeyDownEvent>(
                HandleEditorShortcut,
                TrickleDown.TrickleDown);
            _uiToolkitShell?.Dispose();
            _uiToolkitShell = null;
            _motionListView = null;
            _eventInspectorView = null;
            _timelineView = null;
            _motionAuthoringContainer = null;
            _extensionPanelBody = null;
            _extensionPanelButtons.Clear();
        }
    }
}
