#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeInspectorView : VisualElement
    {
        private UnityEditor.Editor _editor;
        private BTNode _node;

        public BehaviorTreeInspectorView(Action<BTNode> onNodeChanged = null)
        {
            OnNodeChanged = onNodeChanged;
            style.flexGrow = 1;
            style.backgroundColor = BehaviorTreeEditorStyles.Panel;
        }

        public Action<BTNode> OnNodeChanged { get; set; }

        public void UpdateSelection(BTNode node)
        {
            Clear();
            _node = node;
            if (_editor != null)
                UnityEngine.Object.DestroyImmediate(_editor);

            if (node == null)
            {
                var empty = new Label("노드를 선택하세요.");
                empty.style.marginLeft = 12f;
                empty.style.marginTop = 12f;
                empty.style.color = BehaviorTreeEditorStyles.TextMuted;
                Add(empty);
                return;
            }

            Add(CreateIdentityHeader(node));
            Add(CreateInspectorSectionLabel("Properties"));
            _editor = UnityEditor.Editor.CreateEditor(node);
            var propertyBox = new VisualElement();
            propertyBox.style.marginLeft = 10f;
            propertyBox.style.marginRight = 10f;
            propertyBox.style.marginBottom = 10f;
            propertyBox.style.paddingLeft = 8f;
            propertyBox.style.paddingRight = 8f;
            propertyBox.style.paddingTop = 6f;
            propertyBox.style.paddingBottom = 6f;
            propertyBox.style.backgroundColor = BehaviorTreeEditorStyles.PanelAlt;
            propertyBox.style.borderTopColor = BehaviorTreeEditorStyles.Border;
            propertyBox.style.borderRightColor = BehaviorTreeEditorStyles.Border;
            propertyBox.style.borderBottomColor = BehaviorTreeEditorStyles.Border;
            propertyBox.style.borderLeftColor = BehaviorTreeEditorStyles.Border;
            propertyBox.style.borderTopWidth = 1f;
            propertyBox.style.borderRightWidth = 1f;
            propertyBox.style.borderBottomWidth = 1f;
            propertyBox.style.borderLeftWidth = 1f;
            propertyBox.style.borderTopLeftRadius = 6f;
            propertyBox.style.borderTopRightRadius = 6f;
            propertyBox.style.borderBottomLeftRadius = 6f;
            propertyBox.style.borderBottomRightRadius = 6f;
            propertyBox.Add(new IMGUIContainer(() =>
            {
                if (_editor == null)
                    return;

                EditorGUI.BeginChangeCheck();
                _editor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_node);
                    OnNodeChanged?.Invoke(_node);
                }
            }));
            Add(propertyBox);
        }

        private static VisualElement CreateIdentityHeader(BTNode node)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginLeft = 10f;
            header.style.marginRight = 10f;
            header.style.marginTop = 10f;
            header.style.marginBottom = 8f;

            var dot = new VisualElement();
            dot.style.width = 10f;
            dot.style.height = 10f;
            dot.style.marginRight = 8f;
            dot.style.backgroundColor = GetCategoryColor(node);
            dot.style.borderTopLeftRadius = 2f;
            dot.style.borderTopRightRadius = 2f;
            dot.style.borderBottomLeftRadius = 2f;
            dot.style.borderBottomRightRadius = 2f;
            header.Add(dot);

            var textBlock = new VisualElement();
            textBlock.style.flexGrow = 1;

            var title = new Label(node.DisplayName);
            title.style.fontSize = 13f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = BehaviorTreeEditorStyles.Text;
            textBlock.Add(title);

            var meta = new Label($"{GetCategoryName(node)} · {node.GetType().Name} · {ShortGuid(node.Guid)}");
            meta.style.fontSize = 10f;
            meta.style.color = BehaviorTreeEditorStyles.TextDim;
            textBlock.Add(meta);
            header.Add(textBlock);

            return header;
        }

        private static VisualElement CreateDivider()
        {
            var divider = new VisualElement();
            divider.style.height = 1f;
            divider.style.marginLeft = 10f;
            divider.style.marginRight = 10f;
            divider.style.marginBottom = 8f;
            divider.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
            return divider;
        }

        private static Label CreateInspectorSectionLabel(string text)
        {
            var label = new Label(text.ToUpperInvariant());
            label.style.marginLeft = 10f;
            label.style.marginRight = 10f;
            label.style.marginTop = 4f;
            label.style.marginBottom = 7f;
            label.style.paddingTop = 7f;
            label.style.borderTopColor = BehaviorTreeEditorStyles.Border;
            label.style.borderTopWidth = 1f;
            label.style.fontSize = 10f;
            label.style.letterSpacing = 1f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = BehaviorTreeEditorStyles.TextDim;
            return label;
        }

        private static string ShortGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return "none";

            return guid.Length > 8 ? guid.Substring(0, 8) : guid;
        }

        private static string GetCategoryName(BTNode node)
        {
            if (node is BTCompositeNode)
                return "Composite";
            if (node is BTDecoratorNode)
                return "Decorator";
            if (node is BTConditionNode)
                return "Condition";
            return "Action";
        }

        private static Color GetCategoryColor(BTNode node)
        {
            if (node is BTCompositeNode)
                return BehaviorTreeEditorStyles.Composite;
            if (node is BTDecoratorNode)
                return BehaviorTreeEditorStyles.Decorator;
            if (node is BTConditionNode)
                return BehaviorTreeEditorStyles.Condition;
            return BehaviorTreeEditorStyles.Action;
        }
    }
}
#endif
