#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeInspectorView : VisualElement
    {
        private UnityEditor.Editor _editor;

        public BehaviorTreeInspectorView()
        {
            style.flexGrow = 1;
        }

        public void UpdateSelection(BTNode node)
        {
            Clear();
            if (_editor != null)
                UnityEngine.Object.DestroyImmediate(_editor);

            if (node == null)
            {
                var empty = new Label("노드를 선택하세요.");
                empty.style.marginLeft = 12f;
                empty.style.marginTop = 12f;
                empty.style.color = new Color(0.58f, 0.58f, 0.68f);
                Add(empty);
                return;
            }

            Add(CreateIdentityHeader(node));
            Add(CreateDivider());
            _editor = UnityEditor.Editor.CreateEditor(node);
            Add(new IMGUIContainer(() =>
            {
                if (_editor == null)
                    return;

                _editor.OnInspectorGUI();
            }));
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
            title.style.color = new Color(0.90f, 0.90f, 0.94f);
            textBlock.Add(title);

            var meta = new Label($"{GetCategoryName(node)} · {node.GetType().Name} · {ShortGuid(node.Guid)}");
            meta.style.fontSize = 10f;
            meta.style.color = new Color(0.42f, 0.42f, 0.52f);
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
                return new Color(0.34f, 0.48f, 0.86f);
            if (node is BTDecoratorNode)
                return new Color(0.72f, 0.42f, 0.86f);
            if (node is BTConditionNode)
                return new Color(0.90f, 0.62f, 0.24f);
            return new Color(0.34f, 0.78f, 0.52f);
        }
    }
}
#endif
