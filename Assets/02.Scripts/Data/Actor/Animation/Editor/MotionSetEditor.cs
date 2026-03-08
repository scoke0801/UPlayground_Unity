using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionEventBase 서브클래스 관리 유틸리티
    /// </summary>
    public static class MotionEventTypeRegistry
    {
        static Type[] _eventTypes;
        static string[] _eventTypeNames;
        static Dictionary<string, Type> _nameToType;

        /// <summary>
        /// 사용 가능한 모든 MotionEventBase 서브클래스 가져오기
        /// </summary>
        public static Type[] GetAllEventTypes()
        {
            if (_eventTypes == null)
                RefreshEventTypes();
            return _eventTypes;
        }

        /// <summary>
        /// 에디터 표시용 이름 목록
        /// </summary>
        public static string[] GetEventTypeNames()
        {
            if (_eventTypeNames == null)
                RefreshEventTypes();
            return _eventTypeNames;
        }

        /// <summary>
        /// 이름으로 타입 찾기
        /// </summary>
        public static Type GetTypeByName(string name)
        {
            if (_nameToType == null)
                RefreshEventTypes();
            return _nameToType.TryGetValue(name, out var type) ? type : null;
        }

        /// <summary>
        /// 이벤트 타입 캐시 갱신
        /// </summary>
        public static void RefreshEventTypes()
        {
            var baseType = typeof(MotionEventBase);
            _eventTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))
                .OrderBy(t => t.Name)
                .ToArray();

            _eventTypeNames = _eventTypes
                .Select(t => GetFriendlyName(t))
                .ToArray();

            _nameToType = new Dictionary<string, Type>();
            for (int i = 0; i < _eventTypes.Length; i++)
            {
                _nameToType[_eventTypeNames[i]] = _eventTypes[i];
            }
        }

        static string GetFriendlyName(Type type)
        {
            // "BeginParticleEvent" -> "Particle"
            string name = type.Name;
            if (name.EndsWith("Event"))
                name = name.Substring(0, name.Length - 5);
            if (name.StartsWith("Begin"))
                name = name.Substring(5);
            return name;
        }

        /// <summary>
        /// 새 이벤트 인스턴스 생성
        /// </summary>
        public static MotionEventBase CreateEventInstance(Type type)
        {
            return Activator.CreateInstance(type) as MotionEventBase;
        }
    }

    /// <summary>
    /// MotionEventBase 커스텀 PropertyDrawer
    /// </summary>
    [CustomPropertyDrawer(typeof(MotionEventBase), true)]
    public class MotionEventBaseDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + 2;

            if (property.isExpanded)
            {
                var iterator = property.Copy();
                var end = iterator.GetEndProperty();
                iterator.NextVisible(true);

                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    if (iterator.name != "startTime" && iterator.name != "endTime")
                        height += EditorGUI.GetPropertyHeight(iterator, true) + 2;
                    iterator.NextVisible(false);
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var obj = GetTargetObjectOfProperty(property) as MotionEventBase;
            if (obj == null)
            {
                EditorGUI.LabelField(position, label.text, "(Null Event)");
                EditorGUI.EndProperty();
                return;
            }

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, obj.GetDisplayName(), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = foldoutRect.yMax + 2;

                var iterator = property.Copy();
                var end = iterator.GetEndProperty();
                iterator.NextVisible(true);

                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    if (iterator.name != "startTime" && iterator.name != "endTime")
                    {
                        float h = EditorGUI.GetPropertyHeight(iterator, true);
                        Rect propRect = new Rect(position.x, y, position.width, h);
                        EditorGUI.PropertyField(propRect, iterator, true);
                        y += h + 2;
                    }
                    iterator.NextVisible(false);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = int.Parse(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }

        static object GetValue_Imp(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f != null) return f.GetValue(source);

                var p = type.GetProperty(name, System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.IgnoreCase);
                if (p != null) return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
    }

    /// <summary>
    /// 이벤트 추가 팝업 메뉴 헬퍼
    /// </summary>
    public static class MotionEventMenuHelper
    {
        public static void ShowAddEventMenu(List<MotionEventBase> eventList, float defaultStartTime, Action onAdd)
        {
            GenericMenu menu = new GenericMenu();
            
            var types = MotionEventTypeRegistry.GetAllEventTypes();
            var names = MotionEventTypeRegistry.GetEventTypeNames();

            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var name = names[i];
                // 타입 아이콘을 메뉴 경로에 포함
                var visual = MotionEventStyle.GetByType(type);
                
                menu.AddItem(new GUIContent($"{visual.icon}  {name}"), false, () =>
                {
                    var evt = MotionEventTypeRegistry.CreateEventInstance(type);
                    evt.startTime = defaultStartTime;
                    evt.endTime = defaultStartTime + 0.5f;
                    eventList.Add(evt);
                    onAdd?.Invoke();
                });
            }

            menu.ShowAsContext();
        }
    }
}