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
        const float LabelWidth = 160f;
        const float RulerHeight = 26f;
        const float GroupHeight = 20f;
        const float MotionHeight = 26f;
        const float EventHeight = 22f;
        const float RowGap = 1f;
        const float SectionGap = 5f;
        const float BasePixelsPerSecond = 80f;
        const float HandleHitWidth = 9f;

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
        readonly Label _cursorLabel;
        readonly Slider _zoom;
        readonly Toggle _frames;
        readonly IntegerField _fps;
        readonly List<HitRegion> _hitRegions = new();

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
            Marker,
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
            public Rect rect;
            public Motion motion;
            public MotionEventBase motionEvent;
            public int motionIndex;
            public int eventIndex;
            public bool setEvent;
            public float motionOffset;
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
            scroll.Add(_track);
            Add(scroll);

            RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += HandleUndoRedo);
            RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= HandleUndoRedo);
            RefreshData(true);
        }

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
            UpdateCursorLabel();
            _track.MarkDirtyRepaint();
        }

        void HandleUndoRedo()
        {
            RefreshData(true);
            _onChanged?.Invoke();
        }

        void RebuildLabels()
        {
            _track.Clear();
            MotionSet set = _getSet?.Invoke();
            AddRulerLabels(set);
            float y = RulerHeight;

            AddLabel("몽타주", y, GroupHeight, "up-timeline-group-label");
            y += GroupHeight + RowGap;
            if (set?.motions != null && set.motions.Count > 0)
            {
                for (int i = 0; i < set.motions.Count; i++)
                {
                    Motion motion = set.motions[i];
                    AddLabel(motion?.motionName ?? $"Motion {i}", y, MotionHeight, "up-timeline-track-label");
                    y += MotionHeight + RowGap;
                }
            }
            else
            {
                AddLabel("(모션 없음)", y, MotionHeight, "up-timeline-track-label");
                y += MotionHeight + RowGap;
            }

            y += SectionGap;
            AddLabel("타이밍", y, GroupHeight, "up-timeline-group-label");
            y += GroupHeight + RowGap;
            AddLabel("전환점", y, EventHeight, "up-timeline-track-label");
            y += EventHeight + RowGap + SectionGap;

            AddLabel("노티파이", y, GroupHeight, "up-timeline-group-label");
            y += GroupHeight + RowGap;
            int eventRows = AddEventLabels(set, ref y);
            if (eventRows == 0)
            {
                AddLabel("(이벤트 없음)", y, EventHeight, "up-timeline-track-label");
                y += EventHeight + RowGap;
            }

            MotionSetDrawer drawer = _getDrawer?.Invoke();
            if (drawer?.overlayTracks != null && drawer.overlayTracks.Count > 0)
            {
                y += SectionGap;
                AddLabel(drawer.overlayGroupTitle, y, GroupHeight, "up-timeline-group-label");
                y += GroupHeight + RowGap;
                foreach (MotionSetDrawer.OverlayTrack overlay in drawer.overlayTracks)
                {
                    if (overlay == null)
                        continue;
                    AddLabel(overlay.label, y, EventHeight, "up-timeline-track-label");
                    y += EventHeight + RowGap;
                }
            }

            _contentHeight = Mathf.Max(200f, y + 8f);
            _track.style.height = _contentHeight;
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
            if (set?.motions != null && set.motions.Count > 0)
            {
                for (int i = 0; i < set.motions.Count; i++)
                {
                    Motion motion = set.motions[i];
                    DrawMotionRow(painter, motion, i, offset, y, trackWidth, pixelsPerSecond, drawer);
                    offset += motion?.Duration ?? 0f;
                    y += MotionHeight + RowGap;
                }
            }
            else
            {
                DrawTrackBackground(painter, y, MotionHeight, trackWidth);
                y += MotionHeight + RowGap;
            }

            y += SectionGap;
            DrawGroup(painter, y, width, Marker);
            y += GroupHeight + RowGap;
            DrawTimingRow(painter, set, y, trackWidth, pixelsPerSecond, drawer);
            y += EventHeight + RowGap + SectionGap;

            DrawGroup(painter, y, width, new Color(0.40f, 0.55f, 0.90f));
            y += GroupHeight + RowGap;
            int rows = DrawEventRows(painter, set, y, trackWidth, pixelsPerSecond, drawer);
            if (rows == 0)
            {
                DrawTrackBackground(painter, y, EventHeight, trackWidth);
                y += EventHeight + RowGap;
            }
            else
            {
                y += rows * (EventHeight + RowGap);
            }

            if (drawer.overlayTracks != null && drawer.overlayTracks.Count > 0)
            {
                y += SectionGap;
                DrawGroup(painter, y, width, new Color(0.95f, 0.55f, 0.25f));
                y += GroupHeight + RowGap;
                foreach (MotionSetDrawer.OverlayTrack overlay in drawer.overlayTracks)
                {
                    if (overlay == null)
                        continue;
                    DrawOverlayRow(painter, overlay, y, trackWidth, pixelsPerSecond, drawer);
                    y += EventHeight + RowGap;
                }
            }

            DrawPlayRange(painter, set?.TotalDuration ?? 0f, drawer, pixelsPerSecond, width);
            DrawCursor(painter, drawer, pixelsPerSecond, width);
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
            MotionSetDrawer drawer)
        {
            DrawTrackBackground(painter, y, MotionHeight, trackWidth);
            if (motion == null)
                return;

            float x = TimeToX(offset, drawer, pps);
            float endX = TimeToX(offset + motion.Duration, drawer, pps);
            Rect bar = ClipToTrack(new Rect(x, y + 3f, Mathf.Max(4f, endX - x), MotionHeight - 6f), trackWidth);
            if (bar.width <= 0f)
                return;

            DrawRect(painter, bar, MotionColors[index % MotionColors.Length]);
            if (drawer.selectedMotionIndex == index)
                DrawOutline(painter, new Rect(LabelWidth, y, trackWidth, MotionHeight), Selection, 2f);
            if (motion.motionClip != null)
            {
                DrawRect(painter, new Rect(bar.x, bar.y, Mathf.Min(5f, bar.width), bar.height), Handle);
                DrawRect(painter, new Rect(Mathf.Max(bar.x, bar.xMax - 5f), bar.y, Mathf.Min(5f, bar.width), bar.height), Handle);
                _hitRegions.Add(new HitRegion
                {
                    kind = HitKind.Clip,
                    rect = new Rect(x - HandleHitWidth, y, HandleHitWidth * 2f, MotionHeight),
                    motion = motion,
                    motionIndex = index,
                    motionOffset = offset,
                });
                _hitRegions.Add(new HitRegion
                {
                    kind = HitKind.Clip,
                    rect = new Rect(endX - HandleHitWidth, y, HandleHitWidth * 2f, MotionHeight),
                    motion = motion,
                    motionIndex = index,
                    motionOffset = offset,
                    eventIndex = 1,
                });
            }
        }

        void DrawTimingRow(
            Painter2D painter,
            MotionSet set,
            float y,
            float trackWidth,
            float pps,
            MotionSetDrawer drawer)
        {
            DrawTrackBackground(painter, y, EventHeight, trackWidth);
            if (set?.motions == null)
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
            MotionSetDrawer drawer)
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
                        startY + row * (EventHeight + RowGap), trackWidth, pps, drawer);
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
                            startY + row * (EventHeight + RowGap), trackWidth, pps, drawer);
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
            MotionSetDrawer drawer)
        {
            DrawTrackBackground(painter, y, EventHeight, trackWidth);
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
            MotionSetDrawer drawer)
        {
            DrawTrackBackground(painter, y, EventHeight, trackWidth);
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

        void DrawCursor(Painter2D painter, MotionSetDrawer drawer, float pps, float width)
        {
            float x = TimeToX(drawer.cursorTime, drawer, pps);
            if (x < LabelWidth || x > width)
                return;
            DrawLine(painter, new Vector2(x, 0), new Vector2(x, _contentHeight), Cursor, 2f);
            painter.fillColor = Cursor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x - 6f, 0));
            painter.LineTo(new Vector2(x + 6f, 0));
            painter.LineTo(new Vector2(x, 8f));
            painter.ClosePath();
            painter.Fill();
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

                if (hit.kind == HitKind.Marker)
                {
                    _operation = DragOperation.Marker;
                    RecordUndo("Drag Timing Marker");
                    return true;
                }

                _operation = hit.eventIndex == 0 ? DragOperation.ClipStart : DragOperation.ClipEnd;
                RecordUndo(_operation == DragOperation.ClipStart ? "Drag Clip Start" : "Drag Clip End");
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
            _track.MarkDirtyRepaint();
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
