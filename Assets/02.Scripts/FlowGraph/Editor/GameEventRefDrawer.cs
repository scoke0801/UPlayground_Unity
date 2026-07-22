using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// GameEventRef의 enum 타입/값을 드롭다운으로 고르게 하는 드로어.
    /// 후보는 프로젝트(UPlayGround.*) enum 전체 — 문자열 오타로 인한 런타임 해석 실패를 방지한다.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameEventRef))]
    public sealed class GameEventRefDrawer : PropertyDrawer
    {
        private static string[] _enumTypeNames;

        private static string[] EnumTypeNames
        {
            get
            {
                _enumTypeNames ??= TypeCache.GetTypesDerivedFrom<Enum>()
                    .Where(t => t.IsEnum
                        && t.FullName != null
                        && t.FullName.StartsWith("UPlayGround.", StringComparison.Ordinal))
                    .Select(t => t.FullName)
                    .OrderBy(n => n)
                    .ToArray();
                return _enumTypeNames;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeProp = property.FindPropertyRelative("enumTypeName");
            SerializedProperty valueProp = property.FindPropertyRelative("valueName");

            Rect typeRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect valueRect = new(position.x,
                position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUIUtility.singleLineHeight);

            // enum 타입 드롭다운
            int typeIndex = Array.IndexOf(EnumTypeNames, typeProp.stringValue);
            int newTypeIndex = EditorGUI.Popup(typeRect, label.text, typeIndex, EnumTypeNames);
            if (newTypeIndex != typeIndex && newTypeIndex >= 0)
            {
                typeProp.stringValue = EnumTypeNames[newTypeIndex];
                valueProp.stringValue = string.Empty;
            }

            // 값 드롭다운 (타입 미해석 시 텍스트 입력 폴백)
            Type enumType = ResolveType(typeProp.stringValue);
            if (enumType != null)
            {
                string[] names = Enum.GetNames(enumType);
                int valueIndex = Array.IndexOf(names, valueProp.stringValue);
                int newValueIndex = EditorGUI.Popup(valueRect, " ", valueIndex, names);
                if (newValueIndex != valueIndex && newValueIndex >= 0)
                    valueProp.stringValue = names[newValueIndex];
            }
            else
            {
                valueProp.stringValue = EditorGUI.TextField(valueRect, " ", valueProp.stringValue);
            }
        }

        private static Type ResolveType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;
            foreach (Type type in TypeCache.GetTypesDerivedFrom<Enum>())
            {
                if (type.FullName == fullName)
                    return type;
            }
            return null;
        }
    }
}
