using System;
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
                SyncSiblingVariableId(property, graph, values[newIndex]);
                SyncSiblingValueType(property, graph, values[newIndex]);
            }
        }

        private static void SyncSiblingVariableId(
            SerializedProperty variableName,
            FlowGraphSO graph,
            string selectedName)
        {
            FlowVariableDef selected = graph.variables.Find(def =>
                def != null && def.name == selectedName);
            if (selected == null)
                return;

            int lastDot = variableName.propertyPath.LastIndexOf('.');
            if (lastDot < 0)
                return;
            string parentPath = variableName.propertyPath.Substring(0, lastDot);
            SerializedProperty id = variableName.serializedObject
                .FindProperty($"{parentPath}.variableId");
            if (id != null)
                id.stringValue = selected.id;
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

    /// <summary>SubGraph 공개 인자와 부모 Blackboard 변수를 타입 호환 드롭다운으로 매핑한다.</summary>
    [CustomPropertyDrawer(typeof(FlowParameterBinding))]
    public sealed class FlowParameterBindingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is not FlowGraphSO parent
                || !TryGetSubGraphNode(parent, property.propertyPath, out SubGraphNode sub)
                || sub.subGraph == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            SerializedProperty parameterId = property.FindPropertyRelative("parameterId");
            SerializedProperty parameterName = property.FindPropertyRelative("parameterName");
            SerializedProperty variableId = property.FindPropertyRelative("parentVariableId");
            SerializedProperty variableName = property.FindPropertyRelative("parentVariableName");

            var parameters = new List<FlowGraphParameterDef>();
            foreach (FlowGraphParameterDef parameter in sub.subGraph.parameters)
            {
                if (parameter != null)
                    parameters.Add(parameter);
            }

            int parameterIndex = FindParameterIndex(parameters, parameterId.stringValue, parameterName.stringValue);
            string[] parameterOptions = new string[parameters.Count + 1];
            parameterOptions[0] = "(인자 선택)";
            for (int i = 0; i < parameters.Count; i++)
            {
                FlowGraphParameterDef parameter = parameters[i];
                parameterOptions[i + 1] = $"{parameter.direction}  {parameter.name} ({parameter.type})";
            }

            Rect first = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int selectedParameter = EditorGUI.Popup(first, label.text, parameterIndex + 1, parameterOptions);
            FlowGraphParameterDef selected = selectedParameter > 0
                ? parameters[selectedParameter - 1]
                : null;
            if (selected != null)
            {
                parameterId.stringValue = selected.id;
                parameterName.stringValue = selected.name;
            }

            var variables = new List<FlowVariableDef>();
            foreach (FlowVariableDef variable in parent.variables)
            {
                if (variable != null && (selected == null || variable.type == selected.type))
                    variables.Add(variable);
            }
            int variableIndex = FindVariableIndex(variables, variableId.stringValue, variableName.stringValue);
            string[] variableOptions = new string[variables.Count + 1];
            variableOptions[0] = "(부모 변수 선택)";
            for (int i = 0; i < variables.Count; i++)
                variableOptions[i + 1] = $"{variables[i].name} ({variables[i].type})";

            Rect second = new(
                position.x,
                first.yMax + 4f,
                position.width,
                EditorGUIUtility.singleLineHeight);
            int selectedVariable = EditorGUI.Popup(second, "↔ Parent", variableIndex + 1, variableOptions);
            if (selectedVariable > 0)
            {
                FlowVariableDef variable = variables[selectedVariable - 1];
                variableId.stringValue = variable.id;
                variableName.stringValue = variable.name;
            }
        }

        private static int FindParameterIndex(
            List<FlowGraphParameterDef> parameters,
            string id,
            string name)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if ((!string.IsNullOrEmpty(id) && parameters[i].id == id)
                    || (string.IsNullOrEmpty(id) && parameters[i].name == name))
                    return i;
            }
            return -1;
        }

        private static int FindVariableIndex(
            List<FlowVariableDef> variables,
            string id,
            string name)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                if ((!string.IsNullOrEmpty(id) && variables[i].id == id)
                    || (string.IsNullOrEmpty(id) && variables[i].name == name))
                    return i;
            }
            return -1;
        }

        private static bool TryGetSubGraphNode(
            FlowGraphSO graph,
            string propertyPath,
            out SubGraphNode subGraph)
        {
            const string prefix = "nodes.Array.data[";
            int start = propertyPath.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
            {
                subGraph = null;
                return false;
            }
            start += prefix.Length;
            int end = propertyPath.IndexOf(']', start);
            if (end < 0
                || !int.TryParse(propertyPath.Substring(start, end - start), out int index)
                || index < 0
                || index >= graph.nodes.Count)
            {
                subGraph = null;
                return false;
            }
            subGraph = graph.nodes[index] as SubGraphNode;
            return subGraph != null;
        }
    }
}
