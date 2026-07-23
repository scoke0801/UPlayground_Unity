#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UPlayGround.Data.Editor
{
    /// <summary>
    /// 궁극기 타임라인 이벤트의 타입 메타데이터, 딥클론, 에셋 간 클립보드를 제공한다.
    /// 클론은 UnityEngine.Object 참조(프리팹/클립/카메라 SO)는 공유하고
    /// 직렬화 데이터(구조체/enum/리스트/문자열)만 깊게 복제한다.
    /// </summary>
    internal static class UltimateEventClipboard
    {
        internal readonly struct EventKind
        {
            public readonly Type Type;
            public readonly string Label;
            public readonly string UssClass;

            public EventKind(Type type, string label, string ussClass)
            {
                Type = type;
                Label = label;
                UssClass = ussClass;
            }
        }

        /// <summary>추가 메뉴와 블록 색상에서 공통으로 쓰는 이벤트 타입 목록.</summary>
        public static readonly EventKind[] Kinds =
        {
            new(typeof(UltimateSpawnVfxEvent), "VFX 생성", "up-ult-event--vfx"),
            new(typeof(UltimateSoundEvent), "SFX / 보이스", "up-ult-event--sound"),
            new(typeof(UltimateTimeScaleEvent), "타임스케일", "up-ult-event--time"),
            new(typeof(UltimateCameraEffectEvent), "카메라 효과", "up-ult-event--camfx"),
            new(typeof(UltimateCameraShakeEvent), "카메라 흔들림", "up-ult-event--shake"),
            new(typeof(UltimateDamageWindowEvent), "데미지 윈도우", "up-ult-event--damage"),
            new(typeof(UltimateCustomCallbackEvent), "커스텀 콜백", "up-ult-event--callback"),
        };

        public static string ResolveUssClass(UltimateTimelineEvent timelineEvent)
        {
            if (timelineEvent == null)
                return "up-ult-event--callback";

            Type type = timelineEvent.GetType();
            foreach (EventKind kind in Kinds)
            {
                if (kind.Type == type)
                    return kind.UssClass;
            }

            return "up-ult-event--callback";
        }

        private static readonly List<UltimateTimelineEvent> Buffer = new();

        public static bool HasContent => Buffer.Count > 0;
        public static int Count => Buffer.Count;

        public static void Copy(IEnumerable<UltimateTimelineEvent> events)
        {
            Buffer.Clear();
            if (events == null)
                return;

            foreach (UltimateTimelineEvent timelineEvent in events)
            {
                if (timelineEvent != null)
                    Buffer.Add(Clone(timelineEvent));
            }
        }

        public static List<UltimateTimelineEvent> Paste()
        {
            var result = new List<UltimateTimelineEvent>(Buffer.Count);
            foreach (UltimateTimelineEvent timelineEvent in Buffer)
                result.Add(Clone(timelineEvent));
            return result;
        }

        public static UltimateTimelineEvent Clone(UltimateTimelineEvent source)
        {
            return (UltimateTimelineEvent)DeepClone(source);
        }

        private static object DeepClone(object source)
        {
            if (source == null)
                return null;

            Type type = source.GetType();

            // 에셋 참조는 공유한다.
            if (source is UnityEngine.Object)
                return source;

            // 구조체(Vector3/LayerMask/enum 등)·기본형·문자열은 값 복사.
            if (type.IsValueType || source is string)
                return source;

            if (source is IList sourceList)
            {
                var cloneList = (IList)Activator.CreateInstance(type);
                foreach (object item in sourceList)
                    cloneList.Add(DeepClone(item));
                return cloneList;
            }

            object clone = Activator.CreateInstance(type);
            for (Type cursor = type; cursor != null && cursor != typeof(object); cursor = cursor.BaseType)
            {
                FieldInfo[] fields = cursor.GetFields(
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    if (!IsSerializedField(field))
                        continue;
                    field.SetValue(clone, DeepClone(field.GetValue(source)));
                }
            }

            return clone;
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsDefined(typeof(NonSerializedAttribute), false))
                return false;
            if (field.IsPublic)
                return true;
            return field.IsDefined(typeof(SerializeField), false)
                   || field.IsDefined(typeof(SerializeReference), false);
        }
    }
}
#endif
