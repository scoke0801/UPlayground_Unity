using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Editor.Gameplay
{
    /// <summary>
    /// Registry 데이터를 직접 읽는 검색·계층형 GameplayTag 선택 UI.
    /// Registry 변경은 코드 생성이나 재컴파일 없이 즉시 반영된다.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayTag))]
    public sealed class GameplayTagPropertyDrawer : PropertyDrawer
    {
        private const float Gap = 2f;
        private const float ClearButtonWidth = 22f;

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty tagName = property.FindPropertyRelative("_tagName");
            bool isUnregistered = tagName != null
                                  && !string.IsNullOrEmpty(tagName.stringValue)
                                  && !GameplayTagRegistry.IsRegistered(
                                      tagName.stringValue);
            return isUnregistered
                ? EditorGUIUtility.singleLineHeight * 2f + Gap
                : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty tagName = property.FindPropertyRelative("_tagName");
            if (tagName == null)
            {
                EditorGUI.LabelField(
                    position,
                    label,
                    new GUIContent("GameplayTag 직렬화 필드를 찾지 못했습니다."));
                return;
            }

            string current = tagName.stringValue ?? string.Empty;
            bool isUnregistered = !string.IsNullOrEmpty(current)
                                  && !GameplayTagRegistry.IsRegistered(current);

            Rect row = position;
            row.height = EditorGUIUtility.singleLineHeight;
            Rect valueRect = EditorGUI.PrefixLabel(row, label);
            Rect clearRect = valueRect;
            clearRect.xMin = clearRect.xMax - ClearButtonWidth;
            valueRect.xMax = clearRect.xMin - Gap;

            EditorGUI.BeginProperty(position, label, property);
            Color previous = GUI.backgroundColor;
            if (isUnregistered)
                GUI.backgroundColor = new Color(1f, 0.45f, 0.35f);

            string display = string.IsNullOrEmpty(current)
                ? "(없음)"
                : current;
            if (EditorGUI.DropdownButton(
                    valueRect,
                    new GUIContent(display, ResolveTooltip(current)),
                    FocusType.Keyboard))
            {
                var dropdown = new GameplayTagAdvancedDropdown(
                    new AdvancedDropdownState(),
                    GameplayTagRegistry.Definitions,
                    selected =>
                    {
                        tagName.serializedObject.Update();
                        tagName.stringValue = selected;
                        tagName.serializedObject.ApplyModifiedProperties();
                    });
                dropdown.Show(valueRect);
            }

            GUI.backgroundColor = previous;
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(current)))
            {
                if (GUI.Button(clearRect, "×"))
                    tagName.stringValue = string.Empty;
            }
            EditorGUI.EndProperty();

            if (!isUnregistered) return;

            Rect warningRect = position;
            warningRect.y += EditorGUIUtility.singleLineHeight + Gap;
            warningRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.HelpBox(
                warningRect,
                $"Registry에 없는 태그: {current}",
                MessageType.Error);
        }

        private static string ResolveTooltip(string tagName)
        {
            return GameplayTagRegistry.Registry.TryGetDefinition(
                tagName,
                out GameplayTagDefinition definition)
                ? definition.description
                : string.Empty;
        }

        private sealed class GameplayTagAdvancedDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<GameplayTagDefinition> _definitions;
            private readonly Action<string> _onSelected;
            private readonly Dictionary<int, string> _tagByItemId = new();
            private int _nextId = 1;

            public GameplayTagAdvancedDropdown(
                AdvancedDropdownState state,
                IReadOnlyList<GameplayTagDefinition> definitions,
                Action<string> onSelected)
                : base(state)
            {
                _definitions = definitions;
                _onSelected = onSelected;
                minimumSize = new Vector2(440f, 360f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                _tagByItemId.Clear();
                _nextId = 1;

                var root = new AdvancedDropdownItem("Gameplay Tags");
                AddSelectable(root, "(없음)", string.Empty);

                var tree = new TagTreeNode(string.Empty);
                for (int i = 0; i < _definitions.Count; i++)
                {
                    GameplayTagDefinition definition = _definitions[i];
                    if (definition?.IsValid() != true) continue;
                    tree.Add(definition);
                }

                foreach (TagTreeNode child in tree.SortedChildren())
                    root.AddChild(BuildTreeItem(child));
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (_tagByItemId.TryGetValue(item.id, out string tagName))
                    _onSelected?.Invoke(tagName);
            }

            private AdvancedDropdownItem BuildTreeItem(TagTreeNode node)
            {
                bool hasChildren = node.Children.Count > 0;
                string label = hasChildren
                    ? node.Segment
                    : FormatLeaf(node);
                var item = new AdvancedDropdownItem(label);

                if (node.Definition != null)
                {
                    if (hasChildren)
                    {
                        AddSelectable(
                            item,
                            $"이 태그 선택 — {node.FullName}",
                            node.FullName);
                    }
                    else
                    {
                        item.id = RegisterTag(node.FullName);
                    }
                }

                foreach (TagTreeNode child in node.SortedChildren())
                    item.AddChild(BuildTreeItem(child));
                return item;
            }

            private static string FormatLeaf(TagTreeNode node)
            {
                string description = node.Definition?.description;
                return string.IsNullOrWhiteSpace(description)
                    ? node.Segment
                    : $"{node.Segment}  —  {description}";
            }

            private void AddSelectable(
                AdvancedDropdownItem parent,
                string label,
                string tagName)
            {
                var item = new AdvancedDropdownItem(label)
                {
                    id = RegisterTag(tagName),
                };
                parent.AddChild(item);
            }

            private int RegisterTag(string tagName)
            {
                int id = _nextId++;
                _tagByItemId[id] = tagName;
                return id;
            }
        }

        private sealed class TagTreeNode
        {
            public readonly string Segment;
            public readonly Dictionary<string, TagTreeNode> Children =
                new(StringComparer.Ordinal);
            public GameplayTagDefinition Definition;
            public string FullName;

            public TagTreeNode(string segment)
            {
                Segment = segment;
            }

            public void Add(GameplayTagDefinition definition)
            {
                string[] segments = definition.tagName.Split('.');
                TagTreeNode node = this;
                string fullName = string.Empty;
                for (int i = 0; i < segments.Length; i++)
                {
                    string segment = segments[i];
                    fullName = i == 0
                        ? segment
                        : $"{fullName}.{segment}";
                    if (!node.Children.TryGetValue(
                            segment,
                            out TagTreeNode child))
                    {
                        child = new TagTreeNode(segment)
                        {
                            FullName = fullName,
                        };
                        node.Children.Add(segment, child);
                    }
                    node = child;
                }

                node.Definition = definition;
                node.FullName = definition.tagName;
            }

            public List<TagTreeNode> SortedChildren()
            {
                var result = new List<TagTreeNode>(Children.Values);
                result.Sort((left, right) => string.Compare(
                    left.Segment,
                    right.Segment,
                    StringComparison.Ordinal));
                return result;
            }
        }
    }
}
