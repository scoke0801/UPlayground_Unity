#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Animation.Editor;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Data.Editor
{
    /// <summary>
    /// 궁극기 시퀀스 에디터 (UI Toolkit).
    /// 인터랙티브 타임라인(드래그 이동/리사이즈·다중 선택·레인 패킹),
    /// 선택 이벤트 인스펙터, 라이브 검증, 에셋 간 복사/붙여넣기, PlayMode 테스트를 제공한다.
    /// </summary>
    public sealed class UltimateSequenceEditorWindow : EditorWindow
    {
        private const string CommonStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/UPlayGroundEditor.uss";
        private const string UltimateStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/UltimateSequenceEditor.uss";
        private const string InspectorWidthPrefs =
            "UltimateSequenceEditor.InspectorWidth";
        private const string BasicSettingsPrefs =
            "UltimateSequenceEditor.BasicSettingsExpanded";
        private const string GameplaySettingsPrefs =
            "UltimateSequenceEditor.GameplaySettingsExpanded";
        private const string TargetSettingsPrefs =
            "UltimateSequenceEditor.TargetSettingsExpanded";
        private const string StageSettingsPrefs =
            "UltimateSequenceEditor.StageSettingsExpanded";

        private UltimateSequenceAsset _asset;
        private SerializedObject _serialized;

        private ObjectField _assetField;
        private Label _pill;
        private ToolbarButton _pasteButton;
        private ToolbarButton _motionLink;
        private ToolbarButton _cameraLink;
        private ToolbarButton _recordLink;

        private VisualElement _objTracker;
        private VisualElement _validationSection;
        private VisualElement _settingsSection;
        private VisualElement _eventSection;
        private VisualElement _inspectorPane;
        private Label _inspectorTitle;

        private UltimateTimelineTrackView _timeline;
        private Slider _zoom;
        private Toggle _snapToggle;
        private IntegerField _fpsField;
        private Label _timeLabel;
        private Button _playButton;
        private Button _stopButton;

        private float _pps = 80f;
        private bool _snap;
        private int _fps = 30;
        private IVisualElementScheduledItem _pollItem;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/캐릭터/궁극기/궁극기 시퀀스 에디터", priority = 140)]
        public static void Open()
        {
            GetWindow<UltimateSequenceEditorWindow>("궁극기 시퀀스");
        }

        public static void Open(UltimateSequenceAsset asset)
        {
            var window = GetWindow<UltimateSequenceEditorWindow>("궁극기 시퀀스");
            window.Show();
            window.Focus();
            window.BindAssetSafe(asset);
        }

        public void CreateGUI()
        {
            minSize = new Vector2(900f, 560f);

            VisualElement root = rootVisualElement;
            root.Clear();
            root.AddToClassList("up-editor-root");
            root.AddToClassList("up-ult-root");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "up-theme-dark" : "up-theme-light");
            LoadStyle(root, CommonStylePath);
            LoadStyle(root, UltimateStylePath);

            root.Add(BuildToolbar());

            _objTracker = new VisualElement { style = { height = 0f } };
            root.Add(_objTracker);

            float inspectorWidth = Mathf.Clamp(
                EditorPrefs.GetFloat(InspectorWidthPrefs, 360f),
                340f,
                520f);
            var split = new TwoPaneSplitView(1, inspectorWidth, TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("up-ult-split");
            split.Add(BuildTimelinePane());
            split.Add(BuildInspectorPane());
            root.Add(split);

            _pollItem = root.schedule.Execute(PollPlayMode).Every(60);

            BindAsset(_asset);
        }

        private void OnDisable()
        {
            _pollItem?.Pause();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is UltimateSequenceAsset selected)
                BindAssetSafe(selected);
        }

        // ── 상단 툴바 ─────────────────────────────────────
        private VisualElement BuildToolbar()
        {
            var bar = new Toolbar();
            bar.AddToClassList("up-ult-toolbar");

            _assetField = new ObjectField
            {
                objectType = typeof(UltimateSequenceAsset),
                allowSceneObjects = false,
                value = _asset,
            };
            _assetField.AddToClassList("up-ult-toolbar-asset");
            _assetField.RegisterValueChangedCallback(e => BindAsset(e.newValue as UltimateSequenceAsset));
            bar.Add(_assetField);

            _pill = new Label("—");
            _pill.AddToClassList("up-ult-pill");
            bar.Add(_pill);

            bar.Add(new VisualElement { name = "spacer" }.WithClass("up-ult-spacer"));

            var addMenu = new ToolbarMenu { text = "＋ 이벤트" };
            foreach (UltimateEventClipboard.EventKind kind in UltimateEventClipboard.Kinds)
            {
                UltimateEventClipboard.EventKind captured = kind;
                addMenu.menu.AppendAction(
                    captured.Label,
                    _ => AddEvent(captured.Type),
                    _ => _asset != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            bar.Add(addMenu);

            _pasteButton = new ToolbarButton(PasteClipboard) { text = "붙여넣기" };
            bar.Add(_pasteButton);

            var links = new VisualElement();
            links.AddToClassList("up-ult-quicklinks");
            _motionLink = new ToolbarButton(OpenMotionEditor) { text = "MotionSet" };
            _cameraLink = new ToolbarButton(OpenCameraEditor) { text = "카메라 스냅샷" };
            _recordLink = new ToolbarButton(OpenCameraRecorder) { text = "동기 촬영" };
            links.Add(_motionLink);
            links.Add(_cameraLink);
            links.Add(_recordLink);
            bar.Add(links);

            var createMenu = new ToolbarMenu { text = "에셋 생성" };
            foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None)
                    continue;
                CharacterActorType captured = type;
                createMenu.menu.AppendAction(captured.ToString(), _ => CreateAsset(captured));
            }

            bar.Add(createMenu);
            return bar;
        }

        // ── 타임라인 패널 ─────────────────────────────────
        private VisualElement BuildTimelinePane()
        {
            var pane = new VisualElement();
            pane.AddToClassList("up-ult-timeline-pane");

            var toolbar = new VisualElement();
            toolbar.AddToClassList("up-ult-timeline-toolbar");

            var titleBlock = new VisualElement();
            titleBlock.AddToClassList("up-ult-timeline-title-block");
            var kicker = new Label("SEQUENCE");
            kicker.AddToClassList("up-ult-timeline-kicker");
            titleBlock.Add(kicker);
            var title = new Label("연출 타임라인");
            title.AddToClassList("up-ult-timeline-title");
            titleBlock.Add(title);
            toolbar.Add(titleBlock);

            toolbar.Add(new VisualElement().WithClass("up-ult-toolbar-divider"));

            var zoomLabel = new Label("줌");
            zoomLabel.AddToClassList("up-ult-control-label");
            toolbar.Add(zoomLabel);
            _zoom = new Slider(10f, 400f) { value = _pps };
            _zoom.AddToClassList("up-ult-zoom");
            _zoom.RegisterValueChangedCallback(e =>
            {
                _pps = e.newValue;
                _timeline.SetPixelsPerSecond(_pps);
            });
            toolbar.Add(_zoom);

            _snapToggle = new Toggle("스냅") { value = _snap };
            _snapToggle.AddToClassList("up-ult-snap");
            _snapToggle.RegisterValueChangedCallback(e =>
            {
                _snap = e.newValue;
                _timeline.SetSnap(_snap, _fps);
            });
            toolbar.Add(_snapToggle);

            _fpsField = new IntegerField("FPS") { value = _fps };
            _fpsField.AddToClassList("up-ult-fps");
            _fpsField.RegisterValueChangedCallback(e =>
            {
                _fps = Mathf.Max(1, e.newValue);
                _fpsField.SetValueWithoutNotify(_fps);
                _timeline.SetSnap(_snap, _fps);
            });
            toolbar.Add(_fpsField);

            toolbar.Add(new VisualElement().WithClass("up-ult-spacer"));

            _timeLabel = new Label("에디트 모드");
            _timeLabel.AddToClassList("up-ult-time-label");
            toolbar.Add(_timeLabel);

            _playButton = new Button(RunTest) { text = "▶ 테스트" };
            _playButton.AddToClassList("up-ult-tool-btn");
            toolbar.Add(_playButton);

            _stopButton = new Button(StopTest) { text = "■ 중단" };
            _stopButton.AddToClassList("up-ult-tool-btn");
            toolbar.Add(_stopButton);

            pane.Add(toolbar);

            _timeline = new UltimateTimelineTrackView(
                () => _asset,
                ResolveMotionDuration,
                () => _asset,
                OnTimelineDataChanged,
                RefreshEventInspector)
            {
                CopySelected = CopySelected,
                DeleteSelected = DeleteSelected,
                DuplicateSelected = DuplicateSelected,
                PasteClipboard = PasteClipboard,
                CanPaste = () => UltimateEventClipboard.HasContent,
                AddEvent = AddEvent,
                AddEventAtTime = AddEventAtTime,
            };
            _timeline.SetSnap(_snap, _fps);
            _timeline.SetPixelsPerSecond(_pps);

            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.AddToClassList("up-ult-scroll");
            scroll.Add(_timeline);

            // ScrollView 자체가 아닌 외부 호스트 크기를 기준으로 캔버스를 채운다.
            // 콘텐츠 크기 → 스크롤바 → 뷰포트 크기의 순환 계산을 피하기 위한 구조다.
            var timelineViewport = new VisualElement();
            timelineViewport.AddToClassList("up-ult-timeline-viewport");
            timelineViewport.Add(scroll);
            timelineViewport.RegisterCallback<GeometryChangedEvent>(evt =>
                _timeline.SetViewportSize(evt.newRect.size));
            pane.Add(timelineViewport);
            return pane;
        }

        // ── 인스펙터 패널 ─────────────────────────────────
        private VisualElement BuildInspectorPane()
        {
            _inspectorPane = new VisualElement();
            _inspectorPane.AddToClassList("up-ult-inspector-pane");
            _inspectorPane.RegisterCallback<GeometryChangedEvent>(SaveInspectorWidth);

            var header = new VisualElement();
            header.AddToClassList("up-ult-inspector-header");
            var kicker = new Label("ULTIMATE SEQUENCE");
            kicker.AddToClassList("up-ult-inspector-kicker");
            header.Add(kicker);
            _inspectorTitle = new Label("이벤트 인스펙터");
            _inspectorTitle.AddToClassList("up-ult-inspector-title");
            header.Add(_inspectorTitle);
            _inspectorPane.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("up-ult-inspector-scroll");

            // Motion 에디터처럼 현재 선택을 가장 먼저 보여준다.
            _eventSection = new VisualElement();
            _eventSection.AddToClassList("up-ult-section");
            _eventSection.AddToClassList("up-ult-event-section");
            scroll.Add(_eventSection);

            _validationSection = new VisualElement();
            _validationSection.AddToClassList("up-ult-section");
            _validationSection.AddToClassList("up-ult-validation-section");
            scroll.Add(_validationSection);

            _settingsSection = new VisualElement();
            _settingsSection.AddToClassList("up-ult-settings");
            scroll.Add(_settingsSection);

            _inspectorPane.Add(scroll);
            return _inspectorPane;
        }

        private void SaveInspectorWidth(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            if (width >= 340f && width <= 520f)
                EditorPrefs.SetFloat(InspectorWidthPrefs, width);
        }

        // ── 에셋 바인딩 ───────────────────────────────────
        private void BindAssetSafe(UltimateSequenceAsset asset)
        {
            _asset = asset;
            // CreateGUI가 아직 실행 전이면(트리 미구성) CreateGUI가 _asset으로 바인딩한다.
            if (rootVisualElement.childCount > 0)
                BindAsset(asset);
        }

        private void BindAsset(UltimateSequenceAsset asset)
        {
            _asset = asset;
            _serialized = asset != null ? new SerializedObject(asset) : null;

            if (_assetField != null && _assetField.value != asset)
                _assetField.SetValueWithoutNotify(asset);

            _objTracker.Unbind();
            if (_serialized != null)
                _objTracker.TrackSerializedObjectValue(_serialized, _ => OnAssetDataChanged());

            BuildSettingsInspector();
            _timeline?.ClearSelection();
            _timeline?.Rebuild();
            RefreshEventInspector();
            Validate();
            UpdateLinkStates();
        }

        private void BuildSettingsInspector()
        {
            _settingsSection.Clear();

            if (_serialized == null)
            {
                _settingsSection.Add(SectionTitle("시퀀스 설정"));
                _settingsSection.Add(Hint("에셋을 선택하거나 상단에서 캐릭터별 에셋을 생성하세요."));
                return;
            }

            AddSettingsGroup(
                "1단계 · 기본 재생",
                BasicSettingsPrefs,
                true,
                ("ownerType", "소유 캐릭터"),
                ("motionSet", "미리보기 MotionSet"),
                ("cameraProfile", "카메라 프로필"),
                ("motionFadeDuration", "모션 페이드"));
            AddSettingsGroup(
                "2단계 · 게임플레이 잠금",
                GameplaySettingsPrefs,
                false,
                ("timelineUseUnscaledTime", "언스케일드 타임라인"),
                ("lockSettings", "게임플레이 잠금"));
            AddSettingsGroup(
                "3단계 · 타겟 / 배치",
                TargetSettingsPrefs,
                false,
                ("targetPolicy", "타겟 정책"),
                ("placementSettings", "배치 설정"));
            AddSettingsGroup(
                "4단계 · 연출 스테이지",
                StageSettingsPrefs,
                false,
                ("cinematicStage", "연출 스테이지"),
                ("letterbox", "Letterbox UI"),
                ("uiSettings", "UI 표시"));
            _settingsSection.Bind(_serialized);
        }

        private void AddSettingsGroup(
            string title,
            string prefsKey,
            bool defaultExpanded,
            params (string path, string label)[] fields)
        {
            var foldout = new Foldout
            {
                text = title,
                value = EditorPrefs.GetBool(prefsKey, defaultExpanded),
            };
            foldout.AddToClassList("up-ult-settings-foldout");
            foldout.RegisterValueChangedCallback(e => EditorPrefs.SetBool(prefsKey, e.newValue));

            foreach ((string path, string label) in fields)
            {
                var field = new PropertyField { bindingPath = path, label = label };
                field.AddToClassList("up-ult-setting-field");
                foldout.Add(field);
            }

            _settingsSection.Add(foldout);
        }

        private void RefreshEventInspector()
        {
            _eventSection.Clear();

            IReadOnlyCollection<int> selection = _timeline != null ? _timeline.Selection : null;
            int count = selection?.Count ?? 0;

            if (_inspectorTitle != null)
            {
                _inspectorTitle.text = count == 0
                    ? "이벤트 인스펙터"
                    : count == 1
                        ? "선택 이벤트"
                        : $"선택 이벤트 · {count}개";
            }

            _eventSection.Add(SectionTitle(count <= 1 ? "선택 이벤트" : $"선택 이벤트 ({count})"));

            if (_serialized == null || count == 0)
            {
                _eventSection.Add(Hint("타임라인에서 이벤트를 선택하세요.\n빈 곳을 드래그하면 여러 개를 한 번에 선택할 수 있습니다."));
                return;
            }

            if (count == 1)
            {
                int index = selection.First();
                int eventCount = _asset.events?.Count ?? 0;
                if (index < 0 || index >= eventCount)
                {
                    _eventSection.Add(Hint("선택이 유효하지 않습니다."));
                    return;
                }

                UltimateTimelineEvent evt = _asset.events[index];

                var header = new VisualElement();
                header.AddToClassList("up-ult-event-header");
                var swatch = new VisualElement();
                swatch.AddToClassList("up-ult-event-swatch");
                swatch.AddToClassList(UltimateEventClipboard.ResolveUssClass(evt));
                header.Add(swatch);
                header.Add(new Label(evt != null ? evt.DisplayName : "이벤트")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold },
                });
                _eventSection.Add(header);

                SerializedProperty prop = _serialized.FindProperty("events").GetArrayElementAtIndex(index);
                var field = new PropertyField(prop);
                field.BindProperty(prop);
                _eventSection.Add(field);
            }
            else
            {
                _eventSection.Add(Hint($"{count}개 이벤트가 선택되었습니다. 타임라인에서 함께 이동하거나 아래 동작을 적용하세요."));
            }

            var buttons = new VisualElement();
            buttons.AddToClassList("up-ult-btn-row");
            buttons.Add(new Button(DuplicateSelected) { text = "복제" });
            buttons.Add(new Button(CopySelected) { text = "복사" });
            buttons.Add(new Button(DeleteSelected) { text = "삭제" });
            _eventSection.Add(buttons);
        }

        // ── 검증 ──────────────────────────────────────────
        private void Validate()
        {
            _validationSection.Clear();
            _validationSection.Add(SectionTitle("검증"));

            if (_asset == null)
            {
                SetPill("—", null);
                _validationSection.Add(Hint("에셋이 없습니다."));
                return;
            }

            var items = new List<(int severity, string message)>();
            if (!_asset.IsValid(out string error))
                items.Add((2, error));
            if (_asset.motionSet == null
                || _asset.motionSet.motionSet == null
                || !_asset.motionSet.motionSet.IsValid())
            {
                items.Add((1,
                    "미리보기 MotionSet이 없습니다. 인게임 Motion은 Ability Payload의 Motion Key로 실행되지만 에디터 테스트는 사용할 수 없습니다."));
            }
            if (_asset.cameraProfile == null)
                items.Add((1, "카메라 프로필이 없습니다. 카메라 없는 궁극기로 실행됩니다."));
            if (_asset.cinematicStage?.enabled == true
                && _asset.cinematicStage.stage == null)
            {
                items.Add((2, "연출 스테이지가 활성화됐지만 CinematicStageSO가 없습니다."));
            }

            float duration = ResolveMotionDuration();
            if (_asset.events != null)
            {
                foreach (UltimateTimelineEvent evt in _asset.events)
                {
                    if (evt != null && evt.EndTime > duration + 0.001f)
                        items.Add((1, $"'{evt.DisplayName}' 이벤트가 모션 길이({duration:0.###}s)를 초과합니다."));
                }
            }

            int worst = items.Count == 0 ? 0 : items.Max(i => i.severity);
            if (items.Count == 0)
                items.Add((0, "필수 데이터가 연결되어 있습니다."));

            foreach ((int severity, string message) in items)
                _validationSection.Add(ValidationRow(severity, message));

            if (worst == 2)
                SetPill("오류", "up-ult-pill--error");
            else if (worst == 1)
                SetPill($"경고 {items.Count(i => i.severity == 1)}", "up-ult-pill--warn");
            else
                SetPill("정상", "up-ult-pill--ok");
        }

        private void SetPill(string text, string modifier)
        {
            _pill.text = text;
            _pill.ClearClassList();
            _pill.AddToClassList("up-ult-pill");
            if (!string.IsNullOrEmpty(modifier))
                _pill.AddToClassList(modifier);
        }

        private static VisualElement ValidationRow(int severity, string message)
        {
            var row = new VisualElement();
            row.AddToClassList("up-ult-validation-row");

            var badge = new VisualElement();
            badge.AddToClassList("up-ult-validation-badge");
            badge.AddToClassList(severity == 2
                ? "up-ult-badge--error"
                : severity == 1
                    ? "up-ult-badge--warn"
                    : "up-ult-badge--ok");
            row.Add(badge);

            var text = new Label(message);
            text.AddToClassList("up-ult-validation-text");
            row.Add(text);
            return row;
        }

        // ── 구조 편집 ─────────────────────────────────────
        private void AddEvent(Type type)
        {
            AddEventAtTime(type, 0f);
        }

        private void AddEventAtTime(Type type, float startTime)
        {
            if (_serialized == null)
                return;

            _serialized.Update();
            SerializedProperty events = _serialized.FindProperty("events");
            int index = events.arraySize;
            events.InsertArrayElementAtIndex(index);
            var timelineEvent = (UltimateTimelineEvent)Activator.CreateInstance(type);
            timelineEvent.startTime = Mathf.Clamp(startTime, 0f, ResolveMotionDuration());
            events.GetArrayElementAtIndex(index).managedReferenceValue = timelineEvent;
            _serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_asset);

            _timeline.Rebuild();
            _timeline.SelectIndices(new[] { index });
            Validate();
        }

        private void DeleteSelected()
        {
            if (_serialized == null || _timeline.Selection.Count == 0)
                return;

            List<int> indices = _timeline.Selection.OrderByDescending(x => x).ToList();
            _serialized.Update();
            SerializedProperty events = _serialized.FindProperty("events");
            foreach (int index in indices)
            {
                if (index >= 0 && index < events.arraySize)
                    events.DeleteArrayElementAtIndex(index);
            }

            _serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_asset);

            _timeline.ClearSelection();
            _timeline.Rebuild();
            Validate();
        }

        private void DuplicateSelected()
        {
            if (_serialized == null || _timeline.Selection.Count == 0)
                return;

            var clones = new List<UltimateTimelineEvent>();
            foreach (int index in _timeline.Selection.OrderBy(x => x))
            {
                if (index >= 0 && index < (_asset.events?.Count ?? 0) && _asset.events[index] != null)
                    clones.Add(UltimateEventClipboard.Clone(_asset.events[index]));
            }

            AppendEvents(clones);
        }

        private void CopySelected()
        {
            if (_asset?.events == null)
                return;

            var events = new List<UltimateTimelineEvent>();
            foreach (int index in _timeline.Selection.OrderBy(x => x))
            {
                if (index >= 0 && index < _asset.events.Count && _asset.events[index] != null)
                    events.Add(_asset.events[index]);
            }

            UltimateEventClipboard.Copy(events);
            UpdateLinkStates();
        }

        private void PasteClipboard()
        {
            if (_serialized == null || !UltimateEventClipboard.HasContent)
                return;
            AppendEvents(UltimateEventClipboard.Paste());
        }

        private void AppendEvents(List<UltimateTimelineEvent> newEvents)
        {
            if (newEvents == null || newEvents.Count == 0)
                return;

            _serialized.Update();
            SerializedProperty events = _serialized.FindProperty("events");
            var newIndices = new List<int>();
            foreach (UltimateTimelineEvent evt in newEvents)
            {
                int index = events.arraySize;
                events.InsertArrayElementAtIndex(index);
                events.GetArrayElementAtIndex(index).managedReferenceValue = evt;
                newIndices.Add(index);
            }

            _serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_asset);

            _timeline.Rebuild();
            _timeline.SelectIndices(newIndices);
            Validate();
        }

        // ── 타임라인 콜백 ─────────────────────────────────
        private void OnTimelineDataChanged()
        {
            // 드래그로 직접 수정된 필드를 바인딩 인스펙터에 반영한다.
            _serialized?.Update();
            Validate();
        }

        private void OnAssetDataChanged()
        {
            // 인스펙터 편집으로 SerializedObject가 바뀌면 타임라인/검증을 갱신한다.
            // 드래그 중에는 레인 재배치가 튀지 않도록 건너뛴다.
            if (_timeline == null || _timeline.IsDragging)
                return;
            _timeline.RefreshLayout();
            Validate();
            UpdateLinkStates();
        }

        // ── PlayMode ──────────────────────────────────────
        private void PollPlayMode()
        {
            bool playing = Application.isPlaying;
            PlayerActor player = null;
            UltimateSequencePlayer sequencePlayer = null;

            if (playing)
            {
                player = GameObjectManager.Instance?.Player ?? FindFirstObjectByType<PlayerActor>();
                sequencePlayer = player != null ? player.GetComponent<UltimateSequencePlayer>() : null;
            }

            bool active = sequencePlayer != null
                          && sequencePlayer.IsPlaying
                          && sequencePlayer.ActiveAsset == _asset;

            float? cursor = null;
            string label = playing ? "대기" : "에디트 모드";
            if (active && sequencePlayer.RuntimeContext != null)
            {
                cursor = sequencePlayer.RuntimeContext.ElapsedTime;
                label = $"재생 {cursor:0.00}s";
            }

            _timeline?.SetPlayCursor(cursor);
            if (_timeLabel != null)
                _timeLabel.text = label;
            _playButton?.SetEnabled(playing && player != null && (sequencePlayer == null || !sequencePlayer.IsPlaying));
            _stopButton?.SetEnabled(active);
        }

        private void RunTest()
        {
            if (!Application.isPlaying || _asset == null)
                return;

            PlayerActor player = GameObjectManager.Instance?.Player ?? FindFirstObjectByType<PlayerActor>();
            if (player == null)
            {
                Debug.LogWarning("[궁극기 에디터] 씬에서 PlayerActor를 찾지 못했습니다.");
                return;
            }

            player.GetCombat()?.PreviewUltimate(_asset);
        }

        private void StopTest()
        {
            PlayerActor player = GameObjectManager.Instance?.Player ?? FindFirstObjectByType<PlayerActor>();
            player?.GetComponent<UltimateSequencePlayer>()?.Interrupt();
        }

        // ── 퀵 링크 / 생성 ────────────────────────────────
        private void OpenMotionEditor()
        {
            if (_asset?.motionSet != null)
                MotionSetEditorWindow.Open(_asset.motionSet);
        }

        private void OpenCameraEditor()
        {
            if (_asset?.cameraProfile != null)
                CameraSnapshotEditorWindow.Open(_asset.cameraProfile);
        }

        private void OpenCameraRecorder()
        {
            if (_asset?.motionSet != null)
                DialogueCameraRecorderWindow.OpenForMotion(_asset.motionSet);
        }

        private void UpdateLinkStates()
        {
            _motionLink?.SetEnabled(_asset?.motionSet != null);
            _cameraLink?.SetEnabled(_asset?.cameraProfile != null);
            _recordLink?.SetEnabled(_asset?.motionSet != null);
            _pasteButton?.SetEnabled(_serialized != null && UltimateEventClipboard.HasContent);
        }

        private void CreateAsset(CharacterActorType type)
        {
            if (type == CharacterActorType.None)
            {
                EditorUtility.DisplayDialog("생성 불가", "캐릭터 타입을 지정하세요.", "확인");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "궁극기 시퀀스 생성",
                $"UltimateSequence_{type}",
                "asset",
                "저장 위치를 선택하세요.",
                "Assets/10.Datas");
            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<UltimateSequenceAsset>();
            asset.ownerType = type;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            BindAsset(asset);
        }

        private float ResolveMotionDuration()
        {
            return _asset != null
                   && _asset.motionSet != null
                   && _asset.motionSet.motionSet != null
                ? _asset.motionSet.motionSet.TotalDuration
                : 0f;
        }

        // ── 소소한 헬퍼 ───────────────────────────────────
        private static void LoadStyle(VisualElement root, string path)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet != null)
                root.styleSheets.Add(sheet);
            else
                Debug.LogWarning($"[궁극기 에디터] 스타일을 찾을 수 없습니다: {path}");
        }

        private static Label SectionTitle(string text)
        {
            var label = new Label(text);
            label.AddToClassList("up-ult-section-title");
            return label;
        }

        private static Label Hint(string text)
        {
            var label = new Label(text);
            label.AddToClassList("up-ult-empty-hint");
            return label;
        }
    }

    [CustomEditor(typeof(UltimateSequenceAsset))]
    public class UltimateSequenceAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6f);
            if (GUILayout.Button("궁극기 시퀀스 에디터 열기", GUILayout.Height(26f)))
                UltimateSequenceEditorWindow.Open((UltimateSequenceAsset)target);
        }
    }

    internal static class UltimateEditorVisualElementExtensions
    {
        public static VisualElement WithClass(this VisualElement element, string className)
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
#endif
