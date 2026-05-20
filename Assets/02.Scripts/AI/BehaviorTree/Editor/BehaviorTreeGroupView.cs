#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    internal sealed class BehaviorTreeGroupView : GraphElement
    {
        private const float MinWidth = 220f;
        private const float MinHeight = 140f;
        private const float EdgeResizeHandleSize = 10f;
        private const float CornerResizeHandleSize = 18f;

        private readonly Action<BehaviorTreeGroupView, Rect, Vector2> _onMoved;
        private readonly Action _onChanged;
        private readonly TextField _titleField;
        private bool _suppressMoveChildren;
        private bool _isResizing;
        private Vector2 _resizeStartMouse;
        private Rect _resizeStartRect;

        public BehaviorTreeGroupView(
            BehaviorTreeEditorGroup group,
            Action<BehaviorTreeGroupView, Rect, Vector2> onMoved,
            Action onChanged)
        {
            Group = group;
            _onMoved = onMoved;
            _onChanged = onChanged;
            viewDataKey = Group.Guid;

            capabilities |= Capabilities.Movable | Capabilities.Selectable | Capabilities.Deletable;
            pickingMode = PickingMode.Position;

            style.position = Position.Absolute;
            style.borderTopWidth = 2f;
            style.borderRightWidth = 2f;
            style.borderBottomWidth = 2f;
            style.borderLeftWidth = 2f;
            style.borderTopLeftRadius = 7f;
            style.borderTopRightRadius = 7f;
            style.borderBottomLeftRadius = 7f;
            style.borderBottomRightRadius = 7f;
            ApplyColor(Group.Color);

            _titleField = CreateTitleField();
            Add(_titleField);

            Add(CreateResizeHandle(ResizeDirection.Top));
            Add(CreateResizeHandle(ResizeDirection.Right));
            Add(CreateResizeHandle(ResizeDirection.Bottom));
            Add(CreateResizeHandle(ResizeDirection.Left));
            Add(CreateResizeHandle(ResizeDirection.Top | ResizeDirection.Left));
            Add(CreateResizeHandle(ResizeDirection.Top | ResizeDirection.Right));
            Add(CreateResizeHandle(ResizeDirection.Bottom | ResizeDirection.Left));
            Add(CreateResizeHandle(ResizeDirection.Bottom | ResizeDirection.Right));

            _suppressMoveChildren = true;
            SetPosition(Group.Rect);
            _suppressMoveChildren = false;
        }

        public BehaviorTreeEditorGroup Group { get; }

        public override void SetPosition(Rect newPos)
        {
            var previousRect = Group.Rect;
            var clampedRect = new Rect(
                newPos.xMin,
                newPos.yMin,
                Mathf.Max(MinWidth, newPos.width),
                Mathf.Max(MinHeight, newPos.height));
            var delta = clampedRect.position - previousRect.position;
            var sizeChanged = !Mathf.Approximately(clampedRect.width, previousRect.width) ||
                              !Mathf.Approximately(clampedRect.height, previousRect.height);

            base.SetPosition(clampedRect);
            Group.Rect = clampedRect;

            if (!_suppressMoveChildren && !_isResizing && !sizeChanged && delta.sqrMagnitude > 0.0001f)
                _onMoved?.Invoke(this, previousRect, delta);
        }

        public void PersistPosition()
        {
            Group.Rect = GetPosition();
        }

        public void RefreshView()
        {
            _titleField.SetValueWithoutNotify(Group.Title);
            _titleField.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(Group.Color, 0.72f);
            ApplyColor(Group.Color);

            _suppressMoveChildren = true;
            SetPosition(Group.Rect);
            _suppressMoveChildren = false;
        }

        private TextField CreateTitleField()
        {
            var field = new TextField
            {
                value = Group.Title,
                isDelayed = true
            };
            field.style.height = 28f;
            field.style.marginLeft = 0f;
            field.style.marginRight = 0f;
            field.style.marginTop = 0f;
            field.style.marginBottom = 0f;
            field.style.paddingLeft = 10f;
            field.style.paddingRight = 10f;
            field.style.unityFontStyleAndWeight = FontStyle.Bold;
            field.style.fontSize = 12f;
            field.style.color = BehaviorTreeEditorStyles.Text;
            field.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(Group.Color, 0.72f);
            field.style.borderBottomColor = BehaviorTreeEditorStyles.WithAlpha(Color.white, 0.12f);
            field.style.borderBottomWidth = 1f;
            field.RegisterValueChangedCallback(evt =>
            {
                Group.Title = evt.newValue;
                _onChanged?.Invoke();
            });
            return field;
        }

        private VisualElement CreateResizeHandle(ResizeDirection direction)
        {
            var handle = new VisualElement();
            handle.style.position = Position.Absolute;
            handle.pickingMode = PickingMode.Position;
            ApplyResizeHandleLayout(handle, direction);

            handle.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                _isResizing = true;
                _resizeStartMouse = evt.mousePosition;
                _resizeStartRect = GetPosition();
                handle.CaptureMouse();
                evt.StopPropagation();
            });

            handle.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!_isResizing)
                    return;

                var delta = evt.mousePosition - _resizeStartMouse;
                var rect = CalculateResizedRect(_resizeStartRect, delta, direction);
                _suppressMoveChildren = true;
                SetPosition(rect);
                _suppressMoveChildren = false;
                evt.StopPropagation();
            });

            handle.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (!_isResizing || evt.button != 0)
                    return;

                _isResizing = false;
                handle.ReleaseMouse();
                PersistPosition();
                _onChanged?.Invoke();
                evt.StopPropagation();
            });

            return handle;
        }

        private static void ApplyResizeHandleLayout(VisualElement handle, ResizeDirection direction)
        {
            var horizontalEdge = direction is ResizeDirection.Top or ResizeDirection.Bottom;
            var verticalEdge = direction is ResizeDirection.Left or ResizeDirection.Right;

            if (horizontalEdge)
            {
                handle.style.height = EdgeResizeHandleSize;
                handle.style.left = CornerResizeHandleSize;
                handle.style.right = CornerResizeHandleSize;
            }
            else if (verticalEdge)
            {
                handle.style.width = EdgeResizeHandleSize;
                handle.style.top = CornerResizeHandleSize;
                handle.style.bottom = CornerResizeHandleSize;
            }
            else
            {
                handle.style.width = CornerResizeHandleSize;
                handle.style.height = CornerResizeHandleSize;
                handle.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(Color.white, 0.12f);
            }

            if (direction.HasFlag(ResizeDirection.Left))
                handle.style.left = 0f;
            if (direction.HasFlag(ResizeDirection.Right))
                handle.style.right = 0f;
            if (direction.HasFlag(ResizeDirection.Top))
                handle.style.top = 0f;
            if (direction.HasFlag(ResizeDirection.Bottom))
                handle.style.bottom = 0f;
        }

        private static Rect CalculateResizedRect(Rect startRect, Vector2 delta, ResizeDirection direction)
        {
            var rect = startRect;

            if (direction.HasFlag(ResizeDirection.Left))
                rect.xMin = Mathf.Min(startRect.xMax - MinWidth, startRect.xMin + delta.x);
            if (direction.HasFlag(ResizeDirection.Right))
                rect.xMax = Mathf.Max(startRect.xMin + MinWidth, startRect.xMax + delta.x);
            if (direction.HasFlag(ResizeDirection.Top))
                rect.yMin = Mathf.Min(startRect.yMax - MinHeight, startRect.yMin + delta.y);
            if (direction.HasFlag(ResizeDirection.Bottom))
                rect.yMax = Mathf.Max(startRect.yMin + MinHeight, startRect.yMax + delta.y);

            return rect;
        }

        private void ApplyColor(Color color)
        {
            style.backgroundColor = color;
            style.borderTopColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderRightColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderBottomColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderLeftColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
        }

        [Flags]
        private enum ResizeDirection
        {
            Top = 1,
            Right = 2,
            Bottom = 4,
            Left = 8
        }
    }
}
#endif
