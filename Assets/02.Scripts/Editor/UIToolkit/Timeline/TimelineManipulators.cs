using UnityEngine.UIElements;

namespace UPlayGround.Animation.Editor.UIToolkit.Timeline
{
    /// <summary>
    /// 포인터 캡처 기반 타임라인 입력. 창 밖에서 포인터를 놓아도 드래그 상태를 정리한다.
    /// </summary>
    internal sealed class TimelinePointerManipulator : Manipulator
    {
        readonly TimelineView _owner;

        public TimelinePointerManipulator(TimelineView owner)
        {
            _owner = owner;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.RegisterCallback<WheelEvent>(OnWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.UnregisterCallback<WheelEvent>(OnWheel);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (_owner.BeginPointerOperation(evt.localPosition, evt.button, evt.shiftKey))
            {
                target.CapturePointer(evt.pointerId);
                evt.StopImmediatePropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (_owner.UpdatePointerOperation(evt.localPosition))
                evt.StopImmediatePropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (_owner.EndPointerOperation())
            {
                if (target.HasPointerCapture(evt.pointerId))
                    target.ReleasePointer(evt.pointerId);
                evt.StopImmediatePropagation();
            }
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            _owner.CancelPointerOperation();
        }

        void OnWheel(WheelEvent evt)
        {
            if (_owner.HandleWheel(evt.delta.y, evt.ctrlKey || evt.commandKey, evt.shiftKey, evt.localMousePosition))
                evt.StopImmediatePropagation();
        }
    }
}
