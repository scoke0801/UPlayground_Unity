using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Stat
{
    [CustomPropertyDrawer(typeof(AttributeReference))]
    public sealed class AttributeReferencePropertyDrawer : PropertyDrawer
    {
        private const float Gap = 2f;
        private const float ClearButtonWidth = 22f;

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty id =
                property.FindPropertyRelative("_attributeId");
            return GetStringPropertyHeight(id);
        }

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty id =
                property.FindPropertyRelative("_attributeId");
            if (id == null)
            {
                EditorGUI.LabelField(
                    position,
                    label,
                    new GUIContent("AttributeReference 직렬화 필드를 찾지 못했습니다."));
                return;
            }

            DrawStringProperty(position, id, label);
        }

        internal static float GetStringPropertyHeight(
            SerializedProperty id)
        {
            bool unregistered = id != null
                                && id.propertyType
                                == SerializedPropertyType.String
                                && !string.IsNullOrEmpty(id.stringValue)
                                && !AttributeRegistry.IsRegistered(
                                    id.stringValue);
            return unregistered
                ? EditorGUIUtility.singleLineHeight * 2f + Gap
                : EditorGUIUtility.singleLineHeight;
        }

        internal static void DrawStringProperty(
            Rect position,
            SerializedProperty id,
            GUIContent label)
        {
            if (id == null
                || id.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(
                    position,
                    label,
                    new GUIContent("Attribute ID 선택기는 string 필드에만 사용할 수 있습니다."));
                return;
            }

            string current = id.stringValue ?? string.Empty;
            bool unregistered = !string.IsNullOrEmpty(current)
                                && !AttributeRegistry.IsRegistered(current);
            Rect row = position;
            row.height = EditorGUIUtility.singleLineHeight;
            Rect valueRect = EditorGUI.PrefixLabel(row, label);
            Rect clearRect = valueRect;
            clearRect.xMin = clearRect.xMax - ClearButtonWidth;
            valueRect.xMax = clearRect.xMin - Gap;

            EditorGUI.BeginProperty(position, label, id);
            Color previous = GUI.backgroundColor;
            if (unregistered)
                GUI.backgroundColor = new Color(1f, 0.45f, 0.35f);
            if (EditorGUI.DropdownButton(
                    valueRect,
                    new GUIContent(
                        string.IsNullOrEmpty(current)
                            ? "(없음)"
                            : current,
                        ResolveTooltip(current)),
                    FocusType.Keyboard))
            {
                var dropdown = new AttributeDropdown(
                    new AdvancedDropdownState(),
                    AttributeRegistry.Definitions,
                    selected =>
                    {
                        id.serializedObject.Update();
                        id.stringValue = selected;
                        id.serializedObject.ApplyModifiedProperties();
                    });
                dropdown.Show(valueRect);
            }

            GUI.backgroundColor = previous;
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(current)))
            {
                if (GUI.Button(clearRect, "×"))
                    id.stringValue = string.Empty;
            }
            EditorGUI.EndProperty();

            if (!unregistered) return;
            Rect warning = position;
            warning.y += EditorGUIUtility.singleLineHeight + Gap;
            warning.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.HelpBox(
                warning,
                $"Registry에 없는 Attribute: {current}",
                MessageType.Error);
        }

        private static string ResolveTooltip(string attributeId)
        {
            return AttributeRegistry.Registry.TryResolve(
                attributeId,
                out AttributeRegistryEntry entry)
                ? $"{entry.displayName} / {entry.category}"
                : string.Empty;
        }

        private sealed class AttributeDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<AttributeRegistryEntry> _entries;
            private readonly Action<string> _onSelected;
            private readonly Dictionary<int, string> _values = new();
            private int _nextId = 1;

            public AttributeDropdown(
                AdvancedDropdownState state,
                IReadOnlyList<AttributeRegistryEntry> entries,
                Action<string> onSelected)
                : base(state)
            {
                _entries = entries;
                _onSelected = onSelected;
                minimumSize = new Vector2(440f, 360f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                _values.Clear();
                _nextId = 1;
                var root = new AdvancedDropdownItem("Attributes");
                Add(root, "(없음)", string.Empty);
                var categories =
                    new Dictionary<string, AdvancedDropdownItem>(
                        StringComparer.Ordinal);
                for (int i = 0; i < _entries.Count; i++)
                {
                    AttributeRegistryEntry entry = _entries[i];
                    if (entry?.IsValid() != true) continue;
                    string category = string.IsNullOrWhiteSpace(entry.category)
                        ? "기타"
                        : entry.category;
                    if (!categories.TryGetValue(
                            category,
                            out AdvancedDropdownItem parent))
                    {
                        parent = new AdvancedDropdownItem(category);
                        categories.Add(category, parent);
                        root.AddChild(parent);
                    }
                    string label = string.IsNullOrWhiteSpace(entry.displayName)
                        ? entry.attributeId
                        : $"{entry.displayName} — {entry.attributeId}";
                    Add(parent, label, entry.attributeId);
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (_values.TryGetValue(item.id, out string value))
                    _onSelected?.Invoke(value);
            }

            private void Add(
                AdvancedDropdownItem parent,
                string label,
                string value)
            {
                int id = _nextId++;
                _values[id] = value;
                parent.AddChild(new AdvancedDropdownItem(label) { id = id });
            }
        }
    }

    [CustomPropertyDrawer(typeof(AttributeIdSelectorAttribute))]
    public sealed class AttributeIdSelectorPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label) =>
            AttributeReferencePropertyDrawer.GetStringPropertyHeight(
                property);

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label) =>
            AttributeReferencePropertyDrawer.DrawStringProperty(
                position,
                property,
                label);
    }
}
