using System;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    public interface IMotionEventSceneEditor
    {
        Type EventType { get; }
        bool OnSceneGUI(MotionEventBase motionEvent, IMotionEditorContext context);
        void OnInspectorGUI(MotionEventBase motionEvent, IMotionEditorContext context);
    }
}
