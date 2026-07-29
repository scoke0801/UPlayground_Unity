using System;
using System.Collections.Generic;
using System.Linq;
using UPlayGround.Data.Event;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public static class MotionEditorExtensionRegistry
    {
        private static IReadOnlyList<IMotionEditorPanel> _panels;
        private static IReadOnlyList<IMotionEventSceneEditor> _sceneEditors;
        private static IReadOnlyList<IMotionEventOffsetFieldProvider> _offsetProviders;

        public static IReadOnlyList<IMotionEditorPanel> Panels =>
            _panels ??= CreateAll<IMotionEditorPanel>()
                .OrderBy(panel => panel.Order)
                .ThenBy(panel => panel.Title)
                .ToArray();

        public static IMotionEventSceneEditor FindSceneEditor(MotionEventBase motionEvent)
        {
            if (motionEvent == null)
                return null;

            Type eventType = motionEvent.GetType();
            return SceneEditors
                .Where(editor => editor.EventType != null &&
                                 editor.EventType.IsAssignableFrom(eventType))
                .OrderBy(editor => GetInheritanceDistance(eventType, editor.EventType))
                .FirstOrDefault();
        }

        internal static IReadOnlyList<IMotionEventSceneEditor> SceneEditors =>
            _sceneEditors ??= CreateAll<IMotionEventSceneEditor>();

        /// <summary>
        /// 이벤트 인스턴스를 담당하는 offset 필드 provider. 없으면 null.
        /// </summary>
        public static IMotionEventOffsetFieldProvider FindOffsetFieldProvider(
            object motionEvent)
        {
            if (motionEvent == null)
                return null;

            Type eventType = motionEvent.GetType();
            _offsetProviders ??= CreateAll<IMotionEventOffsetFieldProvider>();
            return _offsetProviders
                .Where(provider => provider.EventType != null &&
                                   provider.EventType.IsAssignableFrom(eventType))
                .OrderBy(provider =>
                    GetInheritanceDistance(eventType, provider.EventType))
                .FirstOrDefault();
        }

        private static IReadOnlyList<T> CreateAll<T>()
        {
            return TypeCache.GetTypesDerivedFrom<T>()
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .Select(type => TryCreate<T>(type))
                .Where(instance => instance != null)
                .ToArray();
        }

        private static T TryCreate<T>(Type type)
        {
            try
            {
                return (T)Activator.CreateInstance(type);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MotionSetEditor] 확장 생성 실패: {type.FullName}\n{exception}");
                return default;
            }
        }

        private static int GetInheritanceDistance(Type concreteType, Type candidateType)
        {
            if (concreteType == candidateType)
                return 0;

            int distance = 1;
            for (Type current = concreteType.BaseType;
                 current != null;
                 current = current.BaseType, distance++)
            {
                if (current == candidateType)
                    return distance;
            }

            return int.MaxValue;
        }
    }
}
