using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor.UIToolkit.Timeline
{
    /// <summary>
    /// MotionSet 공용 UI Toolkit 타임라인.
    /// 구조/데이터 변경 시에만 전체를 갱신하고, 재생 중에는 커서 기하만 다시 그린다.
    /// </summary>
    internal sealed class TimelineView : VisualElement
    {
        const float LabelWidth = 210f;
        const float RulerHeight = 26f;
        const float GroupHeight = 26f;
        const float MotionHeight = 28f;
        const float EventHeight = 24f;
        const float RowGap = 1f;
        const float SectionGap = 5f;
        const float BasePixelsPerSecond = 80f;
        const float HandleHitWidth = 9f;
        const string LayerPrefsPrefix = "MotionSetWindow_TimelineLayer_";

        static readonly Color Background = new(0.055f, 0.075f, 0.10f);
        static readonly Color Ruler = new(0.045f, 0.06f, 0.08f);
        static readonly Color Track = new(0.09f, 0.115f, 0.145f);
        static readonly Color Header = new(0.075f, 0.10f, 0.13f);
        static readonly Color Divider = new(0.16f, 0.22f, 0.28f);
        static readonly Color RulerLine = new(0.34f, 0.48f, 0.60f);
        static readonly Color Cursor = new(1f, 0.32f, 0.34f);
        static readonly Color Marker = new(0.85f, 0.25f, 0.25f);
        static readonly Color Handle = new(1f, 0.85f, 0.2f);
        static readonly Color Selection = new(0.55f, 0.78f, 1f);
        static readonly Color[] MotionColors =
        {
            new(0.35f, 0.55f, 0.35f),
            new(0.55f, 0.65f, 0.30f),
            new(0.30f, 0.50f, 0.60f),
            new(0.50f, 0.40f, 0.55f),
        };

        readonly Func<MotionSet> _getSet;
        readonly Func<MotionSetDrawer> _getDrawer;
        readonly Func<UnityEngine.Object> _getUndoTarget;
        readonly Action _onChanged;
        readonly Action _onScrub;
        readonly TimelineTrackElement _track;
        readonly VisualElement _cursorLine;
        readonly Label _cursorLabel;
        readonly Slider _zoom;
        readonly Toggle _frames;
        readonly IntegerField _fps;
        readonly List<HitRegion> _hitRegions = new();
        readonly List<MotionTrackTarget> _motionTrackTargets = new();
        readonly Dictionary<LayerKind, LayerState> _layerStates = new();
        readonly Dictionary<LayerKind, LayerControlVisual> _layerControls = new();

        int _dataFingerprint;
        float _contentHeight = 200f;
        DragOperation _operation;
        HitRegion _activeHit;
        float _eventDuration;
        float _eventPointerOffset;
        int _undoGroup = -1;
        UnityEngine.Object _undoTarget;
        bool _operationChanged;

        enum HitKind
        {
            Event,
            Clip,
            ClipBody,
            Marker,
        }

        enum LayerKind
        {
            Motion,
            Timing,
            Event,
            Overlay,
        }

        enum DragOperation
        {
            None,
            Cursor,
            EventStart,
            EventEnd,
            EventBody,
            ClipStart,
            ClipEnd,
            Marker,
        }

        struct HitRegion
        {
            public HitKind kind;
            public LayerKind layer;
            public Rect rect;
            public Motion motion;
            public MotionEventBase motionEvent;
            public int motionIndex;
            public int eventIndex;
            public bool setEvent;
            public float motionOffset;
            // 클립이 속한 병렬 재생 레이어 인덱스. -1 = BASE(set.motions).
            public int playbackLayerIndex;
        }

        sealed class LayerState
        {
            public bool collapsed;
            public bool visible = true;
            public bool locked;
        }

        sealed class LayerControlVisual
        {
            public VisualElement root;
            public Button collapseButton;
            public Button visibilityButton;
            public Button lockButton;
            public string label;
        }

        sealed class MotionTrackTarget
        {
            public Rect rect;
            public List<Motion> motions;
            public MotionLayer layer;
            public bool isBase;
        }

        sealed class LayerSettingsPopup : PopupWindowContent
        {
            readonly MotionLayer _layer;
            readonly UnityEngine.Object _undoTarget;
            readonly Action _onChanged;

            public LayerSettingsPopup(
                MotionLayer layer,
                UnityEngine.Object undoTarget,
                Action onChanged)
            {
                _layer = layer;
                _undoTarget = undoTarget;
                _onChanged = onChanged;
            }

            public override Vector2 GetWindowSize() => new(330f, 205f);

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.LabelField("재생 레이어 설정", EditorStyles.boldLabel);
                EditorGUILayout.Space(3f);

                string layerName = EditorGUILayout.TextField("이름", _layer.layerName);
                int layerIndex = Mathf.Max(1, EditorGUILayout.IntField("Animancer 레이어", _layer.animancerLayerIndex));
                MotionLayerBlendMode blendMode = (MotionLayerBlendMode)EditorGUILayout.EnumPopup(
                    "블렌드 방식",
                    _layer.blendMode);
                AvatarMask avatarMask = (AvatarMask)EditorGUILayout.ObjectField(
                    "아바타 마스크",
                    _layer.avatarMask,
                    typeof(AvatarMask),
                    false);
                float weight = EditorGUILayout.Slider("가중치", _layer.weight, 0f, 1f);
                bool hold = EditorGUILayout.Toggle("마지막 프레임 유지", _layer.holdLastFrame);

                if (layerName == _layer.layerName &&
                    layerIndex == _layer.animancerLayerIndex &&
                    blendMode == _layer.blendMode &&
                    avatarMask == _layer.avatarMask &&
                    Mathf.Approximately(weight, _layer.weight) &&
                    hold == _layer.holdLastFrame)
                    return;

                if (_undoTarget != null)
                    Undo.RegisterCompleteObjectUndo(_undoTarget, "Edit Playback Layer");
                _layer.layerName = layerName;
                _layer.animancerLayerIndex = layerIndex;
                _layer.blendMode = blendMode;
                _layer.avatarMask = avatarMask;
                _layer.weight = weight;
                _layer.holdLastFrame = hold;
                if (_undoTarget != null)
                    EditorUtility.SetDirty(_undoTarget);
                _onChanged?.Invoke();
            }
        }

        public TimelineView(
            Func<MotionSet> getSet,
            Func<MotionSetDrawer> getDrawer,
            Func<UnityEngine.Object> getUndoTarget,
            Action onChanged,
            Action onScrub)
        {
            _getSet = getSet;
            _getDrawer = getDrawer;
            _getUndoTarget = getUndoTarget;
            _onChanged = onChanged;
            _onScrub = onScrub;
            InitializeLayerStates();

            AddToClassList("up-timeline-view");

            var toolbar = new Toolbar();
            toolbar.AddToClassList("up-timeline-toolbar");

            var titleBlock = new VisualElement();
            titleBlock.AddToClassList("up-timeline-title-block");
            var kicker = new Label("MOTION EVENT");
            kicker.AddToClassList("up-timeline-kicker");
            titleBlock.Add(kicker);
            var title = new Label("타임라인");
            title.AddToClassList("up-timeline-title");
            titleBlock.Add(title);
            toolbar.Add(titleBlock);

            var titleDivider = new VisualElement();
            titleDivider.AddToClassList("up-timeline-toolbar-divider");
            toolbar.Add(titleDivider);

            _zoom = new Slider("줌", 0.2f, 10f) { showInputField = true };
            _zoom.AddToClassList("up-timeline-zoom");
            _zoom.RegisterValueChangedCallback(evt =>
            {
                MotionSetDrawer drawer = _getDrawer?.Invoke();
                if (drawer == null)
                    return;
                drawer.zoom = evt.newValue;
                ClampScroll();
                RefreshData(true);
            });
            toolbar.Add(_zoom);

            var displayDivider = new VisualElement();
            displayDivider.AddToClassList("up-timeline-toolbar-divider");
            toolbar.Add(displayDivider);

            _frames = new Toggle("프레임");
            _frames.AddToClassList("up-timeline-frames");
            _frames.RegisterValueChangedCallback(evt =>
            {
                MotionSetDrawer drawer = _getDrawer?.Invoke();
                if (drawer == null)
                    return;
                drawer.showFrames = evt.newValue;
                _fps.EnableInClassList("up-hidden", !evt.newValue);
                RefreshData(true);
            });
            toolbar.Add(_frames);

            _fps = new IntegerField("FPS");
            _fps.AddToClassList("up-timeline-fps");
            _fps.RegisterValueChangedCallback(evt =>
            {
                MotionSetDrawer drawer = _getDrawer?.Invoke();
                if (drawer == null)
                    return;
                drawer.fps = Mathf.Clamp(evt.newValue, 1, 120);
                _fps.SetValueWithoutNotify(drawer.fps);
                RefreshData(true);
            });
            toolbar.Add(_fps);

            var spacer = new VisualElement();
            spacer.AddToClassList("up-flex-spacer");
            toolbar.Add(spacer);
            _cursorLabel = new Label("0.000s");
            _cursorLabel.AddToClassList("up-timeline-cursor-label");
            toolbar.Add(_cursorLabel);
            Add(toolbar);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("up-timeline-scroll");
            _track = new TimelineTrackElement(this);
            _track.AddManipulator(new TimelinePointerManipulator(this));
            _track.RegisterCallback<GeometryChangedEvent>(_ => RefreshData(true));
            _track.RegisterCallback<DragUpdatedEvent>(HandleAnimationDragUpdated);
            _track.RegisterCallback<DragPerformEvent>(HandleAnimationDragPerform);

            // 재생 커서를 경량 오버레이 엘리먼트로 분리한다. 재생 중에는 커서만 움직이므로,
            // 전체 타임라인 메시를 매 프레임 재생성(MarkDirtyRepaint)하지 않고 이 엘리먼트의
            // 위치만 갱신한다(UpdateCursorLine). RebuildLabels가 _track.Clear()로 자식을
            // 비우므로, 재구성 끝에서 다시 붙인다.
            _cursorLine = new VisualElement();
            _cursorLine.name = "motion-timeline-cursor";
            _cursorLine.pickingMode = PickingMode.Ignore;
            _cursorLine.style.position = Position.Absolute;
            _cursorLine.style.top = 0f;
            _cursorLine.style.width = 2f;
            _cursorLine.style.backgroundColor = Cursor;
            _cursorLine.style.display = DisplayStyle.None;

            scroll.Add(_track);
            Add(scroll);

            RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += HandleUndoRedo);
            RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= HandleUndoRedo);
            RefreshData(true);
        }

        void InitializeLayerStates()
        {
            foreach (LayerKind layer in Enum.GetValues(typeof(LayerKind)))
            {
                string prefix = LayerPrefsPrefix + layer;
                _layerStates[layer] = new LayerState
                {
                    collapsed = EditorPrefs.GetBool(prefix + "_Collapsed", false),
                    visible = EditorPrefs.GetBool(prefix + "_Visible", true),
                    locked = EditorPrefs.GetBool(prefix + "_Locked", false),
                };
            }
        }

        void AddLayerHeader(
            string label,
            LayerKind layer,
            float top,
            Color accent,
            Action addAction = null)
        {
            var root = new VisualElement();
            root.AddToClassList("up-timeline-layer-row");
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.top = top;
            root.style.width = LabelWidth;
            root.style.height = GroupHeight;
            root.style.borderLeftColor = accent;
            root.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());

            var handle = new Label("≡");
            handle.AddToClassList("up-timeline-layer-handle");
            root.Add(handle);

            var collapse = new Button(() => ToggleLayerCollapsed(layer));
            collapse.AddToClassList("up-timeline-layer-name");
            root.Add(collapse);

            var visibility = new Button(() => ToggleLayerVisible(layer));
            visibility.AddToClassList("up-timeline-layer-action");
            root.Add(visibility);

            var locked = new Button(() => ToggleLayerLocked(layer));
            locked.AddToClassList("up-timeline-layer-action");
            root.Add(locked);

            if (addAction != null)
            {
                var add = new Button(addAction) { text = "+" };
                add.tooltip = "새 병렬 재생 레이어 추가";
                add.AddToClassList("up-timeline-layer-action");
                add.AddToClassList("up-timeline-layer-add");
                root.Add(add);
            }

            _layerControls[layer] = new LayerControlVisual
            {
                root = root,
                collapseButton = collapse,
                visibilityButton = visibility,
                lockButton = locked,
                label = label,
            };
            UpdateLayerControl(layer);
            _track.Add(root);
        }

        void ToggleLayerCollapsed(LayerKind layer)
        {
            LayerState state = _layerStates[layer];
            state.collapsed = !state.collapsed;
            SaveLayerState(layer);
            UpdateLayerControl(layer);
            RefreshData(true);
        }

        void ToggleLayerVisible(LayerKind layer)
        {
            LayerState state = _layerStates[layer];
            state.visible = !state.visible;
            SaveLayerState(layer);
            UpdateLayerControl(layer);
            RefreshData(true);
        }

        void ToggleLayerLocked(LayerKind layer)
        {
            LayerState state = _layerStates[layer];
            state.locked = !state.locked;
            SaveLayerState(layer);
            UpdateLayerControl(layer);
            RefreshData(true);
        }

        void SaveLayerState(LayerKind layer)
        {
            LayerState state = _layerStates[layer];
            string prefix = LayerPrefsPrefix + layer;
            EditorPrefs.SetBool(prefix + "_Collapsed", state.collapsed);
            EditorPrefs.SetBool(prefix + "_Visible", state.visible);
            EditorPrefs.SetBool(prefix + "_Locked", state.locked);
        }

        void UpdateLayerControl(LayerKind layer)
        {
            LayerState state = _layerStates[layer];
            LayerControlVisual control = _layerControls[layer];
            control.collapseButton.text = $"{(state.collapsed ? "▶" : "▼")} {control.label}";
            control.collapseButton.tooltip = state.collapsed
                ? $"{control.label} 레이어 펼치기"
                : $"{control.label} 레이어 접기";
            control.visibilityButton.text = state.visible ? "●" : "○";
            control.visibilityButton.tooltip = state.visible
                ? $"{control.label} 레이어 숨기기"
                : $"{control.label} 레이어 표시";
            control.lockButton.text = state.locked ? "◆" : "◇";
            control.lockButton.tooltip = state.locked
                ? $"{control.label} 레이어 잠금 해제"
                : $"{control.label} 레이어 편집 잠금";
            control.root.EnableInClassList("up-layer-hidden-state", !state.visible);
            control.root.EnableInClassList("up-layer-locked-state", state.locked);
        }

        bool IsLayerCollapsed(LayerKind layer) => _layerStates[layer].collapsed;
        bool IsLayerVisible(LayerKind layer) => _layerStates[layer].visible;
        bool IsLayerLocked(LayerKind layer) => _layerStates[layer].locked;

        public void RefreshData(bool force = false)
        {
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer != null)
            {
                _zoom.SetValueWithoutNotify(drawer.zoom);
                _frames.SetValueWithoutNotify(drawer.showFrames);
                _fps.SetValueWithoutNotify(drawer.fps);
                _fps.EnableInClassList("up-hidden", !drawer.showFrames);
            }

            int fingerprint = CalculateFingerprint();
            if (!force && fingerprint == _dataFingerprint)
                return;

            _dataFingerprint = fingerprint;
            RebuildLabels();
            UpdateCursorLabel();
            _track.MarkDirtyRepaint();
        }

        public void RefreshIfChanged()
        {
            RefreshData(false);
        }

        public void RefreshPlayback()
        {
            // 재생/스크럽 중에는 커서만 이동한다. 전체 메시 재생성(MarkDirtyRepaint)은
            // 데이터/레이아웃이 바뀔 때만 필요하므로, 여기서는 커서 오버레이만 갱신한다.
            UpdateCursorLabel();
            UpdateCursorLine();
        }

        // 재생 커서 오버레이 엘리먼트의 위치/표시 상태를 현재 cursorTime 기준으로 갱신한다.
        void UpdateCursorLine()
        {
            if (_cursorLine == null)
                return;
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            float width = _track.contentRect.width;
            if (drawer == null || width <= LabelWidth)
            {
                _cursorLine.style.display = DisplayStyle.None;
                return;
            }
            float x = TimeToX(drawer.cursorTime, drawer, PixelsPerSecond(drawer));
            if (x < LabelWidth || x > width)
            {
                _cursorLine.style.display = DisplayStyle.None;
                return;
            }
            _cursorLine.style.display = DisplayStyle.Flex;
            _cursorLine.style.left = x - 1f; // 2px 폭의 중심을 커서 시간에 맞춘다
            _cursorLine.style.height = _contentHeight;
        }

        void HandleUndoRedo()
        {
            RefreshData(true);
            _onChanged?.Invoke();
        }

        void RebuildLabels()
        {
            _track.Clear();
            _layerControls.Clear();
            _motionTrackTargets.Clear();
            MotionSet set = _getSet?.Invoke();
            AddRulerLabels(set);
            AddLabel("TRACKS", 0f, RulerHeight, "up-timeline-corner-label");
            float y = RulerHeight;

            AddLayerHeader(
                "애니메이션 레이어",
                LayerKind.Motion,
                y,
                new Color(0.35f, 0.70f, 0.42f),
                AddPlaybackLayer);
            y += GroupHeight + RowGap;
            if (!IsLayerCollapsed(LayerKind.Motion))
            {
                bool hasRows = false;
                if (set != null)
                {
                    set.motions ??= new List<Motion>();
                    AddMotionTrackLabel("BASE", "Base Motion", null, set.motions, y, true);
                    AddMotionClipLabels(set.motions, y);
                    _motionTrackTargets.Add(new MotionTrackTarget
                    {
                        rect = new Rect(0f, y, float.MaxValue, MotionHeight),
                        motions = set.motions,
                        isBase = true,
                    });
                    y += MotionHeight + RowGap;
                    hasRows = true;
                }

                if (set?.layers != null)
                {
                    foreach (MotionLayer layer in set.layers)
                    {
                        if (layer == null)
                            continue;
                        layer.motions ??= new List<Motion>();
                        AddMotionTrackLabel(
                            $"L{Mathf.Max(1, layer.animancerLayerIndex)}",
                            layer.layerName,
                            layer,
                            layer.motions,
                            y,
                            false);
                        AddMotionClipLabels(layer.motions, y);
                        _motionTrackTargets.Add(new MotionTrackTarget
                        {
                            rect = new Rect(0f, y, float.MaxValue, MotionHeight),
                            motions = layer.motions,
                            layer = layer,
                        });
                        y += MotionHeight + RowGap;
                        hasRows = true;
                    }
                }

                if (!hasRows)
                {
                    AddLabel("(모션 없음)", y, MotionHeight, "up-timeline-track-label");
                    y += MotionHeight + RowGap;
                }
            }

            y += SectionGap;
            AddLayerHeader("타이밍", LayerKind.Timing, y, Marker);
            y += GroupHeight + RowGap;
            if (!IsLayerCollapsed(LayerKind.Timing))
            {
                AddLabel("전환점", y, EventHeight, "up-timeline-track-label");
                y += EventHeight + RowGap;
            }
            y += SectionGap;

            AddLayerHeader("이벤트", LayerKind.Event, y, new Color(0.40f, 0.55f, 0.90f));
            y += GroupHeight + RowGap;
            int eventRows = IsLayerCollapsed(LayerKind.Event) ? 0 : AddEventLabels(set, ref y);
            if (!IsLayerCollapsed(LayerKind.Event) && eventRows == 0)
            {
                AddLabel("(이벤트 없음)", y, EventHeight, "up-timeline-track-label");
                y += EventHeight + RowGap;
            }

            MotionSetDrawer drawer = _getDrawer?.Invoke();
            y += SectionGap;
            AddLayerHeader(drawer?.overlayGroupTitle ?? "오버레이", LayerKind.Overlay,
                y, new Color(0.95f, 0.55f, 0.25f));
            y += GroupHeight + RowGap;
            if (!IsLayerCollapsed(LayerKind.Overlay))
            {
                if (drawer?.overlayTracks != null && drawer.overlayTracks.Count > 0)
                {
                    foreach (MotionSetDrawer.OverlayTrack overlay in drawer.overlayTracks)
                    {
                        if (overlay == null)
                            continue;
                        AddLabel(overlay.label, y, EventHeight, "up-timeline-track-label");
                        y += EventHeight + RowGap;
                    }
                }
                else
                {
                    AddLabel("(비어 있음)", y, EventHeight, "up-timeline-track-label");
                    y += EventHeight + RowGap;
                }
            }

            _contentHeight = Mathf.Max(200f, y + 8f);
            _track.style.height = _contentHeight;

            // _track.Clear()로 제거됐으므로 커서 오버레이를 최상단(마지막 자식)으로 다시 붙인다.
            _track.Add(_cursorLine);
            UpdateCursorLine();
        }

        void AddRulerLabels(MotionSet set)
        {
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer == null)
                return;

            float pps = PixelsPerSecond(drawer);
            float step = GetRulerStep(pps);
            float start = Mathf.Floor((drawer.scrollX / pps) / step) * step;
            float width = Mathf.Max(LabelWidth, _track.contentRect.width);
            for (float time = start; time <= (set?.TotalDuration ?? 0f) + step; time += step)
            {
                float x = TimeToX(time, drawer, pps);
                if (x < LabelWidth || x > width)
                    continue;
                var label = new Label(drawer.showFrames
                    ? $"F{Mathf.RoundToInt(time * drawer.fps)}"
                    : $"{time:0.##}s");
                label.pickingMode = PickingMode.Ignore;
                label.AddToClassList("up-timeline-ruler-label");
                label.style.position = Position.Absolute;
                label.style.left = x + 2f;
                label.style.top = 0f;
                label.style.width = 52f;
                label.style.height = RulerHeight - 7f;
                _track.Add(label);
            }
        }

        int AddEventLabels(MotionSet set, ref float y)
        {
            int count = 0;
            if (set?.globalEvents != null)
            {
                foreach (MotionEventBase motionEvent in set.globalEvents)
                {
                    if (motionEvent == null)
                        continue;
                    MotionEventStyle.EventVisual visual = MotionEventStyle.Get(motionEvent);
                    AddLabel($"{visual.icon} {motionEvent.GetDisplayName()}", y, EventHeight, "up-timeline-track-label");
                    AddEventBarLabel(motionEvent, 0f, y);
                    y += EventHeight + RowGap;
                    count++;
                }
            }

            if (set?.motions == null)
                return count;
            float motionOffset = 0f;
            for (int mi = 0; mi < set.motions.Count; mi++)
            {
                Motion motion = set.motions[mi];
                if (motion?.events == null)
                {
                    motionOffset += motion?.Duration ?? 0f;
                    continue;
                }
                foreach (MotionEventBase motionEvent in motion.events)
                {
                    if (motionEvent == null)
                        continue;
                    MotionEventStyle.EventVisual visual = MotionEventStyle.Get(motionEvent);
                    string label = motionEvent.GetShortLabel();
                    AddLabel($"{visual.icon} {(string.IsNullOrEmpty(label) ? $"M{mi}" : label)}",
                        y, EventHeight, "up-timeline-track-label");
                    AddEventBarLabel(motionEvent, motionOffset, y);
                    y += EventHeight + RowGap;
                    count++;
                }
                motionOffset += motion.Duration;
            }
            return count;
        }

        void AddLabel(string text, float top, float height, string className)
        {
            var label = new Label(text);
            label.pickingMode = PickingMode.Ignore;
            label.AddToClassList(className);
            label.style.position = Position.Absolute;
            label.style.left = 0f;
            label.style.top = top;
            label.style.width = LabelWidth;
            label.style.height = height;
            _track.Add(label);
        }

        void AddMotionTrackLabel(
            string badgeText,
            string trackName,
            MotionLayer layer,
            List<Motion> motions,
            float top,
            bool isBase)
        {
            var root = new VisualElement();
            root.AddToClassList("up-motion-track-row");
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.top = top;
            root.style.width = LabelWidth;
            root.style.height = MotionHeight;
            root.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());

            if (isBase)
            {
                var baseIcon = new Label("●");
                baseIcon.tooltip = "Base 레이어는 항상 활성화됩니다.";
                baseIcon.AddToClassList("up-motion-track-toggle");
                root.Add(baseIcon);
            }
            else
            {
                var enabled = new Toggle { value = layer.enabled };
                enabled.tooltip = layer.enabled ? "레이어 비활성화" : "레이어 활성화";
                enabled.AddToClassList("up-motion-track-toggle");
                enabled.RegisterValueChangedCallback(evt =>
                    ApplyDataChange("Toggle Playback Layer", () => layer.enabled = evt.newValue));
                root.Add(enabled);
            }

            var badge = new Label(badgeText);
            badge.AddToClassList("up-motion-track-badge");
            root.Add(badge);

            var nameButton = new Button();
            nameButton.text = string.IsNullOrEmpty(trackName) ? "이름 없음" : trackName;
            nameButton.tooltip = isBase
                ? "Base 클립 시퀀스"
                : "클릭하여 레이어 이름·마스크·블렌드·가중치 설정";
            nameButton.AddToClassList("up-motion-track-name");
            if (!isBase)
            {
                nameButton.clicked += () =>
                    UnityEditor.PopupWindow.Show(
                        nameButton.worldBound,
                        new LayerSettingsPopup(
                            layer,
                            _getUndoTarget?.Invoke(),
                            () =>
                            {
                                RefreshData(true);
                                _onChanged?.Invoke();
                            }));
            }
            root.Add(nameButton);

            var count = new Label((motions?.Count ?? 0).ToString());
            count.tooltip = "클립 수";
            count.AddToClassList("up-motion-track-count");
            root.Add(count);

            var add = new Button(() => ShowAddClipMenu(motions)) { text = "+" };
            add.tooltip = "클립 추가 · Project에서 AnimationClip을 이 행으로 드래그할 수도 있습니다.";
            add.AddToClassList("up-motion-track-action");
            root.Add(add);

            if (!isBase)
            {
                var menu = new Button(() => ShowLayerMenu(layer)) { text = "⋮" };
                menu.tooltip = "레이어 메뉴";
                menu.AddToClassList("up-motion-track-action");
                root.Add(menu);
            }

            root.EnableInClassList("up-motion-track-disabled", layer != null && !layer.enabled);
            _track.Add(root);
        }

        void AddMotionClipLabels(List<Motion> motions, float top)
        {
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer == null || motions == null)
                return;

            if (motions.Count == 0)
            {
                var empty = new Label("AnimationClip을 이 트랙에 드롭");
                empty.pickingMode = PickingMode.Ignore;
                empty.AddToClassList("up-motion-track-drop-hint");
                empty.style.position = Position.Absolute;
                empty.style.left = LabelWidth + 9f;
                empty.style.top = top + 4f;
                empty.style.height = MotionHeight - 8f;
                _track.Add(empty);
                return;
            }

            float offset = 0f;
            float pps = PixelsPerSecond(drawer);
            float viewMax = Mathf.Max(LabelWidth, _track.contentRect.width);
            foreach (Motion motion in motions)
            {
                float duration = motion?.Duration ?? 0f;
                float x0 = Mathf.Max(LabelWidth + 5f, TimeToX(offset, drawer, pps) + 6f);
                float x1 = Mathf.Min(viewMax - 3f, TimeToX(offset + duration, drawer, pps) - 5f);
                if (x1 - x0 > 18f)
                {
                    var label = new Label(motion?.motionName ?? "(클립 미지정)");
                    label.pickingMode = PickingMode.Ignore;
                    label.tooltip = motion?.motionClip != null
                        ? $"{motion.motionClip.name} · {duration:0.###}s"
                        : "AnimationClip 미지정";
                    label.AddToClassList("up-motion-clip-label");
                    label.style.position = Position.Absolute;
                    label.style.left = x0;
                    label.style.top = top + 4f;
                    label.style.width = x1 - x0;
                    label.style.height = MotionHeight - 8f;
                    _track.Add(label);
                }
                offset += duration;
            }
        }

        void AddPlaybackLayer()
        {
            MotionSet set = _getSet?.Invoke();
            if (set == null)
                return;
            ApplyDataChange("Add Playback Layer", () =>
            {
                set.layers ??= new List<MotionLayer>();
                int nextIndex = 1;
                foreach (MotionLayer existing in set.layers)
                    if (existing != null)
                        nextIndex = Mathf.Max(nextIndex, existing.animancerLayerIndex + 1);
                set.layers.Add(new MotionLayer
                {
                    layerName = $"Animation Layer {nextIndex}",
                    animancerLayerIndex = nextIndex,
                });
            });
        }

        void ShowAddClipMenu(List<Motion> motions)
        {
            if (motions == null)
                return;
            var menu = new GenericMenu();
            if (UnityEditor.Selection.activeObject is AnimationClip selectedClip)
                menu.AddItem(new GUIContent($"Project 선택 클립 추가/{selectedClip.name}"), false,
                    () => AddClips(motions, motions.Count, new[] { selectedClip }));
            else
                menu.AddDisabledItem(new GUIContent("Project 선택 클립 추가/(AnimationClip을 먼저 선택)"));
            menu.AddItem(new GUIContent("빈 클립 슬롯 추가"), false,
                () => ApplyDataChange("Add Motion Slot", () =>
                    motions.Add(new Motion { motionName = $"Clip {motions.Count + 1}" })));
            menu.ShowAsContext();
        }

        void ShowLayerMenu(MotionLayer layer)
        {
            MotionSet set = _getSet?.Invoke();
            if (set?.layers == null || layer == null)
                return;
            int index = set.layers.IndexOf(layer);
            var menu = new GenericMenu();
            if (index > 0)
                menu.AddItem(new GUIContent("위로 이동"), false,
                    () => ApplyDataChange("Move Playback Layer", () =>
                    {
                        set.layers.RemoveAt(index);
                        set.layers.Insert(index - 1, layer);
                    }));
            else
                menu.AddDisabledItem(new GUIContent("위로 이동"));
            if (index >= 0 && index < set.layers.Count - 1)
                menu.AddItem(new GUIContent("아래로 이동"), false,
                    () => ApplyDataChange("Move Playback Layer", () =>
                    {
                        set.layers.RemoveAt(index);
                        set.layers.Insert(index + 1, layer);
                    }));
            else
                menu.AddDisabledItem(new GUIContent("아래로 이동"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("레이어 삭제"), false,
                () => ApplyDataChange("Remove Playback Layer", () => set.layers.Remove(layer)));
            menu.ShowAsContext();
        }

        void ApplyDataChange(string undoName, Action change)
        {
            UnityEngine.Object target = _getUndoTarget?.Invoke();
            if (target != null)
                Undo.RegisterCompleteObjectUndo(target, undoName);
            change?.Invoke();
            if (target != null)
                EditorUtility.SetDirty(target);
            RefreshData(true);
            _onChanged?.Invoke();
        }

        void HandleAnimationDragUpdated(DragUpdatedEvent evt)
        {
            MotionTrackTarget target = FindMotionTrackTarget(evt.localMousePosition);
            if (target == null || !HasDraggedAnimationClips())
                return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.StopPropagation();
        }

        void HandleAnimationDragPerform(DragPerformEvent evt)
        {
            MotionTrackTarget target = FindMotionTrackTarget(evt.localMousePosition);
            if (target == null)
                return;

            var clips = new List<AnimationClip>();
            foreach (UnityEngine.Object dragged in DragAndDrop.objectReferences)
                if (dragged is AnimationClip clip)
                    clips.Add(clip);
            if (clips.Count == 0)
                return;

            MotionSetDrawer drawer = _getDrawer?.Invoke();
            int insertIndex = GetMotionInsertIndex(
                target.motions,
                drawer != null ? Mathf.Max(0f, XToTime(evt.localMousePosition.x, drawer)) : float.MaxValue);
            DragAndDrop.AcceptDrag();
            AddClips(target.motions, insertIndex, clips);
            evt.StopPropagation();
        }

        MotionTrackTarget FindMotionTrackTarget(Vector2 position)
        {
            if (position.x < LabelWidth || IsLayerCollapsed(LayerKind.Motion) || IsLayerLocked(LayerKind.Motion))
                return null;
            foreach (MotionTrackTarget target in _motionTrackTargets)
                if (target.rect.Contains(position) &&
                    (target.isBase || target.layer == null || target.layer.enabled))
                    return target;
            return null;
        }

        static bool HasDraggedAnimationClips()
        {
            foreach (UnityEngine.Object dragged in DragAndDrop.objectReferences)
                if (dragged is AnimationClip)
                    return true;
            return false;
        }

        static int GetMotionInsertIndex(List<Motion> motions, float time)
        {
            if (motions == null)
                return 0;
            float offset = 0f;
            for (int i = 0; i < motions.Count; i++)
            {
                float duration = motions[i]?.Duration ?? 0f;
                if (time < offset + duration * 0.5f)
                    return i;
                offset += duration;
            }
            return motions.Count;
        }

        void AddClips(
            List<Motion> motions,
            int insertIndex,
            IReadOnlyList<AnimationClip> clips)
        {
            if (motions == null || clips == null || clips.Count == 0)
                return;
            ApplyDataChange("Add Animation Clips", () =>
            {
                int index = Mathf.Clamp(insertIndex, 0, motions.Count);
                foreach (AnimationClip clip in clips)
                {
                    if (clip == null)
                        continue;
                    motions.Insert(index++, new Motion
                    {
                        motionName = clip.name,
                        motionClip = clip,
                    });
                }
            });
        }

        void AddEventBarLabel(MotionEventBase motionEvent, float offset, float top)
        {
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer == null || motionEvent == null)
                return;

            float pps = PixelsPerSecond(drawer);
            float x0 = TimeToX(offset + motionEvent.startTime, drawer, pps);
            float x1 = TimeToX(offset + motionEvent.endTime, drawer, pps);
            float viewMax = Mathf.Max(LabelWidth, _track.contentRect.width);
            float left = Mathf.Max(LabelWidth + 5f, x0 + 5f);
            float right = Mathf.Min(viewMax - 3f, x1 - 3f);
            if (right - left < 12f)
                return;

            string shortLabel = motionEvent.GetShortLabel();
            if (string.IsNullOrEmpty(shortLabel))
                shortLabel = motionEvent.GetDisplayName();
            string start = FormatTimelineTime(motionEvent.startTime, drawer);
            string end = FormatTimelineTime(motionEvent.endTime, drawer);

            var label = new Label($"{shortLabel}  {start}–{end}")
            {
                tooltip = $"{motionEvent.GetDisplayName()}\nStart {start} / End {end}",
                pickingMode = PickingMode.Ignore,
            };
            label.AddToClassList("up-timeline-event-bar-label");
            label.style.position = Position.Absolute;
            label.style.left = left;
            label.style.top = top + 2f;
            label.style.width = right - left;
            label.style.height = EventHeight - 4f;
            _track.Add(label);
        }

        static string FormatTimelineTime(float value, MotionSetDrawer drawer)
        {
            return drawer.showFrames
                ? $"F{Mathf.RoundToInt(value * drawer.fps)}"
                : $"{value:0.###}s";
        }

        internal void GenerateTimeline(MeshGenerationContext context)
        {
            MotionSet set = _getSet?.Invoke();
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            float width = _track.contentRect.width;
            if (width <= LabelWidth || drawer == null)
                return;

            Painter2D painter = context.painter2D;
            DrawRect(painter, new Rect(0, 0, width, _contentHeight), Background);
            DrawRect(painter, new Rect(0, 0, width, RulerHeight), Ruler);
            DrawLine(painter, new Vector2(LabelWidth, 0), new Vector2(LabelWidth, _contentHeight), Divider);

            float trackWidth = width - LabelWidth;
            float pixelsPerSecond = PixelsPerSecond(drawer);
            DrawRuler(painter, set?.TotalDuration ?? 0f, pixelsPerSecond, trackWidth, drawer);

            _hitRegions.Clear();
            float y = RulerHeight;
            DrawGroup(painter, y, width, new Color(0.35f, 0.70f, 0.42f));
            y += GroupHeight + RowGap;
            float offset = 0f;
            if (!IsLayerCollapsed(LayerKind.Motion))
            {
                bool hasRows = false;
                if (set?.motions != null)
                {
                    for (int i = 0; i < set.motions.Count; i++)
                    {
                        Motion motion = set.motions[i];
                        DrawMotionRow(painter, motion, i, offset, y, trackWidth, pixelsPerSecond, drawer,
                            IsLayerVisible(LayerKind.Motion));
                        offset += motion?.Duration ?? 0f;
                    }
                    y += MotionHeight + RowGap;
                    hasRows = true;
                }

                if (set?.layers != null)
                {
                    for (int i = 0; i < set.layers.Count; i++)
                    {
                        DrawPlaybackLayerRow(
                            painter,
                            set.layers[i],
                            i,
                            y,
                            trackWidth,
                            pixelsPerSecond,
                            drawer,
                            IsLayerVisible(LayerKind.Motion));
                        if (set.layers[i] != null)
                        {
                            y += MotionHeight + RowGap;
                            hasRows = true;
                        }
                    }
                }

                if (!hasRows)
                {
                    DrawTrackBackground(painter, y, MotionHeight, trackWidth);
                    y += MotionHeight + RowGap;
                }
            }

            y += SectionGap;
            DrawGroup(painter, y, width, Marker);
            y += GroupHeight + RowGap;
            if (!IsLayerCollapsed(LayerKind.Timing))
            {
                DrawTimingRow(painter, set, y, trackWidth, pixelsPerSecond, drawer,
                    IsLayerVisible(LayerKind.Timing));
                y += EventHeight + RowGap;
            }
            y += SectionGap;

            DrawGroup(painter, y, width, new Color(0.40f, 0.55f, 0.90f));
            y += GroupHeight + RowGap;
            int rows = IsLayerCollapsed(LayerKind.Event)
                ? 0
                : DrawEventRows(painter, set, y, trackWidth, pixelsPerSecond, drawer,
                    IsLayerVisible(LayerKind.Event));
            if (!IsLayerCollapsed(LayerKind.Event) && rows == 0)
            {
                DrawTrackBackground(painter, y, EventHeight, trackWidth);
                y += EventHeight + RowGap;
            }
            else
            {
                y += rows * (EventHeight + RowGap);
            }

            y += SectionGap;
            DrawGroup(painter, y, width, new Color(0.95f, 0.55f, 0.25f));
            y += GroupHeight + RowGap;
            if (!IsLayerCollapsed(LayerKind.Overlay))
            {
                if (drawer.overlayTracks != null && drawer.overlayTracks.Count > 0)
                {
                    foreach (MotionSetDrawer.OverlayTrack overlay in drawer.overlayTracks)
                    {
                        if (overlay == null)
                            continue;
                        DrawOverlayRow(painter, overlay, y, trackWidth, pixelsPerSecond, drawer,
                            IsLayerVisible(LayerKind.Overlay));
                        y += EventHeight + RowGap;
                    }
                }
                else
                {
                    DrawTrackBackground(painter, y, EventHeight, trackWidth);
                    y += EventHeight + RowGap;
                }
            }

            DrawPlayRange(painter, set?.TotalDuration ?? 0f, drawer, pixelsPerSecond, width);
            // 재생 커서는 painter가 아니라 별도 오버레이 엘리먼트(_cursorLine)로 그린다.
            // 재생/스크럽 시 전체 메시 재생성을 피하기 위함. UpdateCursorLine() 참조.
        }

        void DrawRuler(Painter2D painter, float duration, float pps, float trackWidth, MotionSetDrawer drawer)
        {
            float step = GetRulerStep(pps);
            float start = Mathf.Floor((drawer.scrollX / pps) / step) * step;
            for (float time = start; time <= duration + step; time += step)
            {
                float x = TimeToX(time, drawer, pps);
                if (x < LabelWidth || x > LabelWidth + trackWidth)
                    continue;
                DrawLine(painter, new Vector2(x, RulerHeight - 9f), new Vector2(x, RulerHeight), RulerLine);
                float subStep = step / 5f;
                for (int i = 1; i < 5; i++)
                {
                    float sx = TimeToX(time + subStep * i, drawer, pps);
                    if (sx >= LabelWidth && sx <= LabelWidth + trackWidth)
                        DrawLine(painter, new Vector2(sx, RulerHeight - 4f), new Vector2(sx, RulerHeight), RulerLine * 0.65f);
                }
            }
        }

        void DrawMotionRow(
            Painter2D painter,
            Motion motion,
            int index,
            float offset,
            float y,
            float trackWidth,
            float pps,
            MotionSetDrawer drawer,
            bool visible)
        {
            DrawTrackBackground(painter, y, MotionHeight, trackWidth);
            if (!visible || motion == null)
                return;

            float x = TimeToX(offset, drawer, pps);
            float endX = TimeToX(offset + motion.Duration, drawer, pps);
            Rect bar = ClipToTrack(new Rect(x, y + 3f, Mathf.Max(4f, endX - x), MotionHeight - 6f), trackWidth);
            if (bar.width <= 0f)
                return;

            DrawRect(painter, bar, MotionColors[index % MotionColors.Length]);
            if (drawer.selectedLayerIndex < 0 && drawer.selectedMotionIndex == index)
                DrawOutline(painter, new Rect(LabelWidth, y, trackWidth, MotionHeight), Selection, 2f);
            if (motion.motionClip != null)
            {
                DrawRect(painter, new Rect(bar.x, bar.y, Mathf.Min(5f, bar.width), bar.height), Handle);
                DrawRect(painter, new Rect(Mathf.Max(bar.x, bar.xMax - 5f), bar.y, Mathf.Min(5f, bar.width), bar.height), Handle);
            }

            // 클립 본문 선택 영역(핸들보다 먼저 추가 → 히트 테스트에서 핸들이 우선).
            AddClipHitRegions(motion, index, -1, offset, x, endX, y);
        }

        // 클립 본문(선택)과 시작/끝 핸들(드래그) 히트 영역을 등록한다.
        // BASE와 병렬 재생 레이어가 동일한 상호작용을 갖도록 공유한다.
        void AddClipHitRegions(
            Motion motion, int motionIndex, int playbackLayerIndex,
            float offset, float x, float endX, float y)
        {
            // 본문: 클립 전체 폭. 클릭 시 모션 선택(드래그 없음).
            float bodyLeft = Mathf.Max(LabelWidth, x);
            float bodyRight = Mathf.Min(LabelWidth + Mathf.Max(0f, _track.contentRect.width - LabelWidth), endX);
            if (bodyRight > bodyLeft)
            {
                _hitRegions.Add(new HitRegion
                {
                    kind = HitKind.ClipBody,
                    layer = LayerKind.Motion,
                    rect = new Rect(bodyLeft, y, bodyRight - bodyLeft, MotionHeight),
                    motion = motion,
                    motionIndex = motionIndex,
                    motionOffset = offset,
                    playbackLayerIndex = playbackLayerIndex,
                });
            }

            // 시작/끝 핸들: 클립 길이(clipStart/End) 드래그. 클립이 지정된 경우만.
            if (motion.motionClip == null)
                return;
            _hitRegions.Add(new HitRegion
            {
                kind = HitKind.Clip,
                layer = LayerKind.Motion,
                rect = new Rect(x - HandleHitWidth, y, HandleHitWidth * 2f, MotionHeight),
                motion = motion,
                motionIndex = motionIndex,
                motionOffset = offset,
                playbackLayerIndex = playbackLayerIndex,
            });
            _hitRegions.Add(new HitRegion
            {
                kind = HitKind.Clip,
                layer = LayerKind.Motion,
                rect = new Rect(endX - HandleHitWidth, y, HandleHitWidth * 2f, MotionHeight),
                motion = motion,
                motionIndex = motionIndex,
                motionOffset = offset,
                eventIndex = 1,
                playbackLayerIndex = playbackLayerIndex,
            });
        }

        void DrawPlaybackLayerRow(
            Painter2D painter,
            MotionLayer layer,
            int layerOrder,
            float y,
            float trackWidth,
            float pps,
            MotionSetDrawer drawer,
            bool visible)
        {
            DrawTrackBackground(painter, y, MotionHeight, trackWidth);
            if (!visible || layer == null || !layer.enabled || layer.motions == null)
                return;

            bool selectedRow = drawer.selectedLayerIndex == layerOrder;
            float offset = 0f;
            for (int motionIndex = 0; motionIndex < layer.motions.Count; motionIndex++)
            {
                Motion motion = layer.motions[motionIndex];
                if (motion == null)
                    continue;

                float x = TimeToX(offset, drawer, pps);
                float endX = TimeToX(offset + motion.Duration, drawer, pps);
                Rect bar = ClipToTrack(
                    new Rect(x, y + 3f, Mathf.Max(4f, endX - x), MotionHeight - 6f),
                    trackWidth);
                if (bar.width > 0f)
                {
                    Color color = MotionColors[(layerOrder + motionIndex + 1) % MotionColors.Length];
                    DrawRect(painter, bar, new Color(color.r, color.g, color.b, 0.82f));
                    if (motion.motionClip != null)
                    {
                        DrawRect(painter, new Rect(bar.x, bar.y, Mathf.Min(5f, bar.width), bar.height), Handle);
                        DrawRect(painter, new Rect(Mathf.Max(bar.x, bar.xMax - 5f), bar.y, Mathf.Min(5f, bar.width), bar.height), Handle);
                    }
                    // BASE와 동일: 본문=선택, 시작/끝=드래그. playbackLayerIndex로 대상 레이어를 구분.
                    AddClipHitRegions(motion, motionIndex, layerOrder, offset, x, endX, y);
                }

                if (selectedRow && drawer.selectedMotionIndex == motionIndex && bar.width > 0f)
                    DrawOutline(painter, new Rect(LabelWidth, y, trackWidth, MotionHeight), Selection, 2f);

                offset += motion.Duration;
            }
        }

        void DrawTimingRow(
            Painter2D painter,
            MotionSet set,
            float y,
            float trackWidth,
            float pps,
            MotionSetDrawer drawer,
            bool visible)
        {
            DrawTrackBackground(painter, y, EventHeight, trackWidth);
            if (!visible || set?.motions == null)
                return;

            float offset = 0f;
            for (int i = 0; i < set.motions.Count - 1; i++)
            {
                Motion motion = set.motions[i];
                offset += motion?.Duration ?? 0f;
                float x = TimeToX(offset, drawer, pps);
                if (x < LabelWidth - 10f || x > LabelWidth + trackWidth + 10f)
                    continue;
                var diamond = new Rect(x - 7f, y + 4f, 14f, EventHeight - 8f);
                DrawDiamond(painter, diamond, Marker);
                _hitRegions.Add(new HitRegion
                {
                    kind = HitKind.Marker,
                    layer = LayerKind.Timing,
                    rect = new Rect(x - HandleHitWidth, y, HandleHitWidth * 2f, EventHeight),
                    motion = motion,
                    motionIndex = i,
                    motionOffset = offset - (motion?.Duration ?? 0f),
                });
            }
        }

        int DrawEventRows(
            Painter2D painter,
            MotionSet set,
            float startY,
            float trackWidth,
            float pps,
            MotionSetDrawer drawer,
            bool visible)
        {
            int row = 0;
            if (set?.globalEvents != null)
            {
                for (int i = 0; i < set.globalEvents.Count; i++)
                {
                    MotionEventBase motionEvent = set.globalEvents[i];
                    if (motionEvent == null)
                        continue;
                    DrawEventRow(painter, motionEvent, -1, i, true, 0f,
                        startY + row * (EventHeight + RowGap), trackWidth, pps, drawer, visible);
                    row++;
                }
            }

            if (set?.motions == null)
                return row;
            float offset = 0f;
            for (int mi = 0; mi < set.motions.Count; mi++)
            {
                Motion motion = set.motions[mi];
                if (motion?.events != null)
                {
                    for (int ei = 0; ei < motion.events.Count; ei++)
                    {
                        MotionEventBase motionEvent = motion.events[ei];
                        if (motionEvent == null)
                            continue;
                        DrawEventRow(painter, motionEvent, mi, ei, false, offset,
                            startY + row * (EventHeight + RowGap), trackWidth, pps, drawer, visible);
                        row++;
                    }
                }
                offset += motion?.Duration ?? 0f;
            }
            return row;
        }

        void DrawEventRow(
            Painter2D painter,
            MotionEventBase motionEvent,
            int motionIndex,
            int eventIndex,
            bool setEvent,
            float offset,
            float y,
            float trackWidth,
            float pps,
            MotionSetDrawer drawer,
            bool visible)
        {
            DrawTrackBackground(painter, y, EventHeight, trackWidth);
            if (!visible)
                return;
            float x0 = TimeToX(offset + motionEvent.startTime, drawer, pps);
            float x1 = TimeToX(offset + motionEvent.endTime, drawer, pps);
            Rect original = new(x0, y + 3f, Mathf.Max(4f, x1 - x0), EventHeight - 6f);
            Rect bar = ClipToTrack(original, trackWidth);
            MotionEventStyle.EventVisual visual = MotionEventStyle.Get(motionEvent);
            if (bar.width > 0f)
            {
                DrawRect(painter, bar, visual.dimmed);
                DrawRect(painter, new Rect(bar.x, bar.y, bar.width, 2f), visual.color);
                bool selected = setEvent
                    ? drawer.selectedEventIsSetEvent && drawer.selectedEventIndex == eventIndex
                    : !drawer.selectedEventIsSetEvent &&
                      drawer.selectedEventMotionIndex == motionIndex &&
                      drawer.selectedEventIndex == eventIndex;
                if (selected)
                    DrawOutline(painter, bar, Color.white, 1.5f);
                DrawDiamond(painter, new Rect(x0 - 4f, y + EventHeight * 0.5f - 4f, 8f, 8f), visual.color);
                DrawDiamond(painter, new Rect(x1 - 4f, y + EventHeight * 0.5f - 4f, 8f, 8f), visual.color);
            }

            _hitRegions.Add(new HitRegion
            {
                kind = HitKind.Event,
                layer = LayerKind.Event,
                rect = new Rect(x0 - HandleHitWidth, y, Mathf.Max(HandleHitWidth * 2f, x1 - x0 + HandleHitWidth * 2f), EventHeight),
                motionEvent = motionEvent,
                motionIndex = motionIndex,
                eventIndex = eventIndex,
                setEvent = setEvent,
                motionOffset = offset,
            });
        }

        void DrawOverlayRow(
            Painter2D painter,
            MotionSetDrawer.OverlayTrack overlay,
            float y,
            float trackWidth,
            float pps,
            MotionSetDrawer drawer,
            bool visible)
        {
            DrawTrackBackground(painter, y, EventHeight, trackWidth);
            if (!visible)
                return;
            foreach (MotionSetDrawer.OverlaySpan span in overlay.spans)
            {
                if (span == null)
                    continue;
                float x0 = TimeToX(span.start, drawer, pps);
                float x1 = TimeToX(span.end, drawer, pps);
                Rect bar = ClipToTrack(new Rect(x0, y + 3f, Mathf.Max(3f, x1 - x0), EventHeight - 6f), trackWidth);
                if (bar.width <= 0f)
                    continue;
                Color fill = new(overlay.color.r, overlay.color.g, overlay.color.b, span.dashed ? 0.17f : 0.38f);
                DrawRect(painter, bar, fill);
                if (span.dashed)
                {
                    for (float x = bar.x; x < bar.xMax; x += 7f)
                        DrawLine(painter, new Vector2(x, bar.y), new Vector2(Mathf.Min(x + 3f, bar.xMax), bar.y), overlay.color);
                }
                else
                {
                    DrawOutline(painter, bar, overlay.color, 1f);
                }
            }
        }

        void DrawGroup(Painter2D painter, float y, float width, Color accent)
        {
            DrawRect(painter, new Rect(0, y, width, GroupHeight), Header);
            DrawRect(painter, new Rect(0, y, 3f, GroupHeight), accent);
            DrawLine(painter, new Vector2(0, y), new Vector2(width, y), Divider);
            DrawLine(painter, new Vector2(0, y + GroupHeight), new Vector2(width, y + GroupHeight), Divider);
        }

        void DrawTrackBackground(Painter2D painter, float y, float height, float trackWidth)
        {
            DrawRect(painter, new Rect(LabelWidth, y, trackWidth, height), Track);
            DrawLine(painter, new Vector2(0, y + height), new Vector2(LabelWidth + trackWidth, y + height), Divider * 0.65f);
        }

        void DrawPlayRange(Painter2D painter, float duration, MotionSetDrawer drawer, float pps, float width)
        {
            float end = drawer.playRangeEnd > 0f ? drawer.playRangeEnd : duration;
            if (Mathf.Approximately(drawer.playRangeStart, 0f) && Mathf.Approximately(end, duration))
                return;
            float x0 = Mathf.Clamp(TimeToX(drawer.playRangeStart, drawer, pps), LabelWidth, width);
            float x1 = Mathf.Clamp(TimeToX(end, drawer, pps), LabelWidth, width);
            DrawRect(painter, new Rect(x0, RulerHeight, Mathf.Max(0f, x1 - x0), _contentHeight - RulerHeight),
                new Color(0.3f, 1f, 0.3f, 0.10f));
            DrawLine(painter, new Vector2(x0, RulerHeight), new Vector2(x0, _contentHeight), new Color(0.3f, 1f, 0.3f, 0.5f), 2f);
            DrawLine(painter, new Vector2(x1, RulerHeight), new Vector2(x1, _contentHeight), new Color(0.3f, 1f, 0.3f, 0.5f), 2f);
        }


        internal bool BeginPointerOperation(Vector2 position, int button, bool shift)
        {
            if (button != 0)
                return false;
            MotionSet set = _getSet?.Invoke();
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (set == null || drawer == null)
                return false;

            for (int i = _hitRegions.Count - 1; i >= 0; i--)
            {
                HitRegion hit = _hitRegions[i];
                if (!hit.rect.Contains(position))
                    continue;

                _activeHit = hit;
                if (hit.kind == HitKind.Event)
                {
                    drawer.SelectEvent(hit.motionIndex, hit.eventIndex, hit.setEvent);
                    if (IsLayerLocked(hit.layer))
                    {
                        RefreshData(true);
                        _onChanged?.Invoke();
                        return false;
                    }
                    float x0 = TimeToX(hit.motionOffset + hit.motionEvent.startTime, drawer, PixelsPerSecond(drawer));
                    float x1 = TimeToX(hit.motionOffset + hit.motionEvent.endTime, drawer, PixelsPerSecond(drawer));
                    if (Mathf.Abs(position.x - x0) <= HandleHitWidth)
                    {
                        _operation = DragOperation.EventStart;
                        RecordUndo("Drag Event Start");
                    }
                    else if (Mathf.Abs(position.x - x1) <= HandleHitWidth)
                    {
                        _operation = DragOperation.EventEnd;
                        RecordUndo("Drag Event End");
                    }
                    else if (shift)
                    {
                        _operation = DragOperation.EventBody;
                        _eventDuration = hit.motionEvent.endTime - hit.motionEvent.startTime;
                        _eventPointerOffset = XToTime(position.x, drawer) - hit.motionOffset - hit.motionEvent.startTime;
                        RecordUndo("Move Event");
                    }
                    else
                    {
                        RefreshData(true);
                        _onChanged?.Invoke();
                        return false;
                    }
                    return true;
                }

                // 클립 본문 클릭 = 모션 선택(BASE/레이어 공용). 드래그는 시작하지 않는다.
                // 잠금 레이어에서도 선택(뷰)은 허용한다 — 이벤트 선택과 동일한 방침.
                if (hit.kind == HitKind.ClipBody)
                {
                    drawer.SelectClipMotion(hit.playbackLayerIndex, hit.motionIndex);
                    RefreshData(true);
                    _onChanged?.Invoke();
                    return false;
                }

                if (IsLayerLocked(hit.layer))
                    return false;

                if (hit.kind == HitKind.Marker)
                {
                    _operation = DragOperation.Marker;
                    RecordUndo("Drag Timing Marker");
                    return true;
                }

                // 클립 시작/끝 핸들 드래그. 잡는 즉시 해당 모션을 선택해 인스펙터를 연동한다.
                drawer.SelectClipMotion(hit.playbackLayerIndex, hit.motionIndex);
                _operation = hit.eventIndex == 0 ? DragOperation.ClipStart : DragOperation.ClipEnd;
                RecordUndo(_operation == DragOperation.ClipStart ? "Drag Clip Start" : "Drag Clip End");
                _onChanged?.Invoke();
                return true;
            }

            if (position.y <= RulerHeight && position.x >= LabelWidth)
            {
                _operation = DragOperation.Cursor;
                UpdateCursor(position.x);
                return true;
            }
            return false;
        }

        internal bool UpdatePointerOperation(Vector2 position)
        {
            if (_operation == DragOperation.None)
                return false;
            if (_operation == DragOperation.Cursor)
            {
                UpdateCursor(position.x);
                return true;
            }

            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer == null)
                return false;
            float time = XToTime(position.x, drawer);
            float localTime = SnapTime(Mathf.Max(0f, time - _activeHit.motionOffset), drawer);

            switch (_operation)
            {
                case DragOperation.EventStart:
                    _activeHit.motionEvent.startTime = Mathf.Clamp(
                        localTime, 0f, _activeHit.motionEvent.endTime - 0.01f);
                    break;
                case DragOperation.EventEnd:
                    _activeHit.motionEvent.endTime = Mathf.Max(
                        _activeHit.motionEvent.startTime + 0.01f, localTime);
                    break;
                case DragOperation.EventBody:
                    float start = Mathf.Max(0f, SnapTime(localTime - _eventPointerOffset, drawer));
                    _activeHit.motionEvent.startTime = start;
                    _activeHit.motionEvent.endTime = start + _eventDuration;
                    break;
                case DragOperation.ClipStart:
                    UpdateClipStart(localTime);
                    break;
                case DragOperation.ClipEnd:
                case DragOperation.Marker:
                    UpdateClipEnd(localTime);
                    break;
            }

            MarkChanged();
            return true;
        }

        internal bool EndPointerOperation()
        {
            if (_operation == DragOperation.None)
                return false;
            CommitPointerOperation();
            _operation = DragOperation.None;
            RefreshData(true);
            return true;
        }

        internal void CancelPointerOperation()
        {
            if (_operation != DragOperation.None)
                CommitPointerOperation();
            _operation = DragOperation.None;
        }

        internal bool HandleWheel(float delta, bool zoom, bool horizontal, Vector2 position)
        {
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer == null)
                return false;
            if (zoom)
            {
                float before = XToTime(position.x, drawer);
                drawer.zoom = Mathf.Clamp(drawer.zoom - delta * 0.08f, 0.2f, 10f);
                drawer.scrollX = Mathf.Max(0f, before * PixelsPerSecond(drawer) - (position.x - LabelWidth));
                _zoom.SetValueWithoutNotify(drawer.zoom);
            }
            else
            {
                float multiplier = horizontal ? 45f : 24f;
                drawer.scrollX = Mathf.Max(0f, drawer.scrollX + delta * multiplier);
            }
            ClampScroll();
            RefreshData(true);
            return true;
        }

        void UpdateCursor(float x)
        {
            MotionSet set = _getSet?.Invoke();
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (set == null || drawer == null)
                return;
            drawer.cursorTime = Mathf.Clamp(SnapTime(XToTime(x, drawer), drawer), 0f, set.TotalDuration);
            drawer.cursorScrubRequested = true;
            UpdateCursorLabel();
            UpdateCursorLine();
            _onScrub?.Invoke();
        }

        void UpdateClipStart(float localTimelineTime)
        {
            Motion motion = _activeHit.motion;
            if (motion?.motionClip == null)
                return;
            float clipTime = localTimelineTime * Mathf.Max(0.0001f, motion.playbackSpeed) + motion.ClipStartTime;
            float value = Mathf.Clamp(clipTime, 0f, motion.ClipEndTime - 0.01f);
            motion.clipStartTime = Mathf.Approximately(value, 0f) ? -1f : value;
        }

        void UpdateClipEnd(float localTimelineTime)
        {
            Motion motion = _activeHit.motion;
            if (motion?.motionClip == null)
                return;
            float clipTime = localTimelineTime * Mathf.Max(0.0001f, motion.playbackSpeed) + motion.ClipStartTime;
            float value = Mathf.Clamp(clipTime, motion.ClipStartTime + 0.01f, motion.motionClip.length);
            motion.clipEndTime = Mathf.Approximately(value, motion.motionClip.length) ? -1f : value;
        }

        void MarkChanged()
        {
            UnityEngine.Object target = _getUndoTarget?.Invoke();
            if (target != null)
                EditorUtility.SetDirty(target);
            _operationChanged = true;
            RefreshData(true);
        }

        void RecordUndo(string label)
        {
            _undoTarget = _getUndoTarget?.Invoke();
            _undoGroup = -1;
            _operationChanged = false;
            if (_undoTarget == null)
                return;

            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(label);
            // SerializeReference 내부 객체를 직접 수정하므로 전체 객체 스냅샷을 등록해야 한다.
            Undo.RegisterCompleteObjectUndo(_undoTarget, label);
        }

        void CommitPointerOperation()
        {
            if (_undoTarget != null && _operationChanged)
                EditorUtility.SetDirty(_undoTarget);
            if (_undoGroup >= 0)
            {
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(_undoGroup);
            }
            if (_operationChanged)
                _onChanged?.Invoke();

            _undoGroup = -1;
            _undoTarget = null;
            _operationChanged = false;
        }

        void ClampScroll()
        {
            MotionSet set = _getSet?.Invoke();
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer == null)
                return;
            float viewWidth = Mathf.Max(0f, _track.contentRect.width - LabelWidth);
            float contentWidth = (set?.TotalDuration ?? 0f) * PixelsPerSecond(drawer);
            drawer.scrollX = Mathf.Clamp(drawer.scrollX, 0f, Mathf.Max(0f, contentWidth - viewWidth));
        }

        void UpdateCursorLabel()
        {
            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer == null)
            {
                _cursorLabel.text = "-";
                return;
            }
            _cursorLabel.text = drawer.showFrames
                ? $"F{Mathf.RoundToInt(drawer.cursorTime * drawer.fps)} · {drawer.cursorTime:0.000}s"
                : $"{drawer.cursorTime:0.000}s";
        }

        int CalculateFingerprint()
        {
            unchecked
            {
                int hash = 17;
                MotionSet set = _getSet?.Invoke();
                MotionSetDrawer drawer = _getDrawer?.Invoke();
                hash = hash * 31 + (set?.motions?.Count ?? 0);
                hash = hash * 31 + (set?.layers?.Count ?? 0);
                hash = hash * 31 + (set?.globalEvents?.Count ?? 0);
                if (set?.motions != null)
                {
                    foreach (Motion motion in set.motions)
                    {
                        hash = hash * 31 + (motion?.motionName?.GetHashCode() ?? 0);
                        hash = hash * 31 + (motion?.Duration.GetHashCode() ?? 0);
                        hash = hash * 31 + (motion?.events?.Count ?? 0);
                        if (motion?.events == null)
                            continue;
                        foreach (MotionEventBase motionEvent in motion.events)
                        {
                            hash = hash * 31 + (motionEvent?.startTime.GetHashCode() ?? 0);
                            hash = hash * 31 + (motionEvent?.endTime.GetHashCode() ?? 0);
                        }
                    }
                }
                if (set?.layers != null)
                {
                    foreach (MotionLayer layer in set.layers)
                    {
                        hash = hash * 31 + (layer?.layerName?.GetHashCode() ?? 0);
                        hash = hash * 31 + (layer?.enabled.GetHashCode() ?? 0);
                        hash = hash * 31 + (layer?.animancerLayerIndex.GetHashCode() ?? 0);
                        hash = hash * 31 + (layer?.weight.GetHashCode() ?? 0);
                        hash = hash * 31 + (layer?.motions?.Count ?? 0);
                        if (layer?.motions == null)
                            continue;
                        foreach (Motion motion in layer.motions)
                        {
                            hash = hash * 31 + (motion?.motionName?.GetHashCode() ?? 0);
                            hash = hash * 31 + (motion?.Duration.GetHashCode() ?? 0);
                        }
                    }
                }
                if (set?.globalEvents != null)
                {
                    foreach (MotionEventBase motionEvent in set.globalEvents)
                    {
                        hash = hash * 31 + (motionEvent?.startTime.GetHashCode() ?? 0);
                        hash = hash * 31 + (motionEvent?.endTime.GetHashCode() ?? 0);
                    }
                }
                if (drawer?.overlayTracks != null)
                {
                    hash = hash * 31 + drawer.overlayTracks.Count;
                    foreach (MotionSetDrawer.OverlayTrack track in drawer.overlayTracks)
                    {
                        hash = hash * 31 + (track?.spans?.Count ?? 0);
                        if (track?.spans == null)
                            continue;
                        foreach (MotionSetDrawer.OverlaySpan span in track.spans)
                        {
                            hash = hash * 31 + (span?.start.GetHashCode() ?? 0);
                            hash = hash * 31 + (span?.end.GetHashCode() ?? 0);
                        }
                    }
                }
                return hash;
            }
        }

        static float PixelsPerSecond(MotionSetDrawer drawer) =>
            BasePixelsPerSecond * Mathf.Clamp(drawer.zoom, 0.2f, 10f);

        static float TimeToX(float time, MotionSetDrawer drawer, float pps) =>
            LabelWidth + time * pps - drawer.scrollX;

        static float XToTime(float x, MotionSetDrawer drawer) =>
            (x - LabelWidth + drawer.scrollX) / PixelsPerSecond(drawer);

        static float SnapTime(float value, MotionSetDrawer drawer)
        {
            if (!drawer.showFrames)
                return value;
            float frame = 1f / Mathf.Max(1, drawer.fps);
            return Mathf.Round(value / frame) * frame;
        }

        static float GetRulerStep(float pps)
        {
            float[] steps = { 0.05f, 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f };
            foreach (float step in steps)
                if (step * pps >= 55f)
                    return step;
            return 10f;
        }

        static Rect ClipToTrack(Rect rect, float trackWidth)
        {
            float min = Mathf.Max(rect.xMin, LabelWidth);
            float max = Mathf.Min(rect.xMax, LabelWidth + trackWidth);
            return max <= min ? new Rect(min, rect.y, 0f, rect.height) : Rect.MinMaxRect(min, rect.yMin, max, rect.yMax);
        }

        static void DrawRect(Painter2D painter, Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }

        static void DrawLine(Painter2D painter, Vector2 from, Vector2 to, Color color, float width = 1f)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }

        static void DrawOutline(Painter2D painter, Rect rect, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Stroke();
        }

        static void DrawDiamond(Painter2D painter, Rect rect, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.center.x, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.center.y));
            painter.LineTo(new Vector2(rect.center.x, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.center.y));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
