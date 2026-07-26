#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// BlackboardKeySelector의 인스펙터 표시.
    /// 필드를 보유한 노드가 속한 BehaviorTreeAsset의 Blackboard에서
    /// expectedType과 일치하는 키만 드롭다운으로 보여준다.
    /// 일치하는 키가 없거나 현재 키가 더 이상 존재하지 않으면 경고 색으로 표시한다.
    /// UIToolkit 인스펙터에서는 CreatePropertyGUI, IMGUI 인스펙터에서는 OnGUI 폴백을 사용한다.
    /// </summary>
    [CustomPropertyDrawer(typeof(BlackboardKeySelector))]
    public class BlackboardKeySelectorDrawer : PropertyDrawer
    {
        private const string NoneLabel = "<none>";
        private static readonly Color MismatchColor = new Color(1f, 0.55f, 0.55f);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var keyProp = property.FindPropertyRelative("_key");
            var stableIdProp = property.FindPropertyRelative("_stableId");
            var typeProp = property.FindPropertyRelative("_expectedType");
            if (keyProp == null || stableIdProp == null || typeProp == null)
                return new Label($"{property.displayName}: Invalid BlackboardKeySelector");

            var dropdown = new DropdownField();
            dropdown.AddToClassList(DropdownField.alignedFieldUssClassName);
            dropdown.formatSelectedValueCallback = key => FormatChoice(property, key);
            dropdown.formatListItemCallback = key => FormatChoice(property, key);

            void Refresh()
            {
                if (property.serializedObject == null || property.serializedObject.targetObject == null)
                    return;

                var expectedType = (BlackboardValueType)typeProp.enumValueIndex;
                dropdown.label = $"{property.displayName} ({expectedType})";

                var currentKey = keyProp.stringValue;
                var choices = CollectMatchingKeys(ResolveBlackboard(property), expectedType);
                var hasMismatch = !string.IsNullOrWhiteSpace(currentKey) && !choices.Contains(currentKey);

                choices.Insert(0, string.Empty);
                if (hasMismatch)
                    choices.Add(currentKey);

                dropdown.choices = choices;
                dropdown.SetValueWithoutNotify(string.IsNullOrWhiteSpace(currentKey) ? string.Empty : currentKey);

                StyleColor labelColor = hasMismatch
                    ? new StyleColor(MismatchColor)
                    : new StyleColor(StyleKeyword.Null);
                dropdown.labelElement.style.color = labelColor;
                foreach (var text in dropdown.Query<TextElement>().ToList())
                    text.style.color = labelColor;
            }

            dropdown.RegisterValueChangedCallback(evt =>
            {
                keyProp.stringValue = evt.newValue ?? string.Empty;
                stableIdProp.stringValue = ResolveStableId(evt.newValue);
                property.serializedObject.ApplyModifiedProperties();
                Refresh();
            });

            // 다른 경로(Rename 툴, Undo 등)로 값이나 기대 타입이 바뀌면 표시를 동기화한다.
            dropdown.TrackPropertyValue(keyProp, _ => Refresh());
            dropdown.TrackPropertyValue(typeProp, _ => Refresh());
            // Blackboard Key 목록은 외부(Blackboard 패널)에서 수시로 바뀌므로 드롭다운을 열기 직전에 갱신한다.
            dropdown.RegisterCallback<PointerEnterEvent>(_ => Refresh());
            dropdown.RegisterCallback<AttachToPanelEvent>(_ => Refresh());

            return dropdown;
        }

        private static string FormatChoice(SerializedProperty property, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return NoneLabel;

            var keyProp = property.serializedObject != null && property.serializedObject.targetObject != null
                ? property.FindPropertyRelative("_key")
                : null;
            var typeProp = keyProp != null ? property.FindPropertyRelative("_expectedType") : null;
            if (keyProp != null && typeProp != null &&
                string.Equals(key, keyProp.stringValue, System.StringComparison.Ordinal))
            {
                var expectedType = (BlackboardValueType)typeProp.enumValueIndex;
                var options = CollectMatchingKeys(ResolveBlackboard(property), expectedType);
                if (!options.Contains(key))
                    return $"{key} (missing)";
            }

            return BehaviorTreeDisplayNameRegistry.FormatWithRawName(
                BehaviorTreeDisplayNameRegistry.GetBlackboardLabel(key),
                key);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var keyProp = property.FindPropertyRelative("_key");
            var stableIdProp = property.FindPropertyRelative("_stableId");
            var typeProp = property.FindPropertyRelative("_expectedType");
            if (keyProp == null || stableIdProp == null || typeProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Invalid BlackboardKeySelector");
                return;
            }

            var expectedType = (BlackboardValueType)typeProp.enumValueIndex;
            var blackboard = ResolveBlackboard(property);
            var options = CollectMatchingKeys(blackboard, expectedType);

            var currentKey = keyProp.stringValue;
            var currentIndex = options.IndexOf(currentKey);
            var hasMismatch = !string.IsNullOrWhiteSpace(currentKey) && currentIndex < 0;

            options.Insert(0, NoneLabel);
            if (currentIndex >= 0)
                currentIndex += 1;
            else if (string.IsNullOrWhiteSpace(currentKey))
                currentIndex = 0;
            else
            {
                // 현재 값이 옵션에 없으면 미스매치 표시용으로 임시 추가
                options.Add($"{currentKey} (missing)");
                currentIndex = options.Count - 1;
            }

            var contents = new GUIContent[options.Count];
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var optionLabelText = option == NoneLabel || option.EndsWith("(missing)")
                    ? option
                    : BehaviorTreeDisplayNameRegistry.FormatWithRawName(
                        BehaviorTreeDisplayNameRegistry.GetBlackboardLabel(option),
                        option);
                contents[i] = new GUIContent(optionLabelText);
            }

            EditorGUI.BeginProperty(position, label, property);

            var labelText = label.text + $" ({expectedType})";
            var rowLabel = new GUIContent(labelText, label.tooltip);

            var oldColor = GUI.color;
            if (hasMismatch)
                GUI.color = new Color(1f, 0.55f, 0.55f);

            var selected = EditorGUI.Popup(position, rowLabel, currentIndex, contents);
            GUI.color = oldColor;

            if (selected != currentIndex)
            {
                if (selected <= 0)
                {
                    keyProp.stringValue = string.Empty;
                    stableIdProp.stringValue = string.Empty;
                }
                else if (!options[selected].EndsWith("(missing)"))
                {
                    keyProp.stringValue = options[selected];
                    stableIdProp.stringValue = ResolveStableId(options[selected]);
                }
            }

            EditorGUI.EndProperty();
        }

        private static string ResolveStableId(string key)
        {
            return BlackboardKeyRegistry.TryResolve(
                key,
                out BlackboardKeyReference reference)
                ? reference.StableId
                : string.Empty;
        }

        private static List<string> CollectMatchingKeys(Blackboard blackboard, BlackboardValueType expectedType)
        {
            var result = new List<string>();
            if (blackboard == null)
                return result;

            foreach (var entry in blackboard.Entries)
            {
                if (entry == null || entry.ValueType != expectedType || string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                result.Add(entry.Key);
            }

            return result;
        }

        private static Blackboard ResolveBlackboard(SerializedProperty property)
        {
            // 1순위: 노드가 속한 BehaviorTreeAsset
            if (property.serializedObject.targetObject is BTNode node)
            {
                var path = AssetDatabase.GetAssetPath(node);
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
                    if (asset != null)
                        return asset.Blackboard;
                }
            }

            // 2순위: BT 에셋이 직접 직렬화 대상
            if (property.serializedObject.targetObject is BehaviorTreeAsset tree)
                return tree.Blackboard;

            return null;
        }
    }
}
#endif
