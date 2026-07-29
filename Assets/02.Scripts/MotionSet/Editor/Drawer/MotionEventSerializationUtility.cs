using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// Unity 에디터 직렬화기를 사용해 SerializeReference 이벤트 그래프를 깊은 복사한다.
    /// 단순 JsonUtility와 달리 중첩 managed reference의 구체 타입을 보존한다.
    /// </summary>
    internal static class MotionEventSerializationUtility
    {
        [Serializable]
        sealed class EventContainer : ScriptableObject
        {
            [SerializeReference] public List<MotionEventBase> events = new();
        }

        public static string Serialize(IReadOnlyCollection<MotionEventBase> source)
        {
            EventContainer container = ScriptableObject.CreateInstance<EventContainer>();
            try
            {
                if (source != null)
                    foreach (MotionEventBase motionEvent in source)
                        if (motionEvent != null)
                            container.events.Add(motionEvent);
                return EditorJsonUtility.ToJson(container);
            }
            finally
            {
                container.events.Clear();
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        public static List<MotionEventBase> Deserialize(string json)
        {
            var results = new List<MotionEventBase>();
            if (string.IsNullOrEmpty(json))
                return results;
            EventContainer container = ScriptableObject.CreateInstance<EventContainer>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, container);
                if (container.events != null)
                    results.AddRange(container.events);
                container.events = new List<MotionEventBase>();
                return results;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        public static MotionEventBase Clone(MotionEventBase source)
        {
            if (source == null)
                return null;
            List<MotionEventBase> clones = Deserialize(
                Serialize(new[] { source }));
            return clones.Count > 0 ? clones[0] : null;
        }
    }
}
