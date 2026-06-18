using UnityEngine;

namespace UPlayGround.Debugging
{
    public interface IDebugGizmoProvider
    {
        DebugGizmoCategory Category { get; }
        DebugGizmoContentType ContentType { get; }
        UnityEngine.Object Owner { get; }
        bool IsAvailable { get; }

        void CollectSnapshot(DebugGizmoFrameSnapshot snapshot);
        void DrawGizmos(DebugGizmoDrawContext context);
    }
}
