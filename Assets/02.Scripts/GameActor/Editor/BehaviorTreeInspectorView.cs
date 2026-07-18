#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeInspectorView : VisualElement
    {
        private SerializedObject _serializedNode;
        private BTNode _node;
        private BehaviorTreeEditorGroup _group;

        public BehaviorTreeInspectorView(
            Action<BTNode> onNodeChanged = null,
            Action<BehaviorTreeEditorGroup> onGroupChanged = null)
        {
            OnNodeChanged = onNodeChanged;
            OnGroupChanged = onGroupChanged;
            style.flexGrow = 1;
            style.backgroundColor = BehaviorTreeEditorStyles.Panel;
        }

        public Action<BTNode> OnNodeChanged { get; set; }
        public Action<BehaviorTreeEditorGroup> OnGroupChanged { get; set; }

        public void ClearSelection()
        {
            Clear();
            _node = null;
            _group = null;
            _serializedNode = null;

            var empty = new Label("노드 또는 그룹박스를 선택하세요.");
            empty.style.marginLeft = 12f;
            empty.style.marginTop = 12f;
            empty.style.color = BehaviorTreeEditorStyles.TextMuted;
            Add(empty);
        }

        public void UpdateSelection(BTNode node)
        {
            Clear();
            _node = node;
            _group = null;
            _serializedNode = null;

            if (node == null)
            {
                ClearSelection();
                return;
            }

            Add(CreateIdentityHeader(node));
            Add(CreateInspectorSectionLabel("Properties"));
            _serializedNode = new SerializedObject(node);
            var propertyBox = CreatePropertyBox();
            var iterator = _serializedNode.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script")
                        continue;

                    propertyBox.Add(new PropertyField(iterator.Copy()));
                } while (iterator.NextVisible(false));
            }

            propertyBox.Bind(_serializedNode);
            // 바인딩된 필드 수정(Undo 포함)을 감지해 노드 뷰 요약 갱신을 트리거한다.
            propertyBox.TrackSerializedObjectValue(_serializedNode, _ =>
            {
                if (_node == null)
                    return;

                EditorUtility.SetDirty(_node);
                OnNodeChanged?.Invoke(_node);
            });
            Add(propertyBox);

            if (node is BTCompositeNode)
            {
                Add(CreateInspectorSectionLabel("Services"));
                Add(CreateAddServiceButton((BTCompositeNode)node));
            }
        }

        private VisualElement CreateAddServiceButton(BTCompositeNode composite)
        {
            var container = CreatePropertyBox();
            var button = new Button(() =>
            {
                var menu = new GenericMenu();
                var serviceTypes = TypeCache.GetTypesDerivedFrom<BTServiceNode>()
                    .Where(t => !t.IsAbstract && !t.IsGenericType)
                    .OrderBy(t => t.Name)
                    .ToList();

                if (serviceTypes.Count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("(No BTServiceNode subclasses found)"));
                }
                else
                {
                    foreach (var serviceType in serviceTypes)
                    {
                        var capturedType = serviceType;
                        menu.AddItem(new GUIContent(capturedType.Name), false, () => AttachService(composite, capturedType));
                    }
                }

                menu.ShowAsContext();
            })
            {
                text = "+ Add Service"
            };
            StyleButton(button);
            container.Add(button);
            return container;
        }

        private void AttachService(BTCompositeNode composite, System.Type serviceType)
        {
            var assetPath = AssetDatabase.GetAssetPath(composite);
            if (string.IsNullOrEmpty(assetPath))
                return;

            var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(assetPath);
            if (tree == null)
                return;

            var service = ScriptableObject.CreateInstance(serviceType) as BTServiceNode;
            if (service == null)
                return;

            service.name = serviceType.Name;
            service.DisplayName = serviceType.Name;
            service.EnsureGuid();

            Undo.RegisterCreatedObjectUndo(service, "Attach BT Service");
            Undo.RecordObject(tree, "Attach BT Service");
            Undo.RecordObject(composite, "Attach BT Service");

            AssetDatabase.AddObjectToAsset(service, tree);
            tree.Nodes.Add(service);
            composite.Services.Add(service);

            EditorUtility.SetDirty(composite);
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            OnNodeChanged?.Invoke(_node);
            UpdateSelection(_node);
        }

        public void UpdateSelection(BehaviorTreeEditorGroup group)
        {
            Clear();
            _node = null;
            _group = group;
            _serializedNode = null;

            if (group == null)
            {
                ClearSelection();
                return;
            }

            Add(CreateGroupIdentityHeader(group));
            Add(CreateInspectorSectionLabel("Group Box"));
            Add(CreateGroupPropertyBox(group));
        }

        private VisualElement CreateGroupPropertyBox(BehaviorTreeEditorGroup group)
        {
            var propertyBox = CreatePropertyBox();

            var titleField = new TextField("Name")
            {
                value = group.Title,
                isDelayed = true
            };
            StyleField(titleField);
            titleField.RegisterValueChangedCallback(evt =>
            {
                _group.Title = evt.newValue;
                OnGroupChanged?.Invoke(_group);
            });
            propertyBox.Add(titleField);

            var colorField = new ColorField("Color")
            {
                value = group.Color,
                showAlpha = true,
                hdr = false
            };
            StyleField(colorField);
            var applyColorButton = new Button(() =>
            {
                _group.Color = colorField.value;
                OnGroupChanged?.Invoke(_group);
                UpdateSelection(_group);
            })
            {
                text = "Apply Color"
            };
            StyleButton(applyColorButton);
            propertyBox.Add(colorField);
            propertyBox.Add(applyColorButton);

            var positionField = new Vector2Field("Position") { value = group.Rect.position };
            SetVector2FieldDelayed(positionField);
            StyleField(positionField);
            positionField.RegisterValueChangedCallback(evt =>
            {
                var rect = _group.Rect;
                rect.position = evt.newValue;
                _group.Rect = rect;
                OnGroupChanged?.Invoke(_group);
            });
            propertyBox.Add(positionField);

            var sizeField = new Vector2Field("Size") { value = group.Rect.size };
            SetVector2FieldDelayed(sizeField);
            StyleField(sizeField);
            sizeField.RegisterValueChangedCallback(evt =>
            {
                var rect = _group.Rect;
                rect.size = new Vector2(Mathf.Max(220f, evt.newValue.x), Mathf.Max(140f, evt.newValue.y));
                _group.Rect = rect;
                sizeField.SetValueWithoutNotify(rect.size);
                OnGroupChanged?.Invoke(_group);
            });
            propertyBox.Add(sizeField);

            var meta = new Label($"GUID · {ShortGuid(group.Guid)}");
            meta.style.marginTop = 8f;
            meta.style.color = BehaviorTreeEditorStyles.TextDim;
            meta.style.fontSize = 10f;
            propertyBox.Add(meta);

            return propertyBox;
        }

        private static void SetVector2FieldDelayed(Vector2Field field)
        {
            field.Query<FloatField>().ForEach(componentField => componentField.isDelayed = true);
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

            var title = new Label(BehaviorTreeDisplayNameRegistry.GetNodeTitle(node));
            title.style.fontSize = 13f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = BehaviorTreeEditorStyles.Text;
            textBlock.Add(title);

            var meta = new Label($"{GetCategoryName(node)} · {node.GetType().Name} · {node.DisplayName} · {ShortGuid(node.Guid)}");
            meta.style.fontSize = 10f;
            meta.style.color = BehaviorTreeEditorStyles.TextDim;
            textBlock.Add(meta);
            header.Add(textBlock);

            return header;
        }

        private static VisualElement CreateGroupIdentityHeader(BehaviorTreeEditorGroup group)
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
            dot.style.backgroundColor = group.Color;
            dot.style.borderTopLeftRadius = 2f;
            dot.style.borderTopRightRadius = 2f;
            dot.style.borderBottomLeftRadius = 2f;
            dot.style.borderBottomRightRadius = 2f;
            header.Add(dot);

            var textBlock = new VisualElement();
            textBlock.style.flexGrow = 1;

            var title = new Label(group.Title);
            title.style.fontSize = 13f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = BehaviorTreeEditorStyles.Text;
            textBlock.Add(title);

            var meta = new Label($"Group Box · {Mathf.RoundToInt(group.Rect.width)}x{Mathf.RoundToInt(group.Rect.height)}");
            meta.style.fontSize = 10f;
            meta.style.color = BehaviorTreeEditorStyles.TextDim;
            textBlock.Add(meta);
            header.Add(textBlock);

            return header;
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

        private static VisualElement CreatePropertyBox()
        {
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
            return propertyBox;
        }

        private static void StyleField<TValue>(BaseField<TValue> field)
        {
            field.style.marginTop = 3f;
            field.style.marginBottom = 3f;
            field.style.color = BehaviorTreeEditorStyles.Text;
        }

        private static void StyleButton(Button button)
        {
            button.style.height = 24f;
            button.style.marginTop = 3f;
            button.style.marginBottom = 8f;
            button.style.backgroundColor = BehaviorTreeEditorStyles.PanelRaised;
            button.style.color = BehaviorTreeEditorStyles.Text;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderTopColor = BehaviorTreeEditorStyles.BorderStrong;
            button.style.borderRightColor = BehaviorTreeEditorStyles.BorderStrong;
            button.style.borderBottomColor = BehaviorTreeEditorStyles.BorderStrong;
            button.style.borderLeftColor = BehaviorTreeEditorStyles.BorderStrong;
            button.style.borderTopWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderTopLeftRadius = 5f;
            button.style.borderTopRightRadius = 5f;
            button.style.borderBottomLeftRadius = 5f;
            button.style.borderBottomRightRadius = 5f;
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
            if (node is BTServiceNode)
                return "Service";
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
            if (node is BTServiceNode)
                return BehaviorTreeEditorStyles.Decorator;
            return BehaviorTreeEditorStyles.Action;
        }
    }
}
#endif
