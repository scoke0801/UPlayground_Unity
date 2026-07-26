using System;
using System.Collections.Generic;
using UnityEditor;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor.UIToolkit.Timeline
{
    /// <summary>
    /// Unity의 SerializeReference 직렬화 경로를 사용해 이벤트를 깊은 복사한다.
    /// 중첩 SerializeReference와 UnityEngine.Object 참조를 보존한다.
    /// </summary>
    internal static class MotionEventClipboard
    {
        const string Prefix = "UPLAYGROUND_MOTION_EVENTS:";

        public static bool HasEvents =>
            !string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer) &&
            EditorGUIUtility.systemCopyBuffer.StartsWith(Prefix, StringComparison.Ordinal);

        public static void Copy(IReadOnlyCollection<MotionEventBase> source)
        {
            if (source == null || source.Count == 0)
                return;
            EditorGUIUtility.systemCopyBuffer = Prefix +
                MotionEventSerializationUtility.Serialize(source);
        }

        public static List<MotionEventBase> Paste()
        {
            var result = new List<MotionEventBase>();
            if (!HasEvents)
                return result;

            return MotionEventSerializationUtility.Deserialize(
                EditorGUIUtility.systemCopyBuffer.Substring(Prefix.Length));
        }
    }
}
