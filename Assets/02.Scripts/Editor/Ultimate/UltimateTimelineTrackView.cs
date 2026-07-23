#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
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
        private const float RowHeight = 22f;
        private const float RowGap = 3f;
        private const float TopPad = 6f;
        private const float BottomPad = 10f;
        private const float HandleWidth = 7f;
        private const float MinBlockPx = 14f;
        private const float InstantPx = 14f;
        private const float LaneGapPx = 4f;
        private const float MinContentWidth = 240f;
        private const float EndPadding = 60f;

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
        private readonly VisualElement _marquee;
        private readonly VisualElement _cursor;
        private readonly Label _cursorCap;

        private readonly List<Block> _blocks = new();
        private readonly HashSet<int> _selection = new();
        private readonly HashSet<int> _marqueeBase = new();
        private readonly HashSet<int> _tmpSelection = new();
        private readonly List<DragSnapshot> _dragSnapshots = new();

        private float _pps = 80f;
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
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        // ── 외부 설정 ─────────────────────────────────────
        public void SetPixelsPerSecond(float pps)
        {
            _pps = Mathf.Clamp(pps, 10f, 600f);
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
                block.Lane = lane;
                PositionBlock(block);
            }

            int laneCount = Mathf.Max(1, laneEnds.Count);
            float contentWidth = Mathf.Max(MinContentWidth, duration * _pps + EndPadding);
            float contentHeight = RulerHeight + TopPad + laneCount * (RowHeight + RowGap) + BottomPad;
            style.width = contentWidth;
            style.height = contentHeight;

            BuildTicks(duration);
            _grid.MarkDirtyRepaint();
            ApplySelectionClasses();
            if (_cursorTime.HasValue)
                SetPlayCursor(_cursorTime);
        }

        private Block CreateBlock(UltimateTimelineEvent evt, int index)
        {
            var root = new VisualElement();
            root.AddToClassList("up-ult-event");
            root.AddToClassList(UltimateEventClipboard.ResolveUssClass(evt));

            var left = new VisualElement();
            left.AddToClassList("up-ult-event__handle");
            left.pickingMode = PickingMode.Ignore;

            var label = new Label(evt.DisplayName);
            label.AddToClassList("up-ult-event__label");
            label.pickingMode = PickingMode.Ignore;

            var right = new VisualElement();
            right.AddToClassList("up-ult-event__handle");
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
                $"{block.Event.DisplayName}\n@{block.Event.startTime:0.###}s · 길이 {block.Event.duration:0.###}s";
        }

        private void BuildTicks(float duration)
        {
            _ruler.Clear();
            float step = NiceStep(duration);
            for (float t = 0f; t <= duration + 1e-4f; t += step)
            {
                var tick = new Label($"{t:0.##}s");
                tick.AddToClassList("up-ult-tick");
                tick.pickingMode = PickingMode.Ignore;
                tick.style.left = t * _pps + 2f;
                _ruler.Add(tick);
            }
        }

        private static float NiceStep(float duration)
        {
            foreach (float step in NiceSteps)
            {
                if (duration / step <= 12f)
                    return step;
            }

            return duration / 12f;
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

            float step = NiceStep(duration);
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
            else if (block.Event.duration > 0f && localX >= width - HandleWidth)
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
