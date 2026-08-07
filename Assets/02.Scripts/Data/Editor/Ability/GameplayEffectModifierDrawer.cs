using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Ability;

namespace UPlayGround.Data.Editor.Ability
{
    /// <summary>
    /// Modifier의 크기 계산 방식에 따라 관련 필드만 보여준다.
    /// 방식별 필드를 모두 펼치면 목록이 읽기 어려워지므로 조건부로 그린다.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayEffectModifierDefinition))]
    public sealed class GameplayEffectModifierDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private static readonly string[] FixedFields =
        {
            "attributeId",
            "modifierType",
            "magnitudeSource",
            "value",
        };
        private static readonly string[] AttributeBasedFields =
        {
            "attributeId",
            "modifierType",
            "magnitudeSource",
            "sourceAttributeId",
            "captureSource",
            "capturePolicy",
            "coefficient",
            "preAdd",
            "postAdd",
        };
        private static readonly string[] SetByCallerFields =
        {
            "attributeId",
            "modifierType",
            "magnitudeSource",
            "setByCallerKey",
            "allowMissingSetByCaller",
            "setByCallerDefaultValue",
        };
        private static readonly string[] ScalableByLevelFields =
        {
            "attributeId",
            "modifierType",
            "magnitudeSource",
            "value",
            "perLevel",
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect line = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            foreach (string fieldName in EnumerateVisibleFields(property))
            {
                SerializedProperty field = property.FindPropertyRelative(fieldName);
                if (field == null)
                    continue;
                line.y += line.height + Spacing;
                line.height = EditorGUI.GetPropertyHeight(field, true);
                EditorGUI.PropertyField(line, field, true);
            }
            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            foreach (string fieldName in EnumerateVisibleFields(property))
            {
                SerializedProperty field = property.FindPropertyRelative(fieldName);
                if (field == null)
                    continue;
                height += Spacing + EditorGUI.GetPropertyHeight(field, true);
            }
            return height;
        }

        private static string[] EnumerateVisibleFields(SerializedProperty property)
        {
            SerializedProperty sourceProperty =
                property.FindPropertyRelative("magnitudeSource");
            var source = sourceProperty == null
                ? GameplayEffectMagnitudeSource.Fixed
                : (GameplayEffectMagnitudeSource)sourceProperty.intValue;

            return source switch
            {
                GameplayEffectMagnitudeSource.AttributeBased => AttributeBasedFields,
                GameplayEffectMagnitudeSource.SetByCaller => SetByCallerFields,
                GameplayEffectMagnitudeSource.ScalableByLevel => ScalableByLevelFields,
                _ => FixedFields,
            };
        }
    }
}
