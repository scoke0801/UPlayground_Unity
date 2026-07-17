using System;
using UnityEngine;

namespace UPlayGround.Debugging
{
    /// <summary>
    /// GameActor 어셈블리가 상위 디버그 매니저 구현을 직접 참조하지 않도록 연결한다.
    /// </summary>
    public static class DebugGizmoBridge
    {
        public static Action<IDebugGizmoProvider> RegisterHandler;
        public static Action<IDebugGizmoProvider> UnregisterHandler;
        public static Func<DebugGizmoCategory, GameObject, DebugGizmoContentType, bool> SuppressLocalHandler;
        public static Func<DebugGizmoCategory, DebugGizmoContentType, bool> IsLocalContentEnabledHandler;

        public static void RegisterProvider(IDebugGizmoProvider provider)
        {
#if UNITY_EDITOR
            if (provider != null && Application.isPlaying)
                RegisterHandler?.Invoke(provider);
#endif
        }

        public static void UnregisterProvider(IDebugGizmoProvider provider)
        {
#if UNITY_EDITOR
            if (provider != null)
                UnregisterHandler?.Invoke(provider);
#endif
        }

        public static bool ShouldSuppressLocalGizmos(
            DebugGizmoCategory category,
            GameObject owner,
            DebugGizmoContentType contentType = DebugGizmoContentType.All)
        {
#if UNITY_EDITOR
            return SuppressLocalHandler?.Invoke(category, owner, contentType) ?? false;
#else
            return false;
#endif
        }

        public static bool IsLocalContentEnabled(
            DebugGizmoCategory category,
            DebugGizmoContentType contentType)
        {
#if UNITY_EDITOR
            return IsLocalContentEnabledHandler?.Invoke(category, contentType) ?? true;
#else
            return false;
#endif
        }

        public static void Clear()
        {
            RegisterHandler = null;
            UnregisterHandler = null;
            SuppressLocalHandler = null;
            IsLocalContentEnabledHandler = null;
        }
    }
}
