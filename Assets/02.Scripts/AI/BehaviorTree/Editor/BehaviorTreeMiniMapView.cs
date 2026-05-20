#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    internal sealed class BehaviorTreeMiniMapView : IMGUIContainer
    {
        private const float HeaderHeight = 20f;
        private readonly BehaviorTreeGraphView _graphView;
        private bool _isDraggingViewport;

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
            onGUIHandler = DrawMiniMap;
        }

        private void DrawMiniMap()
        {
            if (_graphView == null)
                return;

            var rect = new Rect(0f, 0f, resolvedStyle.width, resolvedStyle.height);
            EditorGUI.DrawRect(rect, BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Panel, 0.94f));
            EditorGUI.DrawRect(new Rect(0f, 0f, rect.width, HeaderHeight), BehaviorTreeEditorStyles.PanelAlt);
            GUI.Label(new Rect(8f, 2f, rect.width - 16f, 16f), "MINIMAP", MiniMapLabelStyle());
            var fitButtonRect = new Rect(rect.width - 42f, 3f, 34f, 14f);
            GUI.Label(fitButtonRect, "FIT", MiniMapButtonStyle());

            var treeBounds = _graphView.GetTreeBounds();
            if (treeBounds.width <= 1f || treeBounds.height <= 1f)
                return;

            var mapRect = new Rect(8f, HeaderHeight + 8f, rect.width - 16f, rect.height - HeaderHeight - 16f);
            var paddedBounds = new Rect(
                treeBounds.xMin - 80f,
                treeBounds.yMin - 80f,
                treeBounds.width + 160f,
                treeBounds.height + 160f);

            HandleInput(mapRect, fitButtonRect, paddedBounds);

            Handles.BeginGUI();
            foreach (var group in _graphView.GetMiniMapGroups())
            {
                var mini = ToMini(group.Rect, paddedBounds, mapRect);
                EditorGUI.DrawRect(mini, BehaviorTreeEditorStyles.WithAlpha(group.Color, 0.52f));
                Handles.DrawSolidRectangleWithOutline(
                    mini,
                    Color.clear,
                    BehaviorTreeEditorStyles.WithAlpha(group.Color, 0.95f));
            }

            foreach (var edge in _graphView.GetMiniMapEdges())
            {
                var from = ToMini(edge.From, paddedBounds, mapRect);
                var to = ToMini(edge.To, paddedBounds, mapRect);
                Handles.DrawBezier(
                    from,
                    to,
                    new Vector2(from.x, Mathf.Lerp(from.y, to.y, 0.45f)),
                    new Vector2(to.x, Mathf.Lerp(from.y, to.y, 0.55f)),
                    edge.Running ? BehaviorTreeEditorStyles.Running : BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.58f),
                    null,
                    edge.Running ? 2f : 1f);
            }

            foreach (var node in _graphView.GetMiniMapNodes())
            {
                var mini = ToMini(node.Rect, paddedBounds, mapRect);
                EditorGUI.DrawRect(mini, node.Running ? BehaviorTreeEditorStyles.Running : BehaviorTreeEditorStyles.WithAlpha(node.Color, 0.82f));
            }

            DrawViewportRect(paddedBounds, mapRect);
            Handles.EndGUI();
        }

        private void HandleInput(Rect mapRect, Rect fitButtonRect, Rect bounds)
        {
            var evt = Event.current;
            if (evt == null)
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (fitButtonRect.Contains(evt.mousePosition))
                {
                    _graphView.FrameAllNodes();
                    evt.Use();
                    return;
                }

                if (mapRect.Contains(evt.mousePosition))
                {
                    _isDraggingViewport = true;
                    MoveGraphToMiniMapPoint(evt.mousePosition, bounds, mapRect);
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDrag && _isDraggingViewport && evt.button == 0)
            {
                MoveGraphToMiniMapPoint(evt.mousePosition, bounds, mapRect);
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                _isDraggingViewport = false;
            }
            else if (evt.type == EventType.ScrollWheel && mapRect.Contains(evt.mousePosition))
            {
                var contentPosition = FromMini(evt.mousePosition, bounds, mapRect);
                _graphView.ZoomAroundContentPosition(contentPosition, -evt.delta.y);
                evt.Use();
            }
        }

        private void MoveGraphToMiniMapPoint(Vector2 miniMapPosition, Rect bounds, Rect mapRect)
        {
            var contentPosition = FromMini(miniMapPosition, bounds, mapRect);
            _graphView.CenterOnContentPosition(contentPosition);
            MarkDirtyRepaint();
        }

        private void DrawViewportRect(Rect bounds, Rect mapRect)
        {
            var visible = _graphView.GetVisibleContentBounds();
            var mini = ToMini(visible, bounds, mapRect);
            mini.xMin = Mathf.Clamp(mini.xMin, mapRect.xMin, mapRect.xMax);
            mini.yMin = Mathf.Clamp(mini.yMin, mapRect.yMin, mapRect.yMax);
            mini.xMax = Mathf.Clamp(mini.xMax, mapRect.xMin, mapRect.xMax);
            mini.yMax = Mathf.Clamp(mini.yMax, mapRect.yMin, mapRect.yMax);

            EditorGUI.DrawRect(mini, BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.14f));
            Handles.DrawSolidRectangleWithOutline(
                mini,
                Color.clear,
                BehaviorTreeEditorStyles.Composite);
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

        private static GUIStyle MiniMapLabelStyle()
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = BehaviorTreeEditorStyles.TextMuted },
                alignment = TextAnchor.MiddleLeft,
                fontSize = 9
            };
        }

        private static GUIStyle MiniMapButtonStyle()
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = BehaviorTreeEditorStyles.Composite },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };
        }
    }
}
#endif
