using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// [FlowVariableName] string 필드를 그래프 Blackboard 선언 변수 드롭다운으로 그린다.
    /// 노드가 FlowGraphSO 안에 직렬화되므로 serializedObject.targetObject에서 선언 목록을 얻는다.
    /// 미선언 값은 "(미선언)" 표기로 유지해 데이터를 잃지 않는다.
    /// </summary>
    [CustomPropertyDrawer(typeof(FlowVariableNameAttribute))]
    public sealed class FlowVariableNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String
                || property.serializedObject.targetObject is not FlowGraphSO graph
                || graph.variables.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var values = new List<string>();
            var options = new List<string>();
            foreach (FlowVariableDef def in graph.variables)
            {
                if (def != null && !string.IsNullOrEmpty(def.name))
                {
                    values.Add(def.name);
                    options.Add($"{def.name}  ({def.type})");
                }
            }

            string current = property.stringValue;
            int index = values.IndexOf(current);
            if (index < 0)
            {
                // 미선언/빈 값도 목록에 노출해 실수로 값이 바뀌지 않게 한다
                options.Insert(0, string.IsNullOrEmpty(current) ? "(선택)" : $"{current} (미선언)");
                values.Insert(0, current);
                index = 0;
            }

            int newIndex = EditorGUI.Popup(position, label.text, index, options.ToArray());
            if (newIndex != index && newIndex >= 0 && newIndex < options.Count)
            {
                property.stringValue = values[newIndex];
                SyncSiblingValueType(property, graph, values[newIndex]);
            }
        }

        /// <summary>변수 선택 시 같은 노드의 value/expected 타입도 선언 타입에 맞춘다.</summary>
        private static void SyncSiblingValueType(SerializedProperty variableName, FlowGraphSO graph, string selectedName)
        {
            FlowVariableDef selected = graph.variables.Find(def => def != null && def.name == selectedName);
            if (selected == null)
                return;

            int lastDot = variableName.propertyPath.LastIndexOf('.');
            if (lastDot < 0)
                return;

            string parentPath = variableName.propertyPath.Substring(0, lastDot);
            SerializedProperty type = variableName.serializedObject.FindProperty($"{parentPath}.value.type")
                                      ?? variableName.serializedObject.FindProperty($"{parentPath}.expected.type");
            if (type != null)
                type.enumValueIndex = (int)selected.type;
        }
    }
}
