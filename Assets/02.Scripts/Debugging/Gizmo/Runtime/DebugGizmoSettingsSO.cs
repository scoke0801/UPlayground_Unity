using UnityEngine;

namespace UPlayGround.Debugging
{
    [CreateAssetMenu(fileName = "DebugGizmoSettings", menuName = "UPlayGround/Debug/Debug Gizmo Settings")]
    public class DebugGizmoSettingsSO : ScriptableObject
    {
        public DebugGizmoCategory defaultCategories =
            DebugGizmoCategory.Combat | DebugGizmoCategory.AI | DebugGizmoCategory.Movement;

        public DebugGizmoContentType defaultContentTypes = DebugGizmoContentType.All;
        public bool drawLabels = true;
        public bool drawOnlyFocus = false;
        public float maxDrawDistance = 60f;
        public bool recordFrames = false;
        public float recordSeconds = 10f;
    }
}
