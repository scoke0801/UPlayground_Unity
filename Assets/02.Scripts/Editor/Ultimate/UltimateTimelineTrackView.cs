#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Data.Editor
{
    /// <summary>
    /// 궁극기 시퀀스의 인터랙티브 타임라인 캔버스.
    /// 이벤트 블록을 드래그 이동/리사이즈하고, 빈 곳 드래그로 다중 선택하며,
    /// 겹치는 이벤트는 레인으로 자동 분리한다. 구조 변경(추가/삭제/붙여넣기)은
    /// 소유 윈도우가 SerializedObject로 처리하고, 여기서는 선택/드래그/레이아웃만 담당한다.
    /// </summary>
    internal sealed class UltimateTimelineTrackView : VisualElement
    {
        private const float RulerHeight = 24f;
        private const float RowHeight = 26f;
        private const float RowGap = 4f;
        private const float TopPad = 8f;
        private const float BottomPad = 10f;
        private const float HandleWidth = 7f;
        private const float MinBlockPx = 14f;
        private const float InstantPx = 104f;
        private const float LaneGapPx = 4f;
        private const float MinContentWidth = 240f;
        private const float EndPadding = 60f;
        private const float MinMajorTickSpacing = 48f;

        private static readonly Color RulerBg = new(0.06f, 0.085f, 0.11f);
        private static readonly Color GridLine = new(0.16f, 0.21f, 0.26f);
        private static readonly Color EndLine = new(0.5f, 0.62f, 0.72f, 0.7f);
        private static readonly Color BaseLine = new(0.22f, 0.30f, 0.37f);

        private static readonly float[] NiceSteps = { 0.05f, 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 20f };

        private enum DragMode
        {
            None,
            MoveBody,
            ResizeStart,
            ResizeEnd,
            Marquee,
        }

        private sealed class Block
        {
            public VisualElement Root;
            public Label Label;
            public int Index;
            public int Lane;
            public UltimateTimelineEvent Event;
        }

        private readonly struct DragSnapshot
        {
            public readonly UltimateTimelineEvent Event;
            public readonly float Start;
            public readonly float Dur;

            public DragSnapshot(UltimateTimelineEvent evt)
            {
                Event = evt;
                Start = evt.startTime;
                Dur = evt.duration;
            }
        }

        private readonly Func<UltimateSequenceAsset> _getAsset;
        private readonly Func<float> _getDuration;
        private readonly Func<UnityEngine.Object> _getUndoTarget;
        private readonly Action _onDataChanged;
        private readonly Action _onSelectionChanged;

        private readonly VisualElement _grid;
        private readonly VisualElement _ruler;
        private readonly VisualElement _blockLayer;
        private readonly VisualElement _emptyState;
        private readonly Label _emptyStateDescription;
        private readonly VisualElement _marquee;
        private readonly VisualElement _cursor;
        private readonly Label _cursorCap;

        private readonly List<Block> _blocks = new();
        private readonly HashSet<int> _selection = new();
        private readonly HashSet<int> _marqueeBase = new();
        private readonly HashSet<int> _tmpSelection = new();
        private readonly List<DragSnapshot> _dragSnapshots = new();

        private float _pps = 80f;
        private float _viewportWidth;
        private float _viewportHeight;
        private int _laneCount = 1;
        private float _contextTime;
        private bool _snap;
        private int _fps = 30;
        private float? _cursorTime;

        private DragMode _drag = DragMode.None;
        private float _dragStartCanvasX;
        private Block _resizeTarget;
        private Vector2 _marqueeStart;
        private string _dragUndoLabel;
        private bool _dragChanged;

        // 소유 윈도우가 주입하는 구조 편집 훅.
        public Action CopySelected;
        public Action DeleteSelected;
        public Action DuplicateSelected;
        public Action PasteClipboard;
        public Func<bool> CanPaste;
        public Action<Type> AddEvent;
        public Action<Type, float> AddEventAtTime;

        public IReadOnlyCollection<int> Selection => _selection;
        public bool IsDragging => _drag != DragMode.None;

        public UltimateTimelineTrackView(
            Func<UltimateSequenceAsset> getAsset,
            Func<float> getDuration,
            Func<UnityEngine.Object> getUndoTarget,
            Action onDataChanged,
            Action onSelectionChanged)
        {
            _getAsset = getAsset;
            _getDuration = getDuration;
            _getUndoTarget = getUndoTarget;
            _onDataChanged = onDataChanged;
            _onSelectionChanged = onSelectionChanged;

            AddToClassList("up-ult-canvas");
            focusable = true;

            _grid = new VisualElement();
            _grid.AddToClassList("up-ult-grid");
            _grid.generateVisualContent += DrawGrid;
            _grid.RegisterCallback<PointerDownEvent>(OnGridPointerDown);
            Add(_grid);

            _ruler = new VisualElement();
            _ruler.AddToClassList("up-ult-ruler");
            _ruler.pickingMode = PickingMode.Ignore;
            Add(_ruler);

            _blockLayer = new VisualElement();
            _blockLayer.style.position = Position.Absolute;
            _blockLayer.style.left = 0;
            _blockLayer.style.top = 0;
            _blockLayer.style.right = 0;
            _blockLayer.style.bottom = 0;
            _blockLayer.pickingMode = PickingMode.Ignore;
            Add(_blockLayer);

            _emptyState = new VisualElement();
            _emptyState.AddToClassList("up-ult-timeline-empty");

            var emptyKicker = new Label("EMPTY SEQUENCE");
            emptyKicker.AddToClassList("up-ult-timeline-empty__kicker");
            _emptyState.Add(emptyKicker);

            var emptyTitle = new Label("첫 연출 이벤트를 배치하세요");
            emptyTitle.AddToClassList("up-ult-timeline-empty__title");
            _emptyState.Add(emptyTitle);

            _emptyStateDescription = new Label();
            _emptyStateDescription.AddToClassList("up-ult-timeline-empty__description");
            _emptyState.Add(_emptyStateDescription);

            var emptyActions = new VisualElement();
            emptyActions.AddToClassList("up-ult-timeline-empty__actions");
            var addMenu = new ToolbarMenu { text = "＋ 이벤트 추가" };
            addMenu.AddToClassList("up-ult-timeline-empty__add");
            foreach (UltimateEventClipboard.EventKind kind in UltimateEventClipboard.Kinds)
            {
                UltimateEventClipboard.EventKind captured = kind;
                addMenu.menu.AppendAction(
                    captured.Label,
                    _ => AddEvent?.Invoke(captured.Type),
                    _ => _getAsset() != null
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
            }
            emptyActions.Add(addMenu);
            _emptyState.Add(emptyActions);
            Add(_emptyState);

            _marquee = new VisualElement();
            _marquee.AddToClassList("up-ult-marquee");
            _marquee.pickingMode = PickingMode.Ignore;
            _marquee.style.display = DisplayStyle.None;
            Add(_marquee);

            _cursor = new VisualElement();
            _cursor.AddToClassList("up-ult-cursor");
            _cursor.pickingMode = PickingMode.Ignore;
            _cursor.style.display = DisplayStyle.None;
            _cursorCap = new Label();
            _cursorCap.AddToClassList("up-ult-cursor-cap");
            _cursorCap.pickingMode = PickingMode.Ignore;
            _cursor.Add(_cursorCap);
            Add(_cursor);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        // ── 외부 설정 ─────────────────────────────────────
        public void SetPixelsPerSecond(float pps)
        {
            _pps = Mathf.Clamp(pps, 10f, 600f);
            RefreshLayout();
        }

        public void SetViewportWidth(float width)
        {
            float next = Mathf.Max(0f, width);
            if (Mathf.Approximately(_viewportWidth, next))
                return;

            _viewportWidth = next;
            RefreshLayout();
        }

        public void SetViewportSize(Vector2 size)
        {
            // 바깥 호스트의 크기를 사용하므로 ScrollView 콘텐츠 측정과 순환하지 않는다.
            // 하단 가로 스크롤바 여유를 미리 빼 세로 스크롤이 1px 차이로 생기는 것도 막는다.
            float nextWidth = Mathf.Max(0f, size.x - 1f);
            float nextHeight = Mathf.Max(0f, size.y - 16f);
            if (Mathf.Approximately(_viewportWidth, nextWidth)
                && Mathf.Approximately(_viewportHeight, nextHeight))
                return;

            _viewportWidth = nextWidth;
            _viewportHeight = nextHeight;
            RefreshLayout();
        }

        public void SetSnap(bool enabled, int fps)
        {
            _snap = enabled;
            _fps = Mathf.Max(1, fps);
        }

        public void SetPlayCursor(float? time)
        {
            _cursorTime = time;
            if (!time.HasValue)
            {
                _cursor.style.display = DisplayStyle.None;
                return;
            }

            _cursor.style.display = DisplayStyle.Flex;
            _cursor.style.left = time.Value * _pps;
            _cursorCap.text = $"{time.Value:0.00}s";
        }

        // ── 선택 ──────────────────────────────────────────
        public void ClearSelection()
        {
            if (_selection.Count == 0)
                return;
            _selection.Clear();
            ApplySelectionClasses();
            _onSelectionChanged?.Invoke();
        }

        public void SelectIndices(IEnumerable<int> indices)
        {
            _selection.Clear();
            if (indices != null)
            {
                foreach (int i in indices)
                    _selection.Add(i);
            }

            ApplySelectionClasses();
            _onSelectionChanged?.Invoke();
        }

        private void SetSelectionSingle(int index)
        {
            _selection.Clear();
            _selection.Add(index);
            ApplySelectionClasses();
            _onSelectionChanged?.Invoke();
        }

        private void ToggleSelection(int index)
        {
            if (!_selection.Remove(index))
                _selection.Add(index);
            ApplySelectionClasses();
            _onSelectionChanged?.Invoke();
        }

        private void SelectAll()
        {
            _selection.Clear();
            foreach (Block block in _blocks)
                _selection.Add(block.Index);
            ApplySelectionClasses();
            _onSelectionChanged?.Invoke();
        }

        private void ApplySelectionClasses()
        {
            foreach (Block block in _blocks)
                block.Root.EnableInClassList("up-ult-event--selected", _selection.Contains(block.Index));
        }

        // ── 재구성 / 레이아웃 ─────────────────────────────
        public void Rebuild()
        {
            foreach (Block block in _blocks)
                _blockLayer.Remove(block.Root);
            _blocks.Clear();

            UltimateSequenceAsset asset = _getAsset();
            int count = asset?.events?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                UltimateTimelineEvent evt = asset.events[i];
                if (evt == null)
                    continue;

                Block block = CreateBlock(evt, i);
                _blocks.Add(block);
                _blockLayer.Add(block.Root);
            }

            _selection.RemoveWhere(s => s < 0 || s >= count);
            _emptyState.style.display = _blocks.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateEmptyStateDescription();
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            float duration = Mathf.Max(0.01f, _getDuration());

            var order = new List<Block>(_blocks);
            order.Sort((a, b) =>
            {
                int c = a.Event.startTime.CompareTo(b.Event.startTime);
                return c != 0 ? c : a.Index.CompareTo(b.Index);
            });

            var laneEnds = new List<float>();
            float maxEventRight = duration * _pps;
            foreach (Block block in order)
            {
                float x0 = block.Event.startTime * _pps;
                float w = block.Event.duration > 0f
                    ? Mathf.Max(MinBlockPx, block.Event.duration * _pps)
                    : InstantPx;

                int lane = -1;
                for (int l = 0; l < laneEnds.Count; l++)
                {
                    if (laneEnds[l] <= x0 - LaneGapPx)
                    {
                        lane = l;
                        break;
                    }
                }

                if (lane < 0)
                {
                    lane = laneEnds.Count;
                    laneEnds.Add(0f);
                }

                laneEnds[lane] = x0 + w;
                maxEventRight = Mathf.Max(maxEventRight, x0 + w);
                block.Lane = lane;
                PositionBlock(block);
            }

            int laneCount = Mathf.Max(1, laneEnds.Count);
            _laneCount = laneCount;
            float contentWidth = Mathf.Max(MinContentWidth, _viewportWidth, maxEventRight + EndPadding);
            float laneContentHeight = RulerHeight + TopPad + laneCount * (RowHeight + RowGap) + BottomPad;
            float contentHeight = _blocks.Count == 0
                ? Mathf.Max(220f, laneContentHeight, _viewportHeight)
                : Mathf.Max(laneContentHeight, _viewportHeight);
            style.width = contentWidth;
            style.height = contentHeight;

            // ScrollView 콘텐츠 높이와 뷰포트 높이를 서로 연동하면 레이아웃 계산이
            // 순환해 불필요한 세로 스크롤이 생길 수 있다. 빈 상태 카드는 고정된
            // 콘텐츠 영역 안에서 가로 폭만 기준으로 배치한다.
            float emptyWidth = Mathf.Clamp(_viewportWidth - 32f, 260f, 440f);
            _emptyState.style.width = emptyWidth;
            _emptyState.style.left = Mathf.Max(16f, (_viewportWidth - emptyWidth) * 0.5f);

            BuildTicks(duration);
            _grid.MarkDirtyRepaint();
            ApplySelectionClasses();
            if (_cursorTime.HasValue)
                SetPlayCursor(_cursorTime);
        }

        private void UpdateEmptyStateDescription()
        {
            float duration = Mathf.Max(0f, _getDuration());
            _emptyStateDescription.text = duration > 0.001f
                ? $"모션 길이 {duration:0.##}초 · 이벤트를 추가한 뒤 블록을 드래그해 타이밍을 조정할 수 있습니다."
                : "먼저 MotionSet을 연결한 뒤 VFX, 사운드, 카메라, 데미지 이벤트를 배치하세요.";
        }

        private Block CreateBlock(UltimateTimelineEvent evt, int index)
        {
            var root = new VisualElement();
            root.AddToClassList("up-ult-event");
            root.AddToClassList(UltimateEventClipboard.ResolveUssClass(evt));

            var left = new VisualElement();
            left.AddToClassList("up-ult-event__handle");
            left.AddToClassList("up-ult-event__handle--left");
            left.pickingMode = PickingMode.Ignore;

            var label = new Label(evt.DisplayName);
            label.AddToClassList("up-ult-event__label");
            label.pickingMode = PickingMode.Ignore;

            var right = new VisualElement();
            right.AddToClassList("up-ult-event__handle");
            right.AddToClassList("up-ult-event__handle--right");
            right.pickingMode = PickingMode.Ignore;

            root.Add(left);
            root.Add(label);
            root.Add(right);

            var block = new Block
            {
                Root = root,
                Label = label,
                Index = index,
                Event = evt,
            };
            root.userData = block;
            root.RegisterCallback<PointerDownEvent>(e => OnBlockPointerDown(e, block));
            return block;
        }

        private void PositionBlock(Block block)
        {
            bool instant = block.Event.duration <= 0f;
            float x0 = block.Event.startTime * _pps;
            float w = instant ? InstantPx : Mathf.Max(MinBlockPx, block.Event.duration * _pps);

            block.Root.style.left = x0;
            block.Root.style.width = w;
            block.Root.style.top = RulerHeight + TopPad + block.Lane * (RowHeight + RowGap);
            block.Root.EnableInClassList("up-ult-event--instant", instant);
            block.Label.text = block.Event.DisplayName;
            block.Root.tooltip =
                $"{block.Event.DisplayName}\n@{block.Event.startTime:0.###}s · 길이 {block.Event.duration:0.###}s"
                + (instant ? "\n오른쪽 끝을 드래그하면 구간 이벤트로 확장됩니다." : string.Empty);
        }

        private void BuildTicks(float duration)
        {
            _ruler.Clear();
            float step = NiceStep();
            for (float t = 0f; t <= duration + 1e-4f; t += step)
            {
                var tick = new Label($"{t:0.##}s");
                tick.AddToClassList("up-ult-tick");
                tick.pickingMode = PickingMode.Ignore;
                tick.style.left = t * _pps + 2f;
                _ruler.Add(tick);
            }
        }

        /// <summary>
        /// 현재 줌에서 라벨이 겹치지 않는 주요 눈금 간격을 선택한다.
        /// 시간 길이만 기준으로 잡으면 짧은 시퀀스에서 0.25초 라벨이
        /// 20px 간격으로 몰리므로 실제 화면 픽셀 간격을 기준으로 한다.
        /// </summary>
        private float NiceStep()
        {
            foreach (float step in NiceSteps)
            {
                if (step * _pps >= MinMajorTickSpacing)
                    return step;
            }

            return NiceSteps[NiceSteps.Length - 1];
        }

        private void DrawGrid(MeshGenerationContext mgc)
        {
            Rect rect = mgc.visualElement.contentRect;
            if (rect.width < 1f || rect.height < 1f)
                return;

            float duration = Mathf.Max(0.01f, _getDuration());
            Painter2D p = mgc.painter2D;

            p.fillColor = RulerBg;
            p.BeginPath();
            p.MoveTo(new Vector2(0f, 0f));
            p.LineTo(new Vector2(rect.width, 0f));
            p.LineTo(new Vector2(rect.width, RulerHeight));
            p.LineTo(new Vector2(0f, RulerHeight));
            p.ClosePath();
            p.Fill();

            // 트랙 행을 교대로 구분해 이벤트가 속한 레인을 빠르게 읽을 수 있게 한다.
            for (int lane = 0; lane < _laneCount; lane++)
            {
                float y = RulerHeight + TopPad + lane * (RowHeight + RowGap);
                if ((lane & 1) == 1)
                {
                    p.fillColor = new Color(0.11f, 0.15f, 0.18f, 0.22f);
                    p.BeginPath();
                    p.MoveTo(new Vector2(0f, y));
                    p.LineTo(new Vector2(rect.width, y));
                    p.LineTo(new Vector2(rect.width, y + RowHeight));
                    p.LineTo(new Vector2(0f, y + RowHeight));
                    p.ClosePath();
                    p.Fill();
                }

                p.strokeColor = new Color(BaseLine.r, BaseLine.g, BaseLine.b, 0.42f);
                p.lineWidth = 1f;
                p.BeginPath();
                p.MoveTo(new Vector2(0f, y + RowHeight));
                p.LineTo(new Vector2(rect.width, y + RowHeight));
                p.Stroke();
            }

            float step = NiceStep();

            // 주요 눈금 사이에는 라벨 없는 보조 눈금만 그려 시간 감각은 유지한다.
            float minorStep = step * 0.5f;
            p.strokeColor = new Color(GridLine.r, GridLine.g, GridLine.b, 0.45f);
            p.lineWidth = 1f;
            p.BeginPath();
            for (float t = minorStep; t <= duration + 1e-4f; t += step)
            {
                float x = t * _pps;
                p.MoveTo(new Vector2(x, RulerHeight * 0.55f));
                p.LineTo(new Vector2(x, rect.height));
            }

            p.Stroke();

            p.strokeColor = GridLine;
            p.lineWidth = 1f;
            p.BeginPath();
            for (float t = 0f; t <= duration + 1e-4f; t += step)
            {
                float x = t * _pps;
                p.MoveTo(new Vector2(x, 0f));
                p.LineTo(new Vector2(x, rect.height));
            }

            p.Stroke();

            float endX = duration * _pps;
            p.strokeColor = EndLine;
            p.lineWidth = 1.5f;
            p.BeginPath();
            p.MoveTo(new Vector2(endX, 0f));
            p.LineTo(new Vector2(endX, rect.height));
            p.Stroke();

            p.strokeColor = BaseLine;
            p.lineWidth = 1f;
            p.BeginPath();
            p.MoveTo(new Vector2(0f, RulerHeight));
            p.LineTo(new Vector2(rect.width, RulerHeight));
            p.Stroke();
        }

        // ── 블록 드래그 ───────────────────────────────────
        private void OnBlockPointerDown(PointerDownEvent evt, Block block)
        {
            if (evt.button == 1)
            {
                // 우클릭: 선택만 보정하고 컨텍스트 메뉴는 버블링에 맡긴다.
                if (!_selection.Contains(block.Index))
                    SetSelectionSingle(block.Index);
                return;
            }

            if (evt.button != 0)
                return;

            evt.StopPropagation();
            Focus();

            bool additive = evt.shiftKey || evt.ctrlKey || evt.commandKey;
            if (additive)
                ToggleSelection(block.Index);
            else if (!_selection.Contains(block.Index))
                SetSelectionSingle(block.Index);

            if (!_selection.Contains(block.Index))
                return; // additive 토글로 선택 해제된 경우 드래그하지 않는다.

            float width = block.Root.resolvedStyle.width;
            float localX = evt.localPosition.x;
            DragMode mode;
            if (block.Event.duration > 0f && localX <= HandleWidth)
                mode = DragMode.ResizeStart;
            else if (localX >= width - HandleWidth)
                mode = DragMode.ResizeEnd;
            else
                mode = DragMode.MoveBody;

            BeginDrag(mode, block, evt.position, evt.pointerId);
        }

        private void BeginDrag(DragMode mode, Block target, Vector2 pointerPos, int pointerId)
        {
            _drag = mode;
            _resizeTarget = target;
            _dragStartCanvasX = ToCanvasX(pointerPos);
            _dragUndoLabel = mode == DragMode.MoveBody ? "궁극기 이벤트 이동" : "궁극기 이벤트 리사이즈";
            _dragChanged = false;
            _dragSnapshots.Clear();
            if (mode == DragMode.MoveBody)
            {
                UltimateSequenceAsset asset = _getAsset();
                foreach (int idx in _selection)
                {
                    if (idx >= 0 && idx < asset.events.Count && asset.events[idx] != null)
                        _dragSnapshots.Add(new DragSnapshot(asset.events[idx]));
                }
            }
            else
            {
                _dragSnapshots.Add(new DragSnapshot(target.Event));
            }

            this.CapturePointer(pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_drag == DragMode.None)
                return;

            if (_drag == DragMode.Marquee)
            {
                UpdateMarquee(evt.position);
                evt.StopPropagation();
                return;
            }

            float duration = Mathf.Max(0.01f, _getDuration());
            float deltaTime = (ToCanvasX(evt.position) - _dragStartCanvasX) / _pps;

            // 실제 이동이 시작될 때 한 번만 Undo를 기록한다(선택만 하는 클릭은 기록하지 않음).
            if (!_dragChanged)
            {
                RegisterUndo(_dragUndoLabel);
                _dragChanged = true;
            }

            switch (_drag)
            {
                case DragMode.MoveBody when _dragSnapshots.Count > 0:
                {
                    float lowest = float.MaxValue;
                    float highest = float.MinValue;
                    foreach (DragSnapshot s in _dragSnapshots)
                    {
                        lowest = Mathf.Min(lowest, s.Start);
                        highest = Mathf.Max(highest, s.Start);
                    }

                    float delta = deltaTime;
                    delta = Mathf.Max(delta, -lowest);
                    delta = Mathf.Min(delta, duration - highest);
                    if (_snap)
                        delta = SnapValue(lowest + delta) - lowest;

                    foreach (DragSnapshot s in _dragSnapshots)
                        s.Event.startTime = Mathf.Clamp(s.Start + delta, 0f, duration);
                    break;
                }

                case DragMode.ResizeStart:
                {
                    DragSnapshot s = _dragSnapshots[0];
                    float origEnd = s.Start + s.Dur;
                    float ns = Mathf.Clamp(SnapValue(s.Start + deltaTime), 0f, origEnd);
                    s.Event.startTime = ns;
                    s.Event.duration = origEnd - ns;
                    break;
                }

                case DragMode.ResizeEnd:
                {
                    DragSnapshot s = _dragSnapshots[0];
                    // 인스턴트 이벤트의 넓은 블록은 선택 편의를 위한 표시 폭일 뿐이다.
                    // 표시 폭을 시간으로 환산하면 같은 드래그가 줌 배율마다 다른 duration을 만든다.
                    float ne = Mathf.Max(SnapValue(s.Start + s.Dur + deltaTime), s.Start);
                    s.Event.duration = ne - s.Start;
                    break;
                }
            }

            PositionAffected();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_drag == DragMode.None)
                return;

            if (this.HasPointerCapture(evt.pointerId))
                this.ReleasePointer(evt.pointerId);

            if (_drag == DragMode.Marquee)
            {
                _marquee.style.display = DisplayStyle.None;
                _drag = DragMode.None;
                _onSelectionChanged?.Invoke();
                return;
            }

            _drag = DragMode.None;
            _resizeTarget = null;

            if (!_dragChanged)
                return; // 이동 없는 클릭: 데이터 변경/재배치 불필요.

            _dragChanged = false;
            UnityEngine.Object undoTarget = _getUndoTarget();
            if (undoTarget != null)
                EditorUtility.SetDirty(undoTarget);
            RefreshLayout();
            _onDataChanged?.Invoke();
        }

        private void PositionAffected()
        {
            if (_drag == DragMode.MoveBody)
            {
                foreach (Block block in _blocks)
                {
                    if (_selection.Contains(block.Index))
                        PositionBlock(block);
                }
            }
            else if (_resizeTarget != null)
            {
                PositionBlock(_resizeTarget);
            }
        }

        // ── 마퀴 다중 선택 ────────────────────────────────
        private void OnGridPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            Focus();
            bool additive = evt.shiftKey || evt.ctrlKey || evt.commandKey;
            _marqueeBase.Clear();
            if (additive)
            {
                foreach (int s in _selection)
                    _marqueeBase.Add(s);
            }
            else if (_selection.Count > 0)
            {
                _selection.Clear();
                ApplySelectionClasses();
                _onSelectionChanged?.Invoke();
            }

            _drag = DragMode.Marquee;
            _marqueeStart = this.WorldToLocal(evt.position);
            _marquee.style.display = DisplayStyle.Flex;
            PositionMarquee(_marqueeStart, _marqueeStart);
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void UpdateMarquee(Vector2 panelPos)
        {
            Vector2 current = this.WorldToLocal(panelPos);
            PositionMarquee(_marqueeStart, current);

            Rect area = RectFromPoints(_marqueeStart, current);
            _tmpSelection.Clear();
            foreach (int s in _marqueeBase)
                _tmpSelection.Add(s);

            foreach (Block block in _blocks)
            {
                if (area.Overlaps(block.Root.layout))
                    _tmpSelection.Add(block.Index);
            }

            // 선택 집합이 실제로 바뀐 경우에만 인스펙터/클래스를 갱신한다.
            if (_tmpSelection.SetEquals(_selection))
                return;

            _selection.Clear();
            foreach (int s in _tmpSelection)
                _selection.Add(s);
            ApplySelectionClasses();
            _onSelectionChanged?.Invoke();
        }

        private void PositionMarquee(Vector2 a, Vector2 b)
        {
            _marquee.style.left = Mathf.Min(a.x, b.x);
            _marquee.style.top = Mathf.Min(a.y, b.y);
            _marquee.style.width = Mathf.Abs(a.x - b.x);
            _marquee.style.height = Mathf.Abs(a.y - b.y);
        }

        private static Rect RectFromPoints(Vector2 a, Vector2 b)
        {
            return new Rect(
                Mathf.Min(a.x, b.x),
                Mathf.Min(a.y, b.y),
                Mathf.Abs(a.x - b.x),
                Mathf.Abs(a.y - b.y));
        }

        // ── 컨텍스트 메뉴 / 키보드 ────────────────────────
        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 1)
                return;

            float duration = Mathf.Max(0f, _getDuration());
            _contextTime = Mathf.Clamp(ToCanvasX(evt.position) / _pps, 0f, duration);
            _contextTime = SnapValue(_contextTime);
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            int count = _selection.Count;
            if (count > 0)
            {
                evt.menu.AppendAction($"복제 ({count})", _ => DuplicateSelected?.Invoke());
                evt.menu.AppendAction($"복사 ({count})", _ => CopySelected?.Invoke());
                evt.menu.AppendAction($"삭제 ({count})", _ => DeleteSelected?.Invoke());
                evt.menu.AppendSeparator();
            }

            bool canAdd = _getAsset() != null && AddEventAtTime != null;
            foreach (UltimateEventClipboard.EventKind kind in UltimateEventClipboard.Kinds)
            {
                UltimateEventClipboard.EventKind captured = kind;
                evt.menu.AppendAction(
                    $"이벤트 추가 @ {_contextTime:0.##}s/{captured.Label}",
                    _ => AddEventAtTime?.Invoke(captured.Type, _contextTime),
                    canAdd
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
            }

            evt.menu.AppendSeparator();

            bool canPaste = CanPaste?.Invoke() ?? false;
            evt.menu.AppendAction(
                "붙여넣기",
                _ => PasteClipboard?.Invoke(),
                canPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            bool cmd = evt.ctrlKey || evt.commandKey;
            if (evt.keyCode is KeyCode.Delete or KeyCode.Backspace)
            {
                DeleteSelected?.Invoke();
                evt.StopPropagation();
            }
            else if (cmd && evt.keyCode == KeyCode.C)
            {
                CopySelected?.Invoke();
                evt.StopPropagation();
            }
            else if (cmd && evt.keyCode == KeyCode.V)
            {
                PasteClipboard?.Invoke();
                evt.StopPropagation();
            }
            else if (cmd && evt.keyCode == KeyCode.D)
            {
                DuplicateSelected?.Invoke();
                evt.StopPropagation();
            }
            else if (cmd && evt.keyCode == KeyCode.A)
            {
                SelectAll();
                evt.StopPropagation();
            }
        }

        // ── 유틸 ──────────────────────────────────────────
        private float ToCanvasX(Vector2 panelPos) => this.WorldToLocal(panelPos).x;

        private float SnapValue(float t) => _snap && _fps > 0 ? Mathf.Round(t * _fps) / _fps : t;

        private void RegisterUndo(string label)
        {
            UnityEngine.Object target = _getUndoTarget();
            if (target != null)
                Undo.RegisterCompleteObjectUndo(target, label);
        }
    }
}
#endif
