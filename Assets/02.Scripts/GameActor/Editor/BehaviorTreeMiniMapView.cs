#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// 그래프 축소 지도를 Painter2D(generateVisualContent)로 그리는 UIToolkit 미니맵.
    /// 클릭/드래그로 뷰포트 이동, 휠로 해당 지점 줌, FIT으로 전체 프레이밍.
    /// </summary>
    internal sealed class BehaviorTreeMiniMapView : VisualElement
    {
        private const float HeaderHeight = 20f;
        private readonly BehaviorTreeGraphView _graphView;
        private bool _isDraggingViewport;
        private IVisualElementScheduledItem _repaintTick;

        public BehaviorTreeMiniMapView(BehaviorTreeGraphView graphView)
        {
            _graphView = graphView;
            pickingMode = PickingMode.Position;
            style.position = Position.Absolute;
            style.right = 12f;
            style.bottom = 12f;
            style.width = 170f;
            style.height = 118f;
            style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Panel, 0.94f);
            style.borderTopColor = BehaviorTreeEditorStyles.Composite;
            style.borderRightColor = BehaviorTreeEditorStyles.Composite;
            style.borderBottomColor = BehaviorTreeEditorStyles.Composite;
            style.borderLeftColor = BehaviorTreeEditorStyles.Composite;
            style.borderTopWidth = 1f;
            style.borderRightWidth = 1f;
            style.borderBottomWidth = 1f;
            style.borderLeftWidth = 1f;
            style.borderTopLeftRadius = 8f;
            style.borderTopRightRadius = 8f;
            style.borderBottomLeftRadius = 8f;
            style.borderBottomRightRadius = 8f;

            BuildHeader();
            generateVisualContent += OnGenerateVisualContent;

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<WheelEvent>(OnWheel);

            // 그래프 팬/줌/노드 이동을 미니맵에 반영하기 위한 저빈도 리페인트.
            RegisterCallback<AttachToPanelEvent>(_ => _repaintTick = schedule.Execute(MarkDirtyRepaint).Every(120));
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                _repaintTick?.Pause();
                _repaintTick = null;
            });
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.position = Position.Absolute;
            header.style.left = 0f;
            header.style.right = 0f;
            header.style.top = 0f;
            header.style.height = HeaderHeight;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.backgroundColor = BehaviorTreeEditorStyles.PanelAlt;
            header.style.paddingLeft = 8f;
            header.style.paddingRight = 8f;
            header.pickingMode = PickingMode.Ignore;

            var title = new Label("MINIMAP");
            title.style.fontSize = 9f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = BehaviorTreeEditorStyles.TextMuted;
            title.style.flexGrow = 1;
            title.pickingMode = PickingMode.Ignore;
            header.Add(title);

            var fitButton = new Label("FIT");
            fitButton.style.fontSize = 9f;
            fitButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            fitButton.style.color = BehaviorTreeEditorStyles.Composite;
            fitButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            fitButton.style.width = 34f;
            fitButton.pickingMode = PickingMode.Position;
            fitButton.RegisterCallback<PointerDownEvent>(evt =>
            {
                _graphView?.FrameAllNodes();
                MarkDirtyRepaint();
                evt.StopPropagation();
            });
            header.Add(fitButton);

            Add(header);
        }

        private Rect GetMapRect()
        {
            return new Rect(
                8f,
                HeaderHeight + 8f,
                resolvedStyle.width - 16f,
                resolvedStyle.height - HeaderHeight - 16f);
        }

        private bool TryGetPaddedBounds(out Rect paddedBounds)
        {
            paddedBounds = default;
            if (_graphView == null)
                return false;

            var treeBounds = _graphView.GetTreeBounds();
            if (treeBounds.width <= 1f || treeBounds.height <= 1f)
                return false;

            paddedBounds = new Rect(
                treeBounds.xMin - 80f,
                treeBounds.yMin - 80f,
                treeBounds.width + 160f,
                treeBounds.height + 160f);
            return true;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (!TryGetPaddedBounds(out var paddedBounds))
                return;

            var painter = context.painter2D;
            var mapRect = GetMapRect();

            foreach (var group in _graphView.GetMiniMapGroups())
            {
                var mini = ToMini(group.Rect, paddedBounds, mapRect);
                FillRect(painter, mini, BehaviorTreeEditorStyles.WithAlpha(group.Color, 0.52f));
                StrokeRect(painter, mini, BehaviorTreeEditorStyles.WithAlpha(group.Color, 0.95f), 1f);
            }

            foreach (var edge in _graphView.GetMiniMapEdges())
            {
                var from = ToMini(edge.From, paddedBounds, mapRect);
                var to = ToMini(edge.To, paddedBounds, mapRect);
                painter.strokeColor = edge.Running
                    ? BehaviorTreeEditorStyles.Running
                    : BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.58f);
                painter.lineWidth = edge.Running ? 2f : 1f;
                painter.BeginPath();
                painter.MoveTo(from);
                painter.BezierCurveTo(
                    new Vector2(from.x, Mathf.Lerp(from.y, to.y, 0.45f)),
                    new Vector2(to.x, Mathf.Lerp(from.y, to.y, 0.55f)),
                    to);
                painter.Stroke();
            }

            foreach (var node in _graphView.GetMiniMapNodes())
            {
                var mini = ToMini(node.Rect, paddedBounds, mapRect);
                FillRect(painter, mini, node.Running
                    ? BehaviorTreeEditorStyles.Running
                    : BehaviorTreeEditorStyles.WithAlpha(node.Color, 0.82f));
            }

            DrawViewportRect(painter, paddedBounds, mapRect);
        }

        private void DrawViewportRect(Painter2D painter, Rect bounds, Rect mapRect)
        {
            var visible = _graphView.GetVisibleContentBounds();
            var mini = ToMini(visible, bounds, mapRect);
            mini.xMin = Mathf.Clamp(mini.xMin, mapRect.xMin, mapRect.xMax);
            mini.yMin = Mathf.Clamp(mini.yMin, mapRect.yMin, mapRect.yMax);
            mini.xMax = Mathf.Clamp(mini.xMax, mapRect.xMin, mapRect.xMax);
            mini.yMax = Mathf.Clamp(mini.yMax, mapRect.yMin, mapRect.yMax);

            FillRect(painter, mini, BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.14f));
            StrokeRect(painter, mini, BehaviorTreeEditorStyles.Composite, 1f);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !TryGetPaddedBounds(out var bounds))
                return;

            var mapRect = GetMapRect();
            var position = (Vector2)evt.localPosition;
            if (!mapRect.Contains(position))
                return;

            _isDraggingViewport = true;
            this.CapturePointer(evt.pointerId);
            MoveGraphToMiniMapPoint(position, bounds, mapRect);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDraggingViewport || !TryGetPaddedBounds(out var bounds))
                return;

            MoveGraphToMiniMapPoint(evt.localPosition, bounds, GetMapRect());
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0)
                return;

            _isDraggingViewport = false;
            if (this.HasPointerCapture(evt.pointerId))
                this.ReleasePointer(evt.pointerId);
        }

        private void OnWheel(WheelEvent evt)
        {
            if (!TryGetPaddedBounds(out var bounds))
                return;

            var mapRect = GetMapRect();
            var position = (Vector2)evt.localMousePosition;
            if (!mapRect.Contains(position))
                return;

            var contentPosition = FromMini(position, bounds, mapRect);
            _graphView.ZoomAroundContentPosition(contentPosition, -evt.delta.y);
            MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void MoveGraphToMiniMapPoint(Vector2 miniMapPosition, Rect bounds, Rect mapRect)
        {
            var contentPosition = FromMini(miniMapPosition, bounds, mapRect);
            _graphView.CenterOnContentPosition(contentPosition);
            MarkDirtyRepaint();
        }

        private static void FillRect(Painter2D painter, Rect rect, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            AddRectPath(painter, rect);
            painter.Fill();
        }

        private static void StrokeRect(Painter2D painter, Rect rect, Color color, float lineWidth)
        {
            painter.strokeColor = color;
            painter.lineWidth = lineWidth;
            painter.BeginPath();
            AddRectPath(painter, rect);
            painter.Stroke();
        }

        private static void AddRectPath(Painter2D painter, Rect rect)
        {
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
        }

        private static Rect ToMini(Rect source, Rect bounds, Rect mapRect)
        {
            var min = ToMini(source.min, bounds, mapRect);
            var max = ToMini(source.max, bounds, mapRect);
            return new Rect(min.x, min.y, Mathf.Max(3f, max.x - min.x), Mathf.Max(3f, max.y - min.y));
        }

        private static Vector2 ToMini(Vector2 source, Rect bounds, Rect mapRect)
        {
            var x = Mathf.InverseLerp(bounds.xMin, bounds.xMax, source.x);
            var y = Mathf.InverseLerp(bounds.yMin, bounds.yMax, source.y);
            return new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, x),
                Mathf.Lerp(mapRect.yMin, mapRect.yMax, y));
        }

        private static Vector2 FromMini(Vector2 source, Rect bounds, Rect mapRect)
        {
            var x = Mathf.InverseLerp(mapRect.xMin, mapRect.xMax, Mathf.Clamp(source.x, mapRect.xMin, mapRect.xMax));
            var y = Mathf.InverseLerp(mapRect.yMin, mapRect.yMax, Mathf.Clamp(source.y, mapRect.yMin, mapRect.yMax));
            return new Vector2(
                Mathf.Lerp(bounds.xMin, bounds.xMax, x),
                Mathf.Lerp(bounds.yMin, bounds.yMax, y));
        }
    }
}
#endif
